import {
  ApiGatewayManagementApiClient,
  PostToConnectionCommand,
} from "@aws-sdk/client-apigatewaymanagementapi";
import { Logger, WebSocketAdapter, WsMessageHandler } from "@nestjs/common";
import { BlobPayloadInputTypes } from "@smithy/types";
import EventEmitter from "events";
import {
  EMPTY,
  Observable,
  filter,
  first,
  fromEvent,
  mergeMap,
  share,
  takeUntil,
} from "rxjs";

export class WsApiGatewayServer extends EventEmitter {
  private readonly logger = new Logger(WsApiGatewayServer.name);

  private readonly clients = new Map<string, WsApiGatewayClient>();

  getClient(domainName: string, stage: string, connectionId: string) {
    let client = this.clients.get(connectionId);
    if (client) {
      return client;
    }

    this.logger.debug(
      `Creating new client with connection id: ${connectionId}`
    );
    client = new WsApiGatewayClient(domainName, stage, connectionId);
    this.clients.set(connectionId, client);
    this.emit("connection", client);
    return client;
  }

  closeClient(connectionId: string) {
    const client = this.clients.get(connectionId);
    if (!client) {
      return client;
    }

    this.clients.delete(connectionId);

    client.close();
  }
}

export class WsApiGatewayClient extends EventEmitter {
  private readonly logger = new Logger(WsApiGatewayClient.name);

  private readonly client: ApiGatewayManagementApiClient;

  constructor(
    private readonly domainName: string,
    private readonly stage: string,
    private readonly connectionId: string
  ) {
    super();

    this.client = new ApiGatewayManagementApiClient({
      endpoint: this.callbackUrl,
    });
  }

  private get callbackUrl() {
    return `https://${this.domainName}/${this.stage}`;
  }

  async send(data: BlobPayloadInputTypes): Promise<void> {
    const command = new PostToConnectionCommand({
      ConnectionId: this.connectionId,
      Data: data,
    });

    try {
      await this.client.send(command);
    } catch (err) {
      const error = this.unknownAsErr(err);
      this.logger.error(error.message, error.stack);
    }
  }

  close(): void {
    this.client.destroy();
    this.emit("close", 0);
  }

  private unknownAsErr(err: unknown): Error {
    if (err instanceof Error) {
      return err;
    }

    try {
      // Throw the error to populate it with a stack trace
      throw new Error(`${err}`);
    } catch (err) {
      return err as Error;
    }
  }
}

// https://github.com/nestjs/nest/blob/master/packages/websockets/adapters/ws-adapter.ts
export class WsApiGatewayAdapter
  implements WebSocketAdapter<WsApiGatewayServer, WsApiGatewayClient, never>
{
  readonly server: WsApiGatewayServer;

  constructor() {
    this.server = new WsApiGatewayServer();
  }

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  create(_port: number): WsApiGatewayServer {
    return this.server;
  }

  bindClientConnect(
    server: WsApiGatewayServer,
    callback: (...args: any[]) => void
  ) {
    server.on("connection", callback);
  }

  bindClientDisconnect?(
    client: WsApiGatewayClient,
    callback: (...args: any[]) => void
  ) {
    client.on("close", callback);
  }

  bindMessageHandlers(
    client: WsApiGatewayClient,
    handlers: WsMessageHandler<string>[],
    transform: (data: any) => Observable<any>
  ) {
    const close$ = fromEvent(client, "close").pipe(share(), first());
    const source$ = fromEvent(client, "message").pipe(
      mergeMap((data) =>
        this.bindMessageHandler(data, handlers, transform).pipe(
          filter((result) => result)
        )
      ),
      takeUntil(close$)
    );

    const onMessage = (response: any) => {
      client.send(JSON.stringify(response));
    };

    source$.subscribe(onMessage);
  }

  bindMessageHandler(
    buffer: any,
    handlers: WsMessageHandler<string>[],
    transform: (data: any) => Observable<any>
  ) {
    try {
      const message = JSON.parse(buffer.data);

      const messageHandler = handlers.find(
        (handler) => handler.message === message.event
      );
      if (!messageHandler) {
        return EMPTY;
      }

      return transform(messageHandler.callback(message.data));
    } catch {
      return EMPTY;
    }
  }

  // eslint-disable-next-line @typescript-eslint/no-unused-vars, @typescript-eslint/no-empty-function
  close(_server: WsApiGatewayServer) {}
}
