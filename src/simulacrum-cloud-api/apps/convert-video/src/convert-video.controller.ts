import { Controller, Get } from '@nestjs/common';
import { ConvertVideoService } from './convert-video.service';

@Controller()
export class ConvertVideoController {
  constructor(private readonly convertVideoService: ConvertVideoService) {}

  @Get()
  getHello(): string {
    return this.convertVideoService.getHello();
  }
}
