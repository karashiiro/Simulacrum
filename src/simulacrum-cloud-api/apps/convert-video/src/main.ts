import { NestFactory } from '@nestjs/core';
import { ConvertVideoModule } from './convert-video.module';

async function bootstrap() {
  const app = await NestFactory.create(ConvertVideoModule);
  await app.listen(3000);
}
bootstrap();
