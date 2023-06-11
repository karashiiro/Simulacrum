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
import * as WebSocket from 'ws';
import { WebSocketServer as Server } from 'ws';

type ScreenState = 'playing' | 'paused';

interface ScreenEvent {
  screenId: string;
  screenState: ScreenState;
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

  @SubscribeMessage('play')
  play(@MessageBody() ev: ScreenEvent): void {
    broadcast<ScreenEvent>(this.wss, {
      event: 'play',
      data: {
        screenId: ev.screenId,
        screenState: 'playing',
      },
    });
  }

  @SubscribeMessage('pause')
  pause(@MessageBody() ev: ScreenEvent): void {
    broadcast<ScreenEvent>(this.wss, {
      event: 'pause',
      data: {
        screenId: ev.screenId,
        screenState: 'paused',
      },
    });
  }

  @SubscribeMessage('pan')
  async pan(@MessageBody() ev: ScreenEvent): Promise<void> {
    await this.db.createPlaybackTracker(0);

    broadcast<ScreenEvent>(this.wss, {
      event: 'pan',
      data: {
        screenId: ev.screenId,
        screenState: 'playing',
      },
    });
  }
}
