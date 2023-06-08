#pragma once

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

        bool Open(const char* uri);
        bool ReadFrame(uint8_t* frame_buffer, int64_t* pts);
        [[nodiscard]] bool SeekFrame(int64_t ts) const;
        void Close();

        [[nodiscard]] int GetWidth() const;
        [[nodiscard]] int GetHeight() const;
        [[nodiscard]] AVRational GetTimeBase() const;

    private:
        AVFormatContext* av_format_ctx;
        AVCodecContext* av_codec_ctx;
        int video_stream_index;
        AVFrame* av_frame;
        AVPacket* av_packet;
        SwsContext* sws_scaler_ctx;
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
    return reader->GetWidth();
}

inline DllExport int VideoReaderGetHeight(const Simulacrum::AV::Core::VideoReader* reader)
{
    return reader->GetHeight();
}

inline DllExport AVRational VideoReaderGetTimeBase(const Simulacrum::AV::Core::VideoReader* reader)
{
    return reader->GetTimeBase();
}
}
