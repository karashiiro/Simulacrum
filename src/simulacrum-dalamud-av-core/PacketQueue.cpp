#include "PacketQueue.h"

PacketQueue::PacketQueue(): total_packet_size(0)
{
}

void PacketQueue::Push(AVPacket* packet)
{
    const std::lock_guard lock(mtx);
    packets.push(packet);
    total_packet_size += packet->size;
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
    total_packet_size -= next_packet->size;
    *packet = next_packet;
    return true;
}

int PacketQueue::TotalPacketSize() const
{
    return total_packet_size;
}
