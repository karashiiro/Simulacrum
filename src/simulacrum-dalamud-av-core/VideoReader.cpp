#include <string>
#include "VideoReader.h"

#include <windows.h>

// Ripped from https://github.com/bmewj/video-app

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
      time_base{},
      av_format_ctx{},
      av_codec_ctx{},
      video_stream_index(-1),
      av_frame{},
      av_packet{},
      sws_scaler_ctx{}
{
}

bool Simulacrum::AV::Core::VideoReader::Open(const char* filename)
{
    // Open the file using libavformat
    av_format_ctx = avformat_alloc_context();
    if (!av_format_ctx)
    {
        return false;
    }

    if (avformat_open_input(&av_format_ctx, filename, nullptr, nullptr) != 0)
    {
        return false;
    }

    // Find the first valid video stream inside the file
    video_stream_index = -1;
    const AVCodecParameters* av_codec_params = nullptr;
    const AVCodec* av_codec = nullptr;
    for (unsigned i = 0; i < av_format_ctx->nb_streams; ++i)
    {
        av_codec_params = av_format_ctx->streams[i]->codecpar;
        av_codec = const_cast<AVCodec*>(avcodec_find_decoder(av_codec_params->codec_id));
        if (!av_codec)
        {
            continue;
        }
        if (av_codec_params->codec_type == AVMEDIA_TYPE_VIDEO)
        {
            video_stream_index = static_cast<int>(i);
            width = av_codec_params->width;
            height = av_codec_params->height;
            time_base = av_format_ctx->streams[i]->time_base;
            break;
        }
    }
    if (video_stream_index == -1 || !av_codec || !av_codec_params)
    {
        return false;
    }

    // Set up a codec context for the decoder
    av_codec_ctx = avcodec_alloc_context3(av_codec);
    if (!av_codec_ctx)
    {
        return false;
    }
    if (avcodec_parameters_to_context(av_codec_ctx, av_codec_params) < 0)
    {
        return false;
    }
    if (avcodec_open2(av_codec_ctx, av_codec, nullptr) < 0)
    {
        return false;
    }

    av_frame = av_frame_alloc();
    if (!av_frame)
    {
        return false;
    }
    av_packet = av_packet_alloc();
    if (!av_packet)
    {
        return false;
    }

    return true;
}

bool Simulacrum::AV::Core::VideoReader::ReadFrame(uint8_t* frame_buffer, int64_t* pts)
{
    // Decode one frame
    while (av_read_frame(av_format_ctx, av_packet) >= 0)
    {
        if (av_packet->stream_index != video_stream_index)
        {
            av_packet_unref(av_packet);
            continue;
        }

        int response = avcodec_send_packet(av_codec_ctx, av_packet);
        if (response < 0)
        {
            return false;
        }

        response = avcodec_receive_frame(av_codec_ctx, av_frame);
        if (response == AVERROR(EAGAIN) || response == AVERROR_EOF)
        {
            av_packet_unref(av_packet);
            continue;
        }

        if (response < 0)
        {
            return false;
        }

        av_packet_unref(av_packet);
        break;
    }

    *pts = av_frame->pts;

    // Set up sws scaler
    if (!sws_scaler_ctx)
    {
        const auto source_pix_fmt = correct_for_deprecated_pixel_format(av_codec_ctx->pix_fmt);
        sws_scaler_ctx = sws_getContext(width, height, source_pix_fmt,
                                        width, height, AV_PIX_FMT_BGRA,
                                        SWS_BILINEAR, nullptr, nullptr, nullptr);
    }
    if (!sws_scaler_ctx)
    {
        return false;
    }

    uint8_t* dest[4] = {frame_buffer, nullptr, nullptr, nullptr};
    int dest_linesize[4] = {width * 4, 0, 0, 0};
    sws_scale(sws_scaler_ctx, av_frame->data, av_frame->linesize, 0, av_frame->height, dest, dest_linesize);

    return true;
}

bool Simulacrum::AV::Core::VideoReader::SeekFrame(const int64_t ts) const
{
    av_seek_frame(av_format_ctx, video_stream_index, ts, AVSEEK_FLAG_BACKWARD);

    // av_seek_frame takes effect after one frame, so I'm decoding one here
    // so that the next call to video_reader_read_frame() will give the correct
    // frame
    while (av_read_frame(av_format_ctx, av_packet) >= 0)
    {
        if (av_packet->stream_index != video_stream_index)
        {
            av_packet_unref(av_packet);
            continue;
        }

        int response = avcodec_send_packet(av_codec_ctx, av_packet);
        if (response < 0)
        {
            printf("Failed to decode packet: %s\n", av_make_error(response));
            return false;
        }

        response = avcodec_receive_frame(av_codec_ctx, av_frame);
        if (response == AVERROR(EAGAIN) || response == AVERROR_EOF)
        {
            av_packet_unref(av_packet);
            continue;
        }
        if (response < 0)
        {
            printf("Failed to decode packet: %s\n", av_make_error(response));
            return false;
        }

        av_packet_unref(av_packet);
        break;
    }

    return true;
}

void Simulacrum::AV::Core::VideoReader::Close()
{
    if (sws_scaler_ctx)
    {
        sws_freeContext(sws_scaler_ctx);
        sws_scaler_ctx = nullptr;
    }
    if (av_format_ctx)
    {
        avformat_close_input(&av_format_ctx);
        avformat_free_context(av_format_ctx);
        av_format_ctx = nullptr;
    }
    if (av_frame)
    {
        av_frame_free(&av_frame);
        av_frame = nullptr;
    }
    if (av_packet)
    {
        av_packet_free(&av_packet);
        av_packet = nullptr;
    }
    if (av_codec_ctx)
    {
        avcodec_free_context(&av_codec_ctx);
        av_codec_ctx = nullptr;
    }
}

int Simulacrum::AV::Core::VideoReader::GetWidth() const
{
    return width;
}

int Simulacrum::AV::Core::VideoReader::GetHeight() const
{
    return height;
}

AVRational Simulacrum::AV::Core::VideoReader::GetTimeBase() const
{
    return time_base;
}
