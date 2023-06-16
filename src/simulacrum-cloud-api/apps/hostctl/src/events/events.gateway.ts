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

interface MediaCreateEvent {
  mediaSource: Omit<MediaSourceDto, 'id' | 'updatedAt'>;
}

interface VideoSourceSyncRequest {
  id: string;
}

interface VideoSourcePlayRequest {
  id: string;
}

interface VideoSourcePauseRequest {
  id: string;
}

interface VideoSourcePanRequest {
  id: string;
  playheadSeconds: number;
}

interface MediaSourceListResponse {
  mediaSources: MediaSourceDto[];
}

interface MediaSourceCreateBroadcast {
  mediaSource: MediaSourceDto;
}

interface VideoSourceSyncResponse {
  mediaSource: MediaSourceDto;
}

interface VideoSourcePlayBroadcast {
  mediaSource: MediaSourceDto;
}

interface VideoSourcePauseBroadcast {
  mediaSource: MediaSourceDto;
}

interface VideoSourcePanBroadcast {
  mediaSource: MediaSourceDto;
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
  async listMediaSources(): Promise<
    Observable<WsResponse<MediaSourceListResponse>>
  > {
    const dtos = await this.db.findAllMediaSources();
    return from(dtos).pipe(
      bufferCount(10),
      map((dtos) => ({
        event: 'MEDIA_SOURCE_LIST',
        data: {
          mediaSources: dtos,
        },
      })),
    );
  }

  @SubscribeMessage('MEDIA_SOURCE_CREATE')
  async createMediaSource(@MessageBody() ev: MediaCreateEvent): Promise<void> {
    const dto = await this.db.createMediaSource(ev.mediaSource);
    broadcast<MediaSourceCreateBroadcast>(this.wss, {
      event: 'MEDIA_SOURCE_CREATE',
      data: {
        mediaSource: dto,
      },
    });
  }

  @SubscribeMessage('VIDEO_SOURCE_SYNC')
  async syncVideoSource(
    @MessageBody() ev: VideoSourceSyncRequest,
  ): Promise<WsResponse<VideoSourceSyncResponse>> {
    const dto = await this.db.findMediaSource(ev.id);
    if (!dto) {
      throw new WsException('Could not find media source.');
    }

    return {
      event: 'VIDEO_SOURCE_SYNC',
      data: {
        mediaSource: dto,
      },
    };
  }

  @SubscribeMessage('VIDEO_SOURCE_PLAY')
  async playMediaSource(
    @MessageBody() ev: VideoSourcePlayRequest,
  ): Promise<void> {
    const dto = await this.db.updateMediaSource(ev.id, {
      meta: {
        type: 'video',
        state: 'playing',
      },
    });
    if (!dto) {
      throw new WsException('Could not find media source.');
    }

    broadcast<VideoSourcePlayBroadcast>(this.wss, {
      event: 'VIDEO_SOURCE_PLAY',
      data: {
        mediaSource: dto,
      },
    });
  }

  @SubscribeMessage('VIDEO_SOURCE_PAUSE')
  async pauseMediaSource(
    @MessageBody() ev: VideoSourcePauseRequest,
  ): Promise<void> {
    const dto = await this.db.updateMediaSource(ev.id, {
      meta: {
        type: 'video',
        state: 'paused',
      },
    });
    if (!dto) {
      throw new WsException('Could not find media source.');
    }

    broadcast<VideoSourcePauseBroadcast>(this.wss, {
      event: 'VIDEO_SOURCE_PAUSE',
      data: {
        mediaSource: dto,
      },
    });
  }

  @SubscribeMessage('VIDEO_SOURCE_PAN')
  async panMediaSource(
    @MessageBody() ev: VideoSourcePanRequest,
  ): Promise<void> {
    const dto = await this.db.updateMediaSource(ev.id, {
      meta: {
        type: 'video',
        playheadSeconds: ev.playheadSeconds,
      },
    });
    if (!dto) {
      throw new WsException('Could not find media source.');
    }

    broadcast<VideoSourcePanBroadcast>(this.wss, {
      event: 'VIDEO_SOURCE_PAN',
      data: {
        mediaSource: dto,
      },
    });
  }
}
