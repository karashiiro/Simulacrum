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

Simulacrum::AV::Core::VideoReader::VideoReader()
    : width{},
      height{},
      sample_rate{},
      bits_per_sample{},
      audio_channel_count{},
      supports_audio{},
      time_base{},
      audio_buffer_size{},
      audio_buffer_index{},
      audio_frame{},
      video_frame{},
      done{},
      av_format_ctx{},
      audio_codec_ctx{},
      video_codec_ctx{},
      audio_stream_index(-1),
      video_stream_index(-1),
      sws_scaler_ctx{},
      swr_resampler_ctx{}
{
    audio_packet_queue = new PacketQueue();
    video_packet_queue = new PacketQueue();
    audio_buffer_pending = new uint8_t[max_audio_buffer_size];
    memset(audio_buffer_pending, 0, max_audio_buffer_size);
}

Simulacrum::AV::Core::VideoReader::~VideoReader()
{
    delete audio_packet_queue;
    delete video_packet_queue;
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
    audio_stream_index = av_find_best_stream(av_format_ctx, AVMEDIA_TYPE_AUDIO, -1, -1, nullptr, 0);
    if (audio_stream_index < 0)
    {
        // TODO: Soundless video files are valid but need special handling here
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not find audio stream in input file");
        return false;
    }

    supports_audio = audio_stream_index != -1;

    // Find the first valid video stream inside the file
    video_stream_index = av_find_best_stream(av_format_ctx, AVMEDIA_TYPE_VIDEO, -1, -1, nullptr, 0);
    if (video_stream_index < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not find video stream in input file");
        return false;
    }

    const auto* audio_codec_params = av_format_ctx->streams[audio_stream_index]->codecpar;
    const auto* audio_codec = const_cast<AVCodec*>(avcodec_find_decoder(audio_codec_params->codec_id));
    if (!audio_codec)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not find audio decoder");
        return false;
    }

    const auto* video_codec_params = av_format_ctx->streams[video_stream_index]->codecpar;
    const auto* video_codec = const_cast<AVCodec*>(avcodec_find_decoder(video_codec_params->codec_id));
    if (!video_codec)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not find video decoder");
        return false;
    }

    if (supports_audio)
    {
        sample_rate = audio_codec_params->sample_rate;
        bits_per_sample = out_bits_per_sample;
        audio_channel_count = out_audio_channels;
    }

    width = video_codec_params->width;
    height = video_codec_params->height;
    time_base = av_format_ctx->streams[video_stream_index]->time_base;

    // Set up a codec context for the audio decoder
    audio_codec_ctx = avcodec_alloc_context3(audio_codec);
    if (!audio_codec_ctx)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not allocate audio decoder context");
        return false;
    }

    if (avcodec_parameters_to_context(audio_codec_ctx, audio_codec_params) < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not copy audio decoder context");
        return false;
    }

    if (avcodec_open2(audio_codec_ctx, audio_codec, nullptr) < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not open audio decoder");
        return false;
    }

    // Set up a codec context for the video decoder
    video_codec_ctx = avcodec_alloc_context3(video_codec);
    if (!video_codec_ctx)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not allocate video decoder context");
        return false;
    }

    if (avcodec_parameters_to_context(video_codec_ctx, video_codec_params) < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not copy video decoder context");
        return false;
    }

    if (avcodec_open2(video_codec_ctx, video_codec, nullptr) < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not open video decoder");
        return false;
    }

    ingest_thread = std::thread(&VideoReader::Ingest, this);

    return true;
}

int Simulacrum::AV::Core::VideoReader::ReadAudioStream(uint8_t* audio_buffer, const int len)
{
    int n_read = 0;
    while (n_read < len)
    {
        if (audio_buffer_size == 0 && !DecodeAudioFrame())
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

bool Simulacrum::AV::Core::VideoReader::DecodeAudioFrame()
{
    AVPacket* next_packet_raw;
    if (!audio_packet_queue->Pop(&next_packet_raw))
    {
        return false;
    }

    const std::shared_ptr<AVPacket*> next_packet(&next_packet_raw, av_packet_free);

    int result = avcodec_send_packet(audio_codec_ctx, *next_packet);
    if (result < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Error submitting packet for decoding: %s", av_make_error(result));
        return false;
    }

    result = avcodec_receive_frame(audio_codec_ctx, &audio_frame);
    if (result < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Error decoding frame: %s", av_make_error(result));
        return false;
    }

    if (!swr_resampler_ctx)
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
                            &audio_codec_ctx->ch_layout, audio_codec_ctx->sample_fmt, audio_codec_ctx->sample_rate,
                            audio_codec_ctx->log_level_offset, nullptr);
        swr_init(swr_resampler_ctx);

        if (!swr_is_initialized(swr_resampler_ctx))
        {
            av_log(nullptr, AV_LOG_ERROR, "[user] Could not initialize swr context");
            return false;
        }
    }

    const auto req_size = av_samples_get_buffer_size(nullptr, audio_channel_count, audio_frame.nb_samples,
                                                     out_sample_format, 1);
    if (req_size < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not get required audio buffer size");
        return false;
    }

    assert(max_audio_buffer_size - audio_buffer_size >= req_size);

    uint8_t* out_data[8] = {audio_buffer_pending, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr};
    const auto sample_count = swr_convert(swr_resampler_ctx, out_data, audio_frame.nb_samples,
                                          const_cast<const uint8_t**>(audio_frame.data), audio_frame.nb_samples);
    if (sample_count < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not rescale audio samples");
        return false;
    }

    assert(audio_channel_count * sample_count * static_cast<int>(sizeof(sample_container)) == req_size);

    audio_buffer_index = 0;
    audio_buffer_size = req_size;

    return true;
}

bool Simulacrum::AV::Core::VideoReader::ReadFrame(uint8_t* frame_buffer, const double* target_pts, double* pts)
{
    double pts_pending;
    do
    {
        AVPacket* next_packet_raw;
        if (!video_packet_queue->Pop(&next_packet_raw))
        {
            return false;
        }

        const std::shared_ptr<AVPacket*> next_packet(&next_packet_raw, av_packet_free);

        int result = avcodec_send_packet(video_codec_ctx, *next_packet);
        if (result < 0)
        {
            av_log(nullptr, AV_LOG_ERROR, "[user] Error submitting packet for decoding: %s", av_make_error(result));
            return false;
        }

        result = avcodec_receive_frame(video_codec_ctx, &video_frame);
        if (result < 0)
        {
            av_log(nullptr, AV_LOG_ERROR, "[user] Error decoding frame: %s", av_make_error(result));
            return false;
        }

        pts_pending = static_cast<double>(video_frame.best_effort_timestamp) * av_q2d(time_base);
    }
    while (target_pts != nullptr && pts_pending < *target_pts);

    if (pts)
    {
        *pts = pts_pending;
    }

    if (!sws_scaler_ctx)
    {
        const auto source_pix_fmt = correct_for_deprecated_pixel_format(video_codec_ctx->pix_fmt);
        sws_scaler_ctx = sws_getContext(width, height, source_pix_fmt,
                                        width, height, AV_PIX_FMT_BGRA,
                                        SWS_BILINEAR, nullptr, nullptr, nullptr);
    }
    if (!sws_scaler_ctx)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not allocate sws context");
        return false;
    }

    uint8_t* dest[4] = {frame_buffer, nullptr, nullptr, nullptr};
    int dest_linesize[4] = {width * 4, 0, 0, 0};
    sws_scale(sws_scaler_ctx, video_frame.data, video_frame.linesize, 0, video_frame.height, dest, dest_linesize);

    return true;
}

bool Simulacrum::AV::Core::VideoReader::SeekFrame(const double pts) const
{
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

    if (audio_codec_ctx)
    {
        avcodec_free_context(&audio_codec_ctx);
        audio_codec_ctx = nullptr;
    }

    if (video_codec_ctx)
    {
        avcodec_free_context(&video_codec_ctx);
        video_codec_ctx = nullptr;
    }
}

void Simulacrum::AV::Core::VideoReader::Ingest() const
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

        if (av_read_frame(av_format_ctx, packet) < 0)
        {
            // No more packets to read
            break;
        }

        if (packet->stream_index == video_stream_index)
        {
            video_packet_queue->Push(packet);
            packet = nullptr;
        }
        else if (packet->stream_index == audio_stream_index)
        {
            audio_packet_queue->Push(packet);
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
