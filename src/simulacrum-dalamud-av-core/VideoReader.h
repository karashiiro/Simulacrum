﻿#pragma once
#include "PacketQueue.h"

extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libswresample/swresample.h>
#include <libswscale/swscale.h>
}

#define DllExport __declspec(dllexport)

namespace Simulacrum::AV::Core
{
    class VideoReader
    {
    public:
        int width, height, sample_rate, bits_per_sample, audio_channel_count;
        double video_frame_delay;
        bool supports_audio;
        AVRational video_time_base, audio_time_base;

        VideoReader();
        ~VideoReader();

        bool Open(const char* uri);
        int ReadAudioStream(uint8_t* audio_buffer, int len, double* pts);
        bool ReadVideoFrame(uint8_t* frame_buffer, const double* target_pts, double* pts);
        bool SeekAudioStream(double target_pts);
        bool SeekVideoFrame(double target_pts);
        void Close();

    private:
        PacketQueue* audio_packet_queue;
        PacketQueue* video_packet_queue;
        uint8_t* audio_buffer_pending;
        int audio_buffer_total_size;
        int audio_buffer_size;
        int audio_buffer_index;
        int64_t video_last_frame_timestamp;
        AVFrame audio_frame;
        AVFrame video_frame;
        double audio_seek_pts;
        double video_seek_pts;
        bool audio_seek_requested;
        bool video_seek_requested;
        int audio_seek_flags;
        int video_seek_flags;
        bool audio_flush_requested;
        bool video_flush_requested;
        std::thread ingest_thread;
        bool done;

        AVFormatContext* av_format_ctx;
        AVCodecContext* audio_codec_ctx;
        AVCodecContext* video_codec_ctx;
        int audio_stream_index;
        int video_stream_index;
        SwsContext* sws_scaler_ctx;
        SwrContext* swr_resampler_ctx;

        bool DecodeAudioFrame();
        bool DecodeVideoFrame();
        bool SeekAudioFrameInternal();
        bool SeekVideoFrameInternal();
        void Ingest();
    };
}

extern "C" {
inline DllExport Simulacrum::AV::Core::VideoReader* VideoReaderAlloc()
{
    return new Simulacrum::AV::Core::VideoReader();
}

inline DllExport void VideoReaderFree(const Simulacrum::AV::Core::VideoReader* reader)
{
    delete reader;
}

inline DllExport bool VideoReaderOpen(Simulacrum::AV::Core::VideoReader* reader, const char* uri)
{
    return reader->Open(uri);
}

inline DllExport int VideoReaderReadAudioStream(
    Simulacrum::AV::Core::VideoReader* reader,
    uint8_t* audio_buffer,
    const int len,
    double* pts)
{
    return reader->ReadAudioStream(audio_buffer, len, pts);
}

inline DllExport bool VideoReaderReadVideoFrame(
    Simulacrum::AV::Core::VideoReader* reader,
    uint8_t* frame_buffer,
    const double* target_pts,
    double* pts)
{
    return reader->ReadVideoFrame(frame_buffer, target_pts, pts);
}

inline DllExport bool VideoReaderSeekAudioStream(Simulacrum::AV::Core::VideoReader* reader, const double target_pts)
{
    return reader->SeekAudioStream(target_pts);
}

inline DllExport bool VideoReaderSeekVideoFrame(Simulacrum::AV::Core::VideoReader* reader, const double target_pts)
{
    return reader->SeekVideoFrame(target_pts);
}

inline DllExport void VideoReaderClose(Simulacrum::AV::Core::VideoReader* reader)
{
    reader->Close();
}

inline DllExport int VideoReaderGetWidth(const Simulacrum::AV::Core::VideoReader* reader)
{
    return reader->width;
}

inline DllExport int VideoReaderGetHeight(const Simulacrum::AV::Core::VideoReader* reader)
{
    return reader->height;
}

inline DllExport double VideoReaderGetVideoFrameDelay(const Simulacrum::AV::Core::VideoReader* reader)
{
    return reader->video_frame_delay;
}

inline DllExport int VideoReaderGetSampleRate(const Simulacrum::AV::Core::VideoReader* reader)
{
    return reader->sample_rate;
}

inline DllExport int VideoReaderGetBitsPerSample(const Simulacrum::AV::Core::VideoReader* reader)
{
    return reader->bits_per_sample;
}

inline DllExport int VideoReaderGetAudioChannelCount(const Simulacrum::AV::Core::VideoReader* reader)
{
    return reader->audio_channel_count;
}

inline DllExport bool VideoReaderSupportsAudio(const Simulacrum::AV::Core::VideoReader* reader)
{
    return reader->supports_audio;
}
}
