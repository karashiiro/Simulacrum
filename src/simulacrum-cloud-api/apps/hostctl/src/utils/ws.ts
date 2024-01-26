import type { WsResponse } from "@nestjs/websockets";
import * as WebSocket from "ws";
import { WebSocketServer as Server } from "ws";
import { WsApiGatewayServer } from "../ws-apigw-adapter";

export async function broadcast<T>(
  server: Server | WsApiGatewayServer,
  message: WsResponse<T>
) {
  if (server instanceof WsApiGatewayServer) {
    await Promise.all(
      server.getClients().map((client) => {
        return client.send(JSON.stringify(message));
      })
    );
  } else {
    server.clients.forEach((client) => {
      if (client.readyState === WebSocket.OPEN) {
        client.send(JSON.stringify(message));
      }
    });
  }
}
