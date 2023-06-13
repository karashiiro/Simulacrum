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
import { ImageSourceDto, VideoSourceDto } from '@simulacrum/db/common';
import { Observable, bufferCount, from, map } from 'rxjs';
import * as WebSocket from 'ws';
import { WebSocketServer as Server } from 'ws';

interface VideoSyncEvent {
  id: string;
}

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

  @SubscribeMessage('IMAGE_SOURCE_LIST')
  async listImageSources(): Promise<Observable<WsResponse<ImageSourceDto[]>>> {
    const dtos = await this.db.findAllImageSources();
    return from(dtos).pipe(
      bufferCount(10),
      map((dtos) => ({
        event: 'IMAGE_SOURCE_LIST',
        data: dtos,
      })),
    );
  }

  @SubscribeMessage('IMAGE_SOURCE_CREATE')
  async createImageSource(): Promise<void> {
    const dto = await this.db.createImageSource();
    broadcast<ImageSourceDto>(this.wss, {
      event: 'IMAGE_SOURCE_CREATE',
      data: dto,
    });
  }

  @SubscribeMessage('VIDEO_SOURCE_SYNC')
  async syncVideoSource(
    @MessageBody() ev: VideoSyncEvent,
  ): Promise<
    WsResponse<Pick<VideoSourceDto, 'id' | 'playheadSeconds' | 'updatedAt'>>
  > {
    const dto = await this.db.findVideoSource(ev.id);
    if (!dto) {
      throw new WsException('Could not find video source.');
    }

    return {
      event: 'VIDEO_SOURCE_SYNC',
      data: {
        id: dto.id,
        playheadSeconds: dto.playheadSeconds,
        updatedAt: dto.updatedAt,
      },
    };
  }

  @SubscribeMessage('VIDEO_SOURCE_LIST')
  async listVideoSources(): Promise<Observable<WsResponse<VideoSourceDto[]>>> {
    const dtos = await this.db.findAllVideoSources();
    return from(dtos).pipe(
      bufferCount(10),
      map((dtos) => ({
        event: 'VIDEO_SOURCE_LIST',
        data: dtos,
      })),
    );
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
      throw new WsException('Could not find video source.');
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
      throw new WsException('Could not find video source.');
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
      throw new WsException('Could not find video source.');
    }

    broadcast<VideoSourceDto>(this.wss, {
      event: 'VIDEO_SOURCE_PAN',
      data: dto,
    });
  }
}
