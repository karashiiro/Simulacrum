#pragma once

#include <mutex>
#include <queue>

extern "C" {
#include <libavformat/avformat.h>
}

class PacketQueue
{
public:
    PacketQueue();

    void Push(AVPacket* packet);
    bool Pop(AVPacket** packet);
    [[nodiscard]] int TotalPacketSize() const;

private:
    std::queue<AVPacket*> packets;
    int total_packet_size;
    std::mutex mtx;
};
