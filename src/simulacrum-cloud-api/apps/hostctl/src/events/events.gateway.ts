import { Logger } from '@nestjs/common';
import {
  MessageBody,
  OnGatewayConnection,
  OnGatewayDisconnect,
  SubscribeMessage,
  WebSocketGateway,
  WebSocketServer,
  WsResponse,
} from '@nestjs/websockets';
import { DbService } from '@simulacrum/db';
import { VideoSourceDto } from '@simulacrum/db/common';
import * as WebSocket from 'ws';
import { WebSocketServer as Server } from 'ws';

interface VideoPlayEvent {
  id: string;
}

interface VideoPauseEvent {
  id: string;
}

interface VideoPanEvent {
  id: string;
  playheadSeconds: number;
}

function broadcast<T>(server: Server, message: WsResponse<T>) {
  server.clients.forEach((client) => {
    if (client.readyState === WebSocket.OPEN) {
      client.send(JSON.stringify(message));
    }
  });
}

@WebSocketGateway()
export class EventsGateway implements OnGatewayConnection, OnGatewayDisconnect {
  private readonly logger = new Logger(EventsGateway.name);

  @WebSocketServer()
  private readonly wss: Server;

  constructor(private readonly db: DbService) {}

  handleConnection() {
    this.logger.log('Got new connection');
  }

  handleDisconnect() {
    this.logger.log('Connection from client ended');
  }

  // TODO: VIDEO_SOURCE_SYNC (request/reply, not broadcasted to all clients)

  @SubscribeMessage('VIDEO_SOURCE_LIST')
  async listVideoSources(): Promise<WsResponse<VideoSourceDto[]>> {
    const dtos = await this.db.findAllVideoSources();
    return {
      event: 'VIDEO_SOURCE_LIST',
      data: dtos,
    };
  }

  @SubscribeMessage('VIDEO_SOURCE_CREATE')
  async createVideoSource(): Promise<void> {
    const dto = await this.db.createVideoSource();
    broadcast<VideoSourceDto>(this.wss, {
      event: 'VIDEO_SOURCE_CREATE',
      data: dto,
    });
  }

  @SubscribeMessage('VIDEO_SOURCE_PLAY')
  async playVideoSource(@MessageBody() ev: VideoPlayEvent): Promise<void> {
    const dto = await this.db.updateVideoSource(ev.id, {
      state: 'playing',
    });
    if (!dto) {
      // TODO: return result type
      return;
    }

    broadcast<VideoSourceDto>(this.wss, {
      event: 'VIDEO_SOURCE_PLAY',
      data: dto,
    });
  }

  @SubscribeMessage('VIDEO_SOURCE_PAUSE')
  async pauseVideoSource(@MessageBody() ev: VideoPauseEvent): Promise<void> {
    const dto = await this.db.updateVideoSource(ev.id, {
      state: 'paused',
    });
    if (!dto) {
      // TODO: return result type
      return;
    }

    broadcast<VideoSourceDto>(this.wss, {
      event: 'VIDEO_SOURCE_PAUSE',
      data: dto,
    });
  }

  @SubscribeMessage('VIDEO_SOURCE_PAN')
  async panVideoSource(@MessageBody() ev: VideoPanEvent): Promise<void> {
    const dto = await this.db.updateVideoSource(ev.id, {
      playheadSeconds: ev.playheadSeconds,
    });
    if (!dto) {
      // TODO: return result type
      return;
    }

    broadcast<VideoSourceDto>(this.wss, {
      event: 'VIDEO_SOURCE_PAN',
      data: dto,
    });
  }
}
