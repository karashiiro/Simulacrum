import { Logger } from '@nestjs/common';
import {
  MessageBody,
  OnGatewayConnection,
  OnGatewayDisconnect,
  SubscribeMessage,
  WebSocketGateway,
  WebSocketServer,
  WsException,
  WsResponse,
} from '@nestjs/websockets';
import { DbService } from '@simulacrum/db';
import { MediaSourceDto } from '@simulacrum/db/common';
import { Observable, bufferCount, from, map } from 'rxjs';
import * as WebSocket from 'ws';
import { WebSocketServer as Server } from 'ws';

interface MediaSyncEvent {
  id: string;
}

interface MediaPlayEvent {
  id: string;
}

interface MediaPauseEvent {
  id: string;
}

interface MediaPanEvent {
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

  @SubscribeMessage('MEDIA_SOURCE_LIST')
  async listMediaSources(): Promise<Observable<WsResponse<MediaSourceDto[]>>> {
    const dtos = await this.db.findAllMediaSources();
    return from(dtos).pipe(
      bufferCount(10),
      map((dtos) => ({
        event: 'MEDIA_SOURCE_LIST',
        data: dtos,
      })),
    );
  }

  @SubscribeMessage('MEDIA_SOURCE_CREATE')
  async createMediaSource(): Promise<void> {
    const dto = await this.db.createMediaSource();
    broadcast<MediaSourceDto>(this.wss, {
      event: 'MEDIA_SOURCE_CREATE',
      data: dto,
    });
  }

  @SubscribeMessage('VIDEO_SOURCE_SYNC')
  async syncVideoSource(
    @MessageBody() ev: MediaSyncEvent,
  ): Promise<WsResponse<MediaSourceDto>> {
    const dto = await this.db.findMediaSource(ev.id);
    if (!dto) {
      throw new WsException('Could not find media source.');
    }

    return {
      event: 'VIDEO_SOURCE_SYNC',
      data: dto,
    };
  }

  @SubscribeMessage('VIDEO_SOURCE_PLAY')
  async playMediaSource(@MessageBody() ev: MediaPlayEvent): Promise<void> {
    const dto = await this.db.updateMediaSource(ev.id, {
      meta: {
        type: 'video',
        state: 'playing',
      },
    });
    if (!dto) {
      throw new WsException('Could not find media source.');
    }

    broadcast<MediaSourceDto>(this.wss, {
      event: 'VIDEO_SOURCE_PLAY',
      data: dto,
    });
  }

  @SubscribeMessage('VIDEO_SOURCE_PAUSE')
  async pauseMediaSource(@MessageBody() ev: MediaPauseEvent): Promise<void> {
    const dto = await this.db.updateMediaSource(ev.id, {
      meta: {
        type: 'video',
        state: 'paused',
      },
    });
    if (!dto) {
      throw new WsException('Could not find media source.');
    }

    broadcast<MediaSourceDto>(this.wss, {
      event: 'VIDEO_SOURCE_PAUSE',
      data: dto,
    });
  }

  @SubscribeMessage('VIDEO_SOURCE_PAN')
  async panMediaSource(@MessageBody() ev: MediaPanEvent): Promise<void> {
    const dto = await this.db.updateMediaSource(ev.id, {
      meta: {
        type: 'video',
        playheadSeconds: ev.playheadSeconds,
      },
    });
    if (!dto) {
      throw new WsException('Could not find media source.');
    }

    broadcast<MediaSourceDto>(this.wss, {
      event: 'VIDEO_SOURCE_PAN',
      data: dto,
    });
  }
}
