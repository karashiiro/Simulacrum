import { WebSocketServer, WebSocket } from "ws";
import { broadcast } from "./ws";
import { WsResponse } from "@nestjs/websockets";

jest.mock("ws");

const createClients = (readyState: (i: number) => WebSocket["readyState"]) => {
  return new Array(4).fill(undefined).map((_, i) => {
    const client = new WebSocket("/dev/null");
    //@ts-expect-error readonly property
    client.readyState = readyState(i);
    return client;
  });
};

describe("ws", () => {
  let server: WebSocketServer;

  beforeEach(() => {
    jest.resetAllMocks();

    server = new WebSocketServer();
    server.clients = new Set();
  });

  describe("broadcast", () => {
    it("broadcasts messages to all clients with an open connection", async () => {
      // Arrange: Create some open clients
      const clients = createClients(() => WebSocket.OPEN);

      clients.forEach((client) => {
        server.clients.add(client);
      });

      // Act: Broadcast a message
      const message: WsResponse<unknown> = {
        event: "event",
        data: {
          hello: "world",
        },
      };

      await broadcast(server, message);

      // Assert: The message was sent to each client
      for (const client of clients) {
        expect(client.send).toHaveBeenCalledWith(JSON.stringify(message));
      }
    });

    it("does not broadcast messages to clients with closed connections", async () => {
      // Arrange: Create some open and closed clients
      const clients = createClients((i) =>
        i % 2 === 0 ? WebSocket.OPEN : WebSocket.CLOSED
      );

      clients.forEach((client) => {
        server.clients.add(client);
      });

      // Act: Broadcast a message
      const message: WsResponse<unknown> = {
        event: "event",
        data: {
          hello: "world",
        },
      };

      await broadcast(server, message);

      // Assert: The message was sent to each open client
      for (const client of clients) {
        if (client.readyState === WebSocket.OPEN) {
          expect(client.send).toHaveBeenCalledWith(JSON.stringify(message));
        } else {
          expect(client.send).not.toHaveBeenCalled();
        }
      }
    });
  });
});
