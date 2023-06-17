import { NestFactory } from "@nestjs/core";
import { HostctlModule } from "./hostctl.module";
import { WsAdapter } from "@nestjs/platform-ws";

async function bootstrap() {
  const app = await NestFactory.create(HostctlModule);
  app.useWebSocketAdapter(new WsAdapter(app));
  await app.listen(3000);
}
bootstrap();
