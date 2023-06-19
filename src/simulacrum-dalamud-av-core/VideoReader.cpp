﻿#include <string>
#include <windows.h>
#include "VideoReader.h"

// Ripped from
// * https://github.com/bmewj/video-app
// * https://ffmpeg.org/doxygen/trunk/api-h264-test_8c_source.html
// * http://dranger.com/ffmpeg/ffmpeg.html

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
      done{},
      av_format_ctx{},
      av_codec_ctx{},
      video_stream_index(-1),
      av_frame{},
      sws_scaler_ctx{}
{
    video_packet_queue = new PacketQueue();
}

Simulacrum::AV::Core::VideoReader::~VideoReader()
{
    delete video_packet_queue;
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

    ingest_thread = std::thread(&VideoReader::Ingest, this);

    return true;
}

bool Simulacrum::AV::Core::VideoReader::ReadFrame(uint8_t* frame_buffer, int64_t* pts)
{
    AVPacket* next_packet_raw;
    if (!video_packet_queue->Pop(&next_packet_raw))
    {
        return false;
    }

    const std::shared_ptr<AVPacket*> next_packet(&next_packet_raw, av_packet_free);

    int result = avcodec_send_packet(av_codec_ctx, *next_packet);
    if (result < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Error submitting packet for decoding: %s", av_make_error(result));
        return false;
    }

    result = avcodec_receive_frame(av_codec_ctx, av_frame);
    if (result < 0)
    {
        av_log(nullptr, AV_LOG_ERROR, "[user] Error decoding frame: %s", av_make_error(result));
        return false;
    }

    *pts = av_frame->pts;

    if (!sws_scaler_ctx)
    {
        const auto source_pix_fmt = correct_for_deprecated_pixel_format(av_codec_ctx->pix_fmt);
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
    sws_scale(sws_scaler_ctx, av_frame->data, av_frame->linesize, 0, av_frame->height, dest, dest_linesize);

    return true;
}

bool Simulacrum::AV::Core::VideoReader::SeekFrame(const int64_t ts) const
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

    if (av_format_ctx)
    {
        avformat_close_input(&av_format_ctx);
        avformat_free_context(av_format_ctx);
        av_format_ctx = nullptr;
    }

    if (av_codec_ctx)
    {
        avcodec_free_context(&av_codec_ctx);
        av_codec_ctx = nullptr;
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
            av_log(nullptr, AV_LOG_INFO, "[user] Pushed packet");
            video_packet_queue->Push(packet);
        }
        else
        {
            av_packet_free(&packet);
        }

        packet = nullptr;
    }

    // Free the packet if it hasn't already been freed
    av_packet_free(&packet);
}
