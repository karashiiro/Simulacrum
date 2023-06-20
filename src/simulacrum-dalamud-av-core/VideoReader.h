#pragma once
#include "PacketQueue.h"

extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libswscale/swscale.h>
}

#define DllExport __declspec(dllexport)

namespace Simulacrum::AV::Core
{
    class VideoReader
    {
    public:
        int width, height;
        AVRational time_base;

        VideoReader();
        ~VideoReader();

        bool Open(const char* uri);
        int ReadAudioStream(uint8_t* audio_buffer, int len);
        bool ReadFrame(uint8_t* frame_buffer, int64_t* pts);
        [[nodiscard]] bool SeekFrame(int64_t ts) const;
        void Close();

    private:
        PacketQueue* audio_packet_queue;
        PacketQueue* video_packet_queue;
        uint8_t* audio_buffer_pending;
        int audio_buffer_size;
        int audio_buffer_index;
        AVFrame audio_frame;
        AVFrame video_frame;
        std::thread ingest_thread;
        bool done;

        AVFormatContext* av_format_ctx;
        AVCodecContext* audio_codec_ctx;
        AVCodecContext* video_codec_ctx;
        int audio_stream_index;
        int video_stream_index;
        SwsContext* sws_scaler_ctx;

        bool DecodeAudioFrame();
        void Ingest() const;
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
    const int len)
{
    return reader->ReadAudioStream(audio_buffer, len);
}

inline DllExport bool VideoReaderReadFrame(
    Simulacrum::AV::Core::VideoReader* reader,
    uint8_t* frame_buffer,
    int64_t* pts)
{
    return reader->ReadFrame(frame_buffer, pts);
}

inline DllExport bool VideoReaderSeekFrame(const Simulacrum::AV::Core::VideoReader* reader, const int64_t ts)
{
    return reader->SeekFrame(ts);
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

inline DllExport AVRational VideoReaderGetTimeBase(const Simulacrum::AV::Core::VideoReader* reader)
{
    return reader->time_base;
}
}
