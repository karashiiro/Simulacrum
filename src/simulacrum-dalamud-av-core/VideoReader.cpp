#include <cassert>
#include <string>
#include <windows.h>
#include "VideoReader.h"

typedef short sample_container;

constexpr auto out_sample_format = AV_SAMPLE_FMT_S16;

enum
{
    max_audio_frame_size = 192000,
    max_audio_buffer_size = max_audio_frame_size * 3 / 2,
    out_bits_per_sample = sizeof(sample_container) * 8,
    out_audio_channels = 2,
};

// Ripped from
// * https://github.com/bmewj/video-app
// * https://ffmpeg.org/doxygen/trunk/api-h264-test_8c_source.html
// * https://ffmpeg.org/doxygen/trunk/ffplay_8c_source.html
// * http://dranger.com/ffmpeg/ffmpeg.html
// * https://rodic.fr/blog/libavcodec-tutorial-decode-audio-file/

// av_err2str returns a temporary array. This doesn't work in gcc.
// This function can be used as a replacement for av_err2str.
static const char* av_make_error(int errnum)
{
    static char str[AV_ERROR_MAX_STRING_SIZE] = {};
    return av_make_error_string(str, AV_ERROR_MAX_STRING_SIZE, errnum);
}

static AVPixelFormat correct_for_deprecated_pixel_format(const AVPixelFormat pix_fmt)
{
    // Fix swscaler deprecated pixel format warning
    // (YUVJ has been deprecated, change pixel format to regular YUV)
    switch (pix_fmt) // NOLINT(clang-diagnostic-switch-enum)
    {
    case AV_PIX_FMT_YUVJ420P: return AV_PIX_FMT_YUV420P;
    case AV_PIX_FMT_YUVJ422P: return AV_PIX_FMT_YUV422P;
    case AV_PIX_FMT_YUVJ444P: return AV_PIX_FMT_YUV444P;
    case AV_PIX_FMT_YUVJ440P: return AV_PIX_FMT_YUV440P;
    default: return pix_fmt;
    }
}

static double pts_to_seconds(const int64_t pts_raw, const AVRational time_base)
{
    return static_cast<double>(pts_raw) * av_q2d(time_base);
}

Simulacrum::AV::Core::VideoReader::VideoReader()
    : width{},
      height{},
      sample_rate{},
      bits_per_sample{},
      audio_channel_count{},
      video_frame_delay{},
      supports_audio{},
      audio_stream{},
      video_stream{},
      audio_buffer_total_size{},
      audio_buffer_size{},
      audio_buffer_index{},
      video_last_frame_timestamp{},
      done{},
      av_format_ctx{},
      sws_scaler_ctx{},
      swr_resampler_ctx{}
{
    audio_stream.packet_queue = new PacketQueue();
    video_stream.packet_queue = new PacketQueue();
    audio_buffer_pending = new uint8_t[max_audio_buffer_size];
    memset(audio_buffer_pending, 0, max_audio_buffer_size);
}

Simulacrum::AV::Core::VideoReader::~VideoReader()
{
    delete audio_stream.packet_queue;
    delete video_stream.packet_queue;
    delete[] audio_buffer_pending;
}

bool Simulacrum::AV::Core::VideoReader::Open(const char* uri)
{
    av_format_ctx = avformat_alloc_context();
    if (!av_format_ctx)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not allocate format context");
        return false;
    }

    // Open the input URI
    if (avformat_open_input(&av_format_ctx, uri, nullptr, nullptr) != 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not open input object");
        return false;
    }

    // Load the stream info for formats that don't provide size information in their header
    if (avformat_find_stream_info(av_format_ctx, nullptr) != 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not find stream info");
        return false;
    }

    // Find the first valid audio stream inside the file
    audio_stream.stream_index = av_find_best_stream(av_format_ctx, AVMEDIA_TYPE_AUDIO, -1, -1, nullptr, 0);
    if (audio_stream.stream_index < 0)
    {
        // TODO: Soundless video files are valid but need special handling here
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not find audio stream in input file");
        return false;
    }

    supports_audio = audio_stream.stream_index != -1;

    // Find the first valid video stream inside the file
    video_stream.stream_index = av_find_best_stream(av_format_ctx, AVMEDIA_TYPE_VIDEO, -1, -1, nullptr, 0);
    if (video_stream.stream_index < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not find video stream in input file");
        return false;
    }

    const AVCodecParameters* audio_codec_params = nullptr;
    const AVCodec* audio_codec = nullptr;
    if (!FindDecoder(audio_stream.stream_index, audio_codec_params, audio_codec))
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not find audio decoder");
        return false;
    }

    const AVCodecParameters* video_codec_params = nullptr;
    const AVCodec* video_codec = nullptr;
    if (!FindDecoder(video_stream.stream_index, video_codec_params, video_codec))
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not find video decoder");
        return false;
    }

    if (supports_audio)
    {
        sample_rate = audio_codec_params->sample_rate;
        bits_per_sample = out_bits_per_sample;
        audio_channel_count = out_audio_channels;
        audio_stream.time_base = av_format_ctx->streams[audio_stream.stream_index]->time_base;
    }

    width = video_codec_params->width;
    height = video_codec_params->height;
    video_stream.time_base = av_format_ctx->streams[video_stream.stream_index]->time_base;

    // Set up a codec context for the audio decoder
    if (!InitializeCodecContext(audio_stream.codec_ctx, *audio_codec_params, *audio_codec))
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not initialize audio decoder context");
        return false;
    }

    // Set up a codec context for the video decoder
    if (!InitializeCodecContext(video_stream.codec_ctx, *video_codec_params, *video_codec))
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not initialize video decoder context");
        return false;
    }

    ingest_thread = std::thread(&VideoReader::Ingest, this);

    return true;
}

int Simulacrum::AV::Core::VideoReader::ReadAudioStream(uint8_t* audio_buffer, const int len, double& pts)
{
    if (audio_stream.flush_requested || audio_buffer_size == 0)
    {
        // Do an initial decode in case we have no data, so the pts we return is correct
        if (!DecodeAudioFrame())
        {
            // Failed to decode audio frame, nothing to do
            return 0;
        }
    }

    // Return the estimated pts of the beginning of the audio buffer
    const auto block_alignment = audio_channel_count * (bits_per_sample / 8);
    const auto average_bytes_per_second = sample_rate * block_alignment;
    const auto best_effort_timestamp = audio_stream.current_frame.best_effort_timestamp;
    const auto pts_base = pts_to_seconds(best_effort_timestamp, audio_stream.time_base);
    const auto initial_audio_buffer_index = audio_buffer_index;
    pts = pts_base + initial_audio_buffer_index / static_cast<double>(average_bytes_per_second);

    int n_read = 0;
    while (n_read < len)
    {
        if (audio_stream.flush_requested || audio_buffer_size == 0 && !DecodeAudioFrame())
        {
            // Failed to decode audio frame, nothing to do
            break;
        }

        const auto to_read = min(len - n_read, audio_buffer_size);
        memcpy(audio_buffer + n_read, audio_buffer_pending + audio_buffer_index, to_read);
        n_read += to_read;
        audio_buffer_index += to_read;
        audio_buffer_size -= to_read;
    }

    return n_read;
}

bool Simulacrum::AV::Core::VideoReader::ReadVideoFrame(
    uint8_t* frame_buffer,
    const double& target_pts,
    double& pts)
{
    do
    {
        if (!DecodeVideoFrame())
        {
            return false;
        }

        const auto best_effort_timestamp = video_stream.current_frame.best_effort_timestamp;

        pts = pts_to_seconds(best_effort_timestamp, video_stream.time_base);

        const auto pts_diff = best_effort_timestamp - video_last_frame_timestamp;
        video_frame_delay = pts_to_seconds(pts_diff, video_stream.time_base);

        video_last_frame_timestamp = best_effort_timestamp;
    }
    while (pts < target_pts);

    // Initialize the scaler if needed, now that some data has been decoded into the codec context
    if (!sws_scaler_ctx && !InitializeVideoScaler())
    {
        return false;
    }

    if (frame_buffer)
    {
        CopyScaledVideo(frame_buffer);
    }

    return true;
}

bool Simulacrum::AV::Core::VideoReader::SeekAudioStream(const double target_pts)
{
    audio_stream.seek_pts = target_pts;
    audio_stream.seek_requested = true;

    // This may or may not cause problems
    const auto last_pts = pts_to_seconds(video_last_frame_timestamp, video_stream.time_base);
    audio_stream.seek_flags = target_pts - last_pts > 0 ? 0 : AVSEEK_FLAG_BACKWARD;

    return true;
}

bool Simulacrum::AV::Core::VideoReader::SeekVideoFrame(const double target_pts)
{
    video_stream.seek_pts = target_pts;
    video_stream.seek_requested = true;

    const auto last_pts = pts_to_seconds(video_last_frame_timestamp, video_stream.time_base);
    video_stream.seek_flags = target_pts - last_pts > 0 ? 0 : AVSEEK_FLAG_BACKWARD;

    return true;
}

void Simulacrum::AV::Core::VideoReader::Close()
{
    done = true;
    ingest_thread.join();

    if (sws_scaler_ctx)
    {
        sws_freeContext(sws_scaler_ctx);
        sws_scaler_ctx = nullptr;
    }

    if (swr_resampler_ctx)
    {
        swr_free(&swr_resampler_ctx);
        swr_resampler_ctx = nullptr;
    }

    if (av_format_ctx)
    {
        avformat_close_input(&av_format_ctx);
        avformat_free_context(av_format_ctx);
        av_format_ctx = nullptr;
    }

    if (audio_stream.codec_ctx)
    {
        avcodec_free_context(&audio_stream.codec_ctx);
        audio_stream.codec_ctx = nullptr;
    }

    if (video_stream.codec_ctx)
    {
        avcodec_free_context(&video_stream.codec_ctx);
        video_stream.codec_ctx = nullptr;
    }
}

bool Simulacrum::AV::Core::VideoReader::FindDecoder(
    const int stream_index,
    const AVCodecParameters*& codec_params,
    const AVCodec*& codec) const
{
    const auto* found_codec_params = av_format_ctx->streams[stream_index]->codecpar;
    const auto* found_codec = avcodec_find_decoder(found_codec_params->codec_id);
    if (!found_codec)
    {
        return false;
    }

    codec_params = found_codec_params;
    codec = found_codec;

    return true;
}

bool Simulacrum::AV::Core::VideoReader::InitializeCodecContext(
    AVCodecContext*& codec_ctx,
    const AVCodecParameters& codec_params,
    const AVCodec& codec)
{
    codec_ctx = avcodec_alloc_context3(&codec);
    if (!codec_ctx)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not allocate decoder context");
        return false;
    }

    if (avcodec_parameters_to_context(codec_ctx, &codec_params) < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not copy decoder context");
        return false;
    }

    if (avcodec_open2(codec_ctx, &codec, nullptr) < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not open decoder");
        return false;
    }

    return true;
}

bool Simulacrum::AV::Core::VideoReader::DecodeAudioFrame()
{
    if (audio_stream.flush_requested)
    {
        // Handle audio stream flush requests
        audio_stream.flush_requested = false;
        avcodec_flush_buffers(audio_stream.codec_ctx);
    }

    AVPacket* next_packet_raw;
    if (!audio_stream.packet_queue->Pop(next_packet_raw))
    {
        return false;
    }

    // Set up the packet to be disposed at the end of the scope
    const std::shared_ptr<AVPacket*> next_packet(&next_packet_raw, av_packet_free);

    int result = avcodec_send_packet(audio_stream.codec_ctx, *next_packet);
    if (result < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Error submitting packet for decoding: %s", av_make_error(result));
        return false;
    }

    result = avcodec_receive_frame(audio_stream.codec_ctx, &audio_stream.current_frame);
    if (result < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Error decoding frame: %s", av_make_error(result));
        return false;
    }

    // Initialize the resampler if needed, now that some data has been decoded into the codec context
    if (!swr_resampler_ctx && !InitializeAudioResampler())
    {
        return false;
    }

    const auto req_size = av_samples_get_buffer_size(nullptr, audio_channel_count,
                                                     audio_stream.current_frame.nb_samples, out_sample_format, 1);
    if (req_size < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not get required audio buffer size");
        return false;
    }

    assert(max_audio_buffer_size - audio_buffer_size >= req_size);

    // Resample the audio into our expected format
    int sample_count;
    if (!CopyResampledAudio(audio_buffer_pending, sample_count))
    {
        return false;
    }

    assert(audio_channel_count * sample_count * static_cast<int>(sizeof(sample_container)) == req_size);

    audio_buffer_index = 0;
    audio_buffer_size = req_size;
    audio_buffer_total_size = req_size;

    return true;
}

bool Simulacrum::AV::Core::VideoReader::DecodeVideoFrame()
{
    if (video_stream.flush_requested)
    {
        // Handle video stream flush requests
        video_stream.flush_requested = false;
        avcodec_flush_buffers(video_stream.codec_ctx);
    }

    AVPacket* next_packet_raw;
    if (!video_stream.packet_queue->Pop(next_packet_raw))
    {
        return false;
    }

    // Set up the packet to be disposed at the end of the scope
    const std::shared_ptr<AVPacket*> next_packet(&next_packet_raw, av_packet_free);

    int result = avcodec_send_packet(video_stream.codec_ctx, *next_packet);
    if (result < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Error submitting packet for decoding: %s", av_make_error(result));
        return false;
    }

    result = avcodec_receive_frame(video_stream.codec_ctx, &video_stream.current_frame);
    if (result < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Error decoding frame: %s", av_make_error(result));
        return false;
    }

    return true;
}

int Simulacrum::AV::Core::VideoReader::SeekAudioFrameInternal()
{
    const auto audio_seek_frame = static_cast<int64_t>(audio_stream.seek_pts / av_q2d(audio_stream.time_base));
    const auto result = av_seek_frame(av_format_ctx, audio_stream.stream_index, audio_seek_frame,
                                      audio_stream.seek_flags);
    if (result < 0)
    {
        return result;
    }

    audio_stream.packet_queue->Flush();
    audio_stream.flush_requested = true;

    return result;
}

int Simulacrum::AV::Core::VideoReader::SeekVideoFrameInternal()
{
    const auto video_seek_frame = static_cast<int64_t>(video_stream.seek_pts / av_q2d(video_stream.time_base));
    const auto result = av_seek_frame(av_format_ctx, video_stream.stream_index, video_seek_frame,
                                      video_stream.seek_flags);
    if (result < 0)
    {
        return result;
    }

    video_stream.packet_queue->Flush();
    video_stream.flush_requested = true;

    return result;
}

bool Simulacrum::AV::Core::VideoReader::CopyResampledAudio(uint8_t* audio_buffer, int& samples_read) const
{
    // Resample the audio into our expected format
    uint8_t* out_data[8] = {audio_buffer, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr};
    const auto sample_count = swr_convert(swr_resampler_ctx, out_data, audio_stream.current_frame.nb_samples,
                                          const_cast<const uint8_t**>(audio_stream.current_frame.data),
                                          audio_stream.current_frame.nb_samples);
    if (sample_count < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not resample audio samples");
        return false;
    }

    samples_read = sample_count;

    return true;
}

void Simulacrum::AV::Core::VideoReader::CopyScaledVideo(uint8_t* frame_buffer) const
{
    uint8_t* dest[4] = {frame_buffer, nullptr, nullptr, nullptr};
    const int dest_linesize[4] = {width * 4, 0, 0, 0};

    // Rescale the frame into our expected format
    sws_scale(sws_scaler_ctx, video_stream.current_frame.data, video_stream.current_frame.linesize, 0,
              video_stream.current_frame.height, dest, dest_linesize);
}

bool Simulacrum::AV::Core::VideoReader::InitializeAudioResampler()
{
    swr_resampler_ctx = swr_alloc();
    if (!swr_resampler_ctx)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not allocate swr context");
        return false;
    }

    AVChannelLayout out_ch_layout;
    av_channel_layout_default(&out_ch_layout, audio_channel_count);
    swr_alloc_set_opts2(&swr_resampler_ctx, &out_ch_layout, out_sample_format, sample_rate,
                        &audio_stream.codec_ctx->ch_layout, audio_stream.codec_ctx->sample_fmt,
                        audio_stream.codec_ctx->sample_rate, audio_stream.codec_ctx->log_level_offset, nullptr);
    swr_init(swr_resampler_ctx);

    if (!swr_is_initialized(swr_resampler_ctx))
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not initialize swr context");
        return false;
    }

    return true;
}

bool Simulacrum::AV::Core::VideoReader::InitializeVideoScaler()
{
    const auto source_pix_fmt = correct_for_deprecated_pixel_format(video_stream.codec_ctx->pix_fmt);
    sws_scaler_ctx = sws_getContext(width, height, source_pix_fmt,
                                    width, height, AV_PIX_FMT_BGRA,
                                    SWS_BILINEAR, nullptr, nullptr, nullptr);
    if (!sws_scaler_ctx)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not allocate sws context");
        return false;
    }

    return true;
}

void Simulacrum::AV::Core::VideoReader::Ingest()
{
    AVPacket* packet = nullptr;

    while (!done)
    {
        if (!packet)
        {
            packet = av_packet_alloc();
            if (!packet)
            {
                av_log(nullptr, AV_LOG_ERROR, "[user] Could not allocate packet");
                break;
            }
        }

        if (audio_stream.seek_requested)
        {
            if (const auto result = SeekAudioFrameInternal(); result < 0)
            {
                av_log(nullptr, AV_LOG_ERROR, "[user] Could not seek audio stream: %s", av_make_error(result));
            }

            audio_stream.seek_requested = false;
        }

        if (video_stream.seek_requested)
        {
            if (const auto result = SeekVideoFrameInternal(); result < 0)
            {
                av_log(nullptr, AV_LOG_ERROR, "[user] Could not seek video stream: %s", av_make_error(result));
            }

            video_stream.seek_requested = false;
        }

        if (av_read_frame(av_format_ctx, packet) < 0)
        {
            // No more packets to read right now, but seeking could change that
            continue;
        }

        if (packet->stream_index == video_stream.stream_index)
        {
            video_stream.packet_queue->Push(packet);
            packet = nullptr;
        }
        else if (packet->stream_index == audio_stream.stream_index)
        {
            audio_stream.packet_queue->Push(packet);
            packet = nullptr;
        }
        else
        {
            av_packet_unref(packet);
        }
    }

    // Free the packet if it hasn't already been freed
    av_packet_free(&packet);
}
