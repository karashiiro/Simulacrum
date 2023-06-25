#pragma once

#include <mutex>
#include <queue>

extern "C" {
#include <libavformat/avformat.h>
}

class PacketQueue
{
public:
    ~PacketQueue();

    void Push(AVPacket* packet);
    bool Pop(AVPacket** packet);
    void Flush();
    size_t Size();

private:
    std::queue<AVPacket*> packets;
    std::mutex mtx;
};
