import type { WsResponse } from "@nestjs/websockets";
import * as WebSocket from "ws";
import { WebSocketServer as Server } from "ws";

export function broadcast<T>(server: Server, message: WsResponse<T>) {
  server.clients.forEach((client) => {
    if (client.readyState === WebSocket.OPEN) {
      client.send(JSON.stringify(message));
    }
  });
}
