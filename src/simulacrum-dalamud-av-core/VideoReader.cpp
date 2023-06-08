﻿#include <string>
#include <windows.h>
#include "VideoReader.h"

extern "C" {
#include <libavutil/imgutils.h>
}

// Ripped from https://github.com/bmewj/video-app
// and https://ffmpeg.org/doxygen/trunk/api-h264-test_8c_source.html

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
      byte_buffer_size{},
      time_base{},
      av_format_ctx{},
      av_codec_ctx{},
      video_stream_index(-1),
      av_frame{},
      av_packet{},
      sws_scaler_ctx{}
{
}

bool Simulacrum::AV::Core::VideoReader::Open(const char* uri)
{
    av_format_ctx = avformat_alloc_context();
    if (!av_format_ctx)
    {
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

    // Find the first valid video stream inside the file
    video_stream_index = av_find_best_stream(av_format_ctx, AVMEDIA_TYPE_VIDEO, -1, -1, nullptr, 0);
    if (video_stream_index < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not find video stream in input file");
        return false;
    }

    const auto* av_codec_params = av_format_ctx->streams[video_stream_index]->codecpar;
    const auto* av_codec = const_cast<AVCodec*>(avcodec_find_decoder(av_codec_params->codec_id));
    if (!av_codec)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not find decoder");
        return false;
    }

    width = av_codec_params->width;
    height = av_codec_params->height;
    time_base = av_format_ctx->streams[video_stream_index]->time_base;

    // Set up a codec context for the decoder
    av_codec_ctx = avcodec_alloc_context3(av_codec);
    if (!av_codec_ctx)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not allocate decoder context");
        return false;
    }

    if (avcodec_parameters_to_context(av_codec_ctx, av_codec_params) < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not copy decoder context");
        return false;
    }

    if (avcodec_open2(av_codec_ctx, av_codec, nullptr) < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not open decoder");
        return false;
    }

    av_frame = av_frame_alloc();
    if (!av_frame)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not allocate frame");
        return false;
    }

    av_packet = av_packet_alloc();
    if (!av_packet)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Could not allocate packet");
        return false;
    }

    byte_buffer_size = av_image_get_buffer_size(static_cast<AVPixelFormat>(av_codec_params->format), width, height, 16);

    return true;
}

bool Simulacrum::AV::Core::VideoReader::ReadFrame(uint8_t* frame_buffer, const int64_t frame_buffer_size, int64_t* pts)
{
    if (frame_buffer_size < byte_buffer_size)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] The provided buffer is too short to read frame data");
        return false;
    }

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

int Simulacrum::AV::Core::VideoReader::GetRequiredBufferSize() const
{
    return byte_buffer_size;
}

AVRational Simulacrum::AV::Core::VideoReader::GetTimeBase() const
{
    return time_base;
}
