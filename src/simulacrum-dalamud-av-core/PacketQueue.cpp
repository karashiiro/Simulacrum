#include "PacketQueue.h"

PacketQueue::~PacketQueue()
{
    Flush();
}

void PacketQueue::Push(AVPacket* packet)
{
    const std::lock_guard lock(mtx);
    packets.push(packet);
}

bool PacketQueue::Pop(AVPacket** packet)
{
    const std::lock_guard lock(mtx);
    if (packets.empty())
    {
        return false;
    }

    auto* next_packet = packets.front();
    packets.pop();
    *packet = next_packet;
    return true;
}

void PacketQueue::Flush()
{
    const std::lock_guard lock(mtx);
    while (!packets.empty())
    {
        auto* packet = packets.front();
        av_packet_free(&packet);
        packets.pop();
    }
}

size_t PacketQueue::Size()
{
    const std::lock_guard lock(mtx);
    return packets.size();
}
