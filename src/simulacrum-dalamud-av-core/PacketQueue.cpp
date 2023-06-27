#include "PacketQueue.h"

PacketQueue::~PacketQueue()
{
    Flush();
}

void PacketQueue::Push(AVPacket* packet)
{
    const std::unique_lock lock(mtx);
    packets.push(packet);
}

bool PacketQueue::Pop(AVPacket*& packet)
{
    const std::unique_lock lock(mtx);
    if (packets.empty())
    {
        return false;
    }

    auto* next_packet = packets.front();
    packets.pop();
    packet = next_packet;
    return true;
}

void PacketQueue::Flush()
{
    const std::unique_lock lock(mtx);
    while (!packets.empty())
    {
        auto* packet = packets.front();
        av_packet_free(&packet);
        packets.pop();
    }
}

size_t PacketQueue::Size()
{
    const std::unique_lock lock(mtx);
    return packets.size();
}
