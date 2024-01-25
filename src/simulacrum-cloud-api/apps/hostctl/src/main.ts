import { NestFactory } from "@nestjs/core";
import { HostctlModule } from "./hostctl.module";
import { WsAdapter } from "@nestjs/platform-ws";
import serverlessExpress from "@codegenie/serverless-express";
import type { Callback, Context, Handler } from "aws-lambda";
import type { INestApplication } from "@nestjs/common";

let server: Handler;

async function initServerless(app: INestApplication): Promise<Handler> {
  await app.init();
  const expressApp = app.getHttpAdapter().getInstance();
  return serverlessExpress({ app: expressApp });
}

async function bootstrap() {
  const app = await NestFactory.create(HostctlModule);

  app.useWebSocketAdapter(new WsAdapter(app));

  switch (process.env.SIMULACRUM_COMPUTE_PLATFORM) {
    case "aws":
      // Initialize the server if needed
      server = server ?? (await initServerless(app));
      break;
    case "generic":
    default:
      // Just listen on the target port
      await app.listen(3000);
      break;
  }
}

const didBootstrap = bootstrap();

// Export handler for Lambda interface
export const handler: Handler = async (
  event: any,
  context: Context,
  callback: Callback
) => {
  console.log(event);
  return didBootstrap.then(() => server(event, context, callback));
};
