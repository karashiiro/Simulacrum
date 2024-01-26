import { NestFactory } from "@nestjs/core";
import { HostctlModule } from "./hostctl.module";
import { WsAdapter } from "@nestjs/platform-ws";
import type { APIGatewayProxyWebsocketHandlerV2, Handler } from "aws-lambda";
import type { INestApplication } from "@nestjs/common";
import { WsApiGatewayAdapter } from "./ws-apigw-adapter";

let server: Handler;

async function initServerless(
  app: INestApplication
): Promise<APIGatewayProxyWebsocketHandlerV2> {
  const wsAdapter = new WsApiGatewayAdapter();

  app.useWebSocketAdapter(wsAdapter);

  await app.init();

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  return (event, _context, callback) => {
    const { body, requestContext } = event;
    const { routeKey, domainName, stage, connectionId } = requestContext;

    switch (routeKey) {
      case "$default":
        // Create/acquire a connection
        const client = wsAdapter.server.getClient(
          domainName,
          stage,
          connectionId
        );

        // Emit a message using the connection - it'll get where it
        // needs to go so long as that place is a WebSocketGateway
        client.emit("message", body, callback);
        break;
      case "$disconnect":
        wsAdapter.server.closeClient(connectionId);
        break;
      default:
        throw new Error(`Unknown route key: ${routeKey}`);
    }
  };
}

async function initGeneric(app: INestApplication): Promise<void> {
  app.useWebSocketAdapter(new WsAdapter(app));
  await app.listen(3000);
}

async function bootstrap() {
  const app = await NestFactory.create(HostctlModule);

  switch (process.env.SIMULACRUM_COMPUTE_PLATFORM) {
    case "aws":
      // Initialize the server if needed
      server = server ?? (await initServerless(app));
      break;
    case "generic":
    default:
      await initGeneric(app);
      break;
  }
}

const didBootstrap = bootstrap();

// Export handler for Lambda interface - this cannot be async, or else execution
// will end once the handler promise resolves instead of when the callback is
// invoked
export const handler: APIGatewayProxyWebsocketHandlerV2 = (
  event,
  context,
  callback
) => {
  console.log(event);
  didBootstrap
    .then(() => server(event, context, callback))
    .catch((err) => callback(err));
};
