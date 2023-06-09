import { Module } from '@nestjs/common';
import { ConvertVideoController } from './convert-video.controller';
import { ConvertVideoService } from './convert-video.service';

@Module({
  imports: [],
  controllers: [ConvertVideoController],
  providers: [ConvertVideoService],
})
export class ConvertVideoModule {}
