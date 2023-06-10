import { Logger } from '@nestjs/common';
import {
  MessageBody,
  OnGatewayConnection,
  SubscribeMessage,
  WebSocketGateway,
  WebSocketServer,
  WsResponse,
} from '@nestjs/websockets';
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
export class EventsGateway implements OnGatewayConnection {
  @WebSocketServer()
  private readonly wss: Server;

  handleConnection() {
    Logger.log('Got new connection');
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
  pan(@MessageBody() ev: ScreenEvent): void {
    broadcast<ScreenEvent>(this.wss, {
      event: 'pan',
      data: {
        screenId: ev.screenId,
        screenState: 'playing',
      },
    });
  }
}
