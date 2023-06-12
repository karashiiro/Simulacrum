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
import { MediaSourceDto, PlaybackTrackerDto } from '@simulacrum/db/common';
import * as WebSocket from 'ws';
import { WebSocketServer as Server } from 'ws';

interface PlayEvent {
  id: string;
}

interface PauseEvent {
  id: string;
}

interface PanEvent {
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

  // TODO: MEDIA_SOURCE_LIST (called on connect)
  // TODO: PLAYBACK_TRACKER_SYNC (request/reply, not broadcasted to all clients)
  // TODO: Merge playback trackers and media sources, it makes more sense for video

  @SubscribeMessage('MEDIA_SOURCE_CREATE')
  async createMediaSource(): Promise<void> {
    const dto = await this.db.createMediaSource();
    broadcast<MediaSourceDto>(this.wss, {
      event: 'MEDIA_SOURCE_CREATE',
      data: dto,
    });
  }

  @SubscribeMessage('PLAYBACK_TRACKER_PLAY')
  async play(@MessageBody() ev: PlayEvent): Promise<void> {
    const dto = await this.db.updatePlaybackTracker(ev.id, {
      state: 'playing',
    });
    if (!dto) {
      // TODO: return result type
      return;
    }

    broadcast<PlaybackTrackerDto>(this.wss, {
      event: 'PLAYBACK_TRACKER_PLAY',
      data: dto,
    });
  }

  @SubscribeMessage('PLAYBACK_TRACKER_PAUSE')
  async pause(@MessageBody() ev: PauseEvent): Promise<void> {
    const dto = await this.db.updatePlaybackTracker(ev.id, {
      state: 'paused',
    });
    if (!dto) {
      // TODO: return result type
      return;
    }

    broadcast<PlaybackTrackerDto>(this.wss, {
      event: 'PLAYBACK_TRACKER_PAUSE',
      data: dto,
    });
  }

  @SubscribeMessage('PLAYBACK_TRACKER_PAN')
  async pan(@MessageBody() ev: PanEvent): Promise<void> {
    const dto = await this.db.updatePlaybackTracker(ev.id, {
      playheadSeconds: ev.playheadSeconds,
    });
    if (!dto) {
      // TODO: return result type
      return;
    }

    broadcast<PlaybackTrackerDto>(this.wss, {
      event: 'PLAYBACK_TRACKER_PAN',
      data: dto,
    });
  }
}
