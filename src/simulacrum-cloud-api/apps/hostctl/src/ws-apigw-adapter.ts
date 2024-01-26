import {
  ApiGatewayManagementApiClient,
  PostToConnectionCommand,
} from "@aws-sdk/client-apigatewaymanagementapi";
import { Logger, WebSocketAdapter, WsMessageHandler } from "@nestjs/common";
import { WsResponse } from "@nestjs/websockets";
import { BlobPayloadInputTypes } from "@smithy/types";
import EventEmitter from "node:events";
import {
  EMPTY,
  Observable,
  first,
  fromEvent,
  map,
  mergeMap,
  share,
  takeUntil,
} from "rxjs";
import { unknownAsErr } from "./utils/errors";
import { Callback } from "aws-lambda";

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
    this.logger.debug(
      `Sending message to client with connection ID: ${this.connectionId}`
    );

    const command = new PostToConnectionCommand({
      ConnectionId: this.connectionId,
      Data: data,
    });

    try {
      await this.client.send(command);
    } catch (err) {
      const error = unknownAsErr(err);
      this.logger.error(error.message, error.stack);
    }
  }

  close(): void {
    this.logger.debug(
      `Closing client with connection ID: ${this.connectionId}`
    );

    this.client.destroy();
    this.emit("close", 0);
  }
}

// https://github.com/nestjs/nest/blob/master/packages/websockets/adapters/ws-adapter.ts
export class WsApiGatewayAdapter
  implements WebSocketAdapter<WsApiGatewayServer, WsApiGatewayClient, never>
{
  private readonly logger = new Logger(WsApiGatewayAdapter.name);

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
    const source$ = fromEvent(
      client,
      "message",
      (data, callback) => [data, callback] as [string, Callback]
    ).pipe(
      mergeMap(([data, callback]) =>
        this.bindMessageHandler(data, handlers, transform).pipe(
          map((response) => [response, callback] as [any, Callback])
        )
      ),
      takeUntil(close$)
    );

    source$.subscribe(([response, callback]) => {
      const data = JSON.stringify(response);
      this.logger.verbose(data);
      client
        .send(data)
        .then(() =>
          callback(undefined, {
            statusCode: 200,
          })
        )
        .catch((err) => {
          const error = unknownAsErr(err);
          this.logger.error(error.message, error.stack);
          callback(err);
        });
    });
  }

  bindMessageHandler(
    data: string,
    handlers: WsMessageHandler<string>[],
    transform: (data: any) => Observable<any>
  ) {
    try {
      const message: WsResponse = JSON.parse(data);
      this.logger.verbose(message);

      const messageHandler = handlers.find(
        (handler) => handler.message === message.event
      );
      if (!messageHandler) {
        return EMPTY;
      }

      this.logger.debug(`Executing handler for event: "${message.event}"`);
      return transform(messageHandler.callback(message.data, message.event));
    } catch (err) {
      const error = unknownAsErr(err);
      this.logger.error(error.message, error.stack);

      return EMPTY;
    }
  }

  // eslint-disable-next-line @typescript-eslint/no-unused-vars, @typescript-eslint/no-empty-function
  close(_server: WsApiGatewayServer) {}
}
