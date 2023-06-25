#pragma once
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

        VideoReader();
        ~VideoReader();

        /**
         * \brief Opens a video file. This may be a local file or a network object.
         * \param uri The URI of the file to open.
         * \return `true` if the file was opened successfully; otherwise `false`.
         */
        bool Open(const char* uri);

        /**
         * \brief Reads data from the audio stream into the provided buffer.
         * \param audio_buffer The buffer to read audio data into.
         * \param len The length of the buffer. The function will try to write this many bytes into the buffer.
         * \param pts The timestamp of the beginning of the output data.
         * \return The number of bytes that were written into the buffer.
         */
        int ReadAudioStream(uint8_t* audio_buffer, int len, double& pts);

        /**
         * \brief Reads a video frame from the file. This always reads at least one video frame.
         * \param frame_buffer The buffer to read frame data into. It must have width * height * pixel_size elements.
         * \param target_pts The timestamp to read frame data at. This is a "best effort" parameter.
         * \param pts The timestamp of the actual frame that was read.
         * \return `true` if a frame was read successfully; otherwise `false`.
         */
        bool ReadVideoFrame(uint8_t* frame_buffer, const double& target_pts, double& pts);

        /**
         * \brief Seeks to the specified position in the file's audio stream. Note that not all stream
         * formats support seeking in one or both directions.
         * \param target_pts The timestamp, in seconds, of the position to seek to.
         * \return `true` if the seek operation completed successfully; otherwise `false`.
         */
        bool SeekAudioStream(double target_pts);

        /**
         * \brief Seeks to the specified video frame in the file. Note that not all stream formats
         * support seeking in one or both directions.
         * \param target_pts The timestamp, in seconds, of the frame to seek to.
         * \return `true` if the seek operation completed successfully; otherwise `false`.
         */
        bool SeekVideoFrame(double target_pts);

        /**
         * \brief Closes the current file and releases all resources associated with it.
         */
        void Close();

    private:
        struct StreamInfo
        {
            PacketQueue* packet_queue;
            AVCodecContext* codec_ctx;
            AVFrame current_frame;
            AVRational time_base;
            int stream_index = -1;
            double seek_pts;
            bool seek_requested;
            int seek_flags;
            bool flush_requested;
        };

        StreamInfo audio_stream;
        StreamInfo video_stream;
        uint8_t* audio_buffer_pending;
        int audio_buffer_total_size;
        int audio_buffer_size;
        int audio_buffer_index;
        int64_t video_last_frame_timestamp;
        std::thread ingest_thread;
        bool done;

        AVFormatContext* av_format_ctx;
        SwsContext* sws_scaler_ctx;
        SwrContext* swr_resampler_ctx;

        /**
         * \brief Finds the decoder associated with the specified stream.
         * \param stream_index The index of the stream to find a decoder for, relative to the format context.
         * \param codec_params A pointer to the output codec parameters. This will be overwritten.
         * \param codec A pointer to the output codec. This will be overwritten.
         * \return `true` if the operation completed successfully; otherwise `false`.
         */
        bool FindDecoder(int stream_index, const AVCodecParameters*& codec_params, const AVCodec*& codec) const;

        /**
         * \brief Initializes a codec context.
         * \param codec_ctx A pointer to the output codec context. This will be overwritten.
         * \param codec_params The codec parameters.
         * \param codec The codec itself.
         * \return `true` if the operation completed successfully; otherwise `false`.
         */
        static bool InitializeCodecContext(AVCodecContext*& codec_ctx, const AVCodecParameters& codec_params,
                                           const AVCodec& codec);

        /**
         * \brief Decodes the next audio frame.
         * \return `true` if the operation completed successfully; otherwise `false`.
         */
        bool DecodeAudioFrame();

        /**
         * \brief Decodes the next video frame.
         * \return `true` if the operation completed successfully; otherwise `false`.
         */
        bool DecodeVideoFrame();

        bool SeekAudioFrameInternal();
        bool SeekVideoFrameInternal();

        /**
         * \brief Resamples the current audio frame data and copies it into the provided output buffer.
         * \param audio_buffer The buffer to write output samples into.
         * \param samples_read The number of samples that were read.
         * \return `true` if the operation completed successfully; otherwise `false`.
         */
        bool CopyResampledAudio(uint8_t* audio_buffer, int& samples_read) const;

        /**
         * \brief Scales the current video frame data and copies it into the provided output buffer.
         * \param frame_buffer The frame buffer to write output data into. It must support
         * width * height * pixel_size elements.
         */
        void CopyScaledVideo(uint8_t* frame_buffer) const;

        /**
         * \brief Initializes the audio resampler context.
         * \return `true` if the resampler context was initialized successfully; otherwise `false`.
         */
        bool InitializeAudioResampler();

        /**
         * \brief Initializes the video scaler context.
         * \return `true` if the scaler context was initialized successfully; otherwise `false`.
         */
        bool InitializeVideoScaler();

        /**
         * \brief Ingests data from the underlying streams into this instance's internal buffers.
         */
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
    double& pts)
{
    return reader->ReadAudioStream(audio_buffer, len, pts);
}

inline DllExport bool VideoReaderReadVideoFrame(
    Simulacrum::AV::Core::VideoReader* reader,
    uint8_t* frame_buffer,
    const double& target_pts,
    double& pts)
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
