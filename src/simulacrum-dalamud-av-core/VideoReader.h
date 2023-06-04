#pragma once

#define DllExport __declspec(dllexport)

extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libswscale/swscale.h>
}

namespace Simulacrum
{
    namespace AV
    {
        namespace Core
        {
            class VideoReader
            {
            public:
                int width, height;
                AVRational time_base;

                VideoReader();

                bool Open(const char* filename);
                bool ReadFrame(uint8_t* frame_buffer, int64_t* pts);
                bool SeekFrame(int64_t ts) const;
                void Close();

            private:
                AVFormatContext* av_format_ctx;
                AVCodecContext* av_codec_ctx;
                int video_stream_index;
                AVFrame* av_frame;
                AVPacket* av_packet;
                SwsContext* sws_scaler_ctx;
            };
        }
    }
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

inline DllExport bool VideoReaderOpen(Simulacrum::AV::Core::VideoReader* reader, const char* filename)
{
    return reader->Open(filename);
}

inline DllExport bool VideoReaderReadFrame(
    Simulacrum::AV::Core::VideoReader* reader,
    uint8_t* frame_buffer,
    int64_t* pts)
{
    return reader->ReadFrame(frame_buffer, pts);
}

inline DllExport bool VideoReaderSeekFrame(Simulacrum::AV::Core::VideoReader* reader, int64_t ts)
{
    return reader->SeekFrame(ts);
}

inline DllExport void VideoReaderClose(Simulacrum::AV::Core::VideoReader* reader)
{
    reader->Close();
}
}
