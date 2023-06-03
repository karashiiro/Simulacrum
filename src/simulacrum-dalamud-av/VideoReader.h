#pragma once

#include "pch.h"

// ReSharper disable CppInconsistentNaming

using namespace System;

namespace Simulacrum
{
    namespace AV
    {
        public class VideoReaderInternal sealed
        {
        public:
            int width, height;
            AVRational time_base;

            VideoReaderInternal();

            bool Open(const char* filename);
            bool ReadFrame(uint8_t* frame_buffer, int64_t* pts);
            bool SeekFrame(int64_t ts);
            void Close();

        private:
            AVFormatContext* av_format_ctx;
            AVCodecContext* av_codec_ctx;
            int video_stream_index;
            AVFrame* av_frame;
            AVPacket* av_packet;
            SwsContext* sws_scaler_ctx;
        };

        public ref class VideoReader sealed
        {
        public:
            VideoReader();
            ~VideoReader();

            bool Open(String^ filename);
            bool ReadFrame(uint8_t* frame_buffer, int64_t* pts);
            bool SeekFrame(int64_t ts);
            void Close();

        protected:
            !VideoReader();

        private:
            VideoReaderInternal* reader;
        };
    }
}
