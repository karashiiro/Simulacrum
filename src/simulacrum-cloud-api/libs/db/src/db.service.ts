import { Injectable } from '@nestjs/common';
import { DbAccessService, ImageSourceDto, VideoSourceDto } from './common';
import { DynamoDbService } from './dynamodb/dynamodb.service';

@Injectable()
export class DbService implements DbAccessService {
  constructor(private readonly ddb: DynamoDbService) {}

  findVideoSource(id: string): Promise<VideoSourceDto | undefined> {
    return this.ddb.findVideoSource(id);
  }

  findAllVideoSources(): Promise<VideoSourceDto[]> {
    return this.ddb.findAllVideoSources();
  }

  createVideoSource(): Promise<VideoSourceDto> {
    return this.ddb.createVideoSource();
  }

  updateVideoSource(
    id: string,
    dto: Partial<VideoSourceDto>,
  ): Promise<VideoSourceDto | undefined> {
    return this.ddb.updateVideoSource(id, dto);
  }

  findImageSource(id: string): Promise<ImageSourceDto | undefined> {
    return this.ddb.findImageSource(id);
  }

  findAllImageSources(): Promise<ImageSourceDto[]> {
    return this.ddb.findAllImageSources();
  }

  createImageSource(): Promise<ImageSourceDto> {
    return this.ddb.createImageSource();
  }

  updateImageSource(
    id: string,
    dto: Partial<ImageSourceDto>,
  ): Promise<ImageSourceDto | undefined> {
    return this.ddb.updateImageSource(id, dto);
  }
}
