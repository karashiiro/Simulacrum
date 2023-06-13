import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import { createConnection, getEntityManager } from '@typedorm/core';
import { DocumentClientV3 } from '@typedorm/document-client';
import { Injectable } from '@nestjs/common';
import { table } from './entity/table';
import { DbAccessService, ImageSourceDto, VideoSourceDto } from '../common';
import { VideoSource } from './entity/video-source.entity';
import { ImageSource } from './entity/image-source.entity';

const documentClient = new DocumentClientV3(
  new DynamoDBClient({
    endpoint: 'http://localhost:8000',
  }),
);

createConnection({
  table: table,
  entities: [VideoSource, ImageSource],
  documentClient,
});

@Injectable()
export class DynamoDbService implements DbAccessService {
  private readonly entityManager = getEntityManager();

  findVideoSource(id: string): Promise<VideoSourceDto | undefined> {
    return this.entityManager.findOne(VideoSource, { id });
  }

  async findAllVideoSources(): Promise<VideoSourceDto[]> {
    const results = await this.entityManager.find(VideoSource, {});
    return results.items;
  }

  createVideoSource(): Promise<VideoSourceDto> {
    return this.entityManager.create(new VideoSource());
  }

  updateVideoSource(
    id: string,
    dto: Partial<VideoSourceDto>,
  ): Promise<VideoSourceDto | undefined> {
    return this.entityManager.update(VideoSource, { id }, dto);
  }

  findImageSource(id: string): Promise<ImageSourceDto | undefined> {
    return this.entityManager.findOne(ImageSource, { id });
  }

  async findAllImageSources(): Promise<ImageSourceDto[]> {
    const results = await this.entityManager.find(ImageSource, {});
    return results.items;
  }

  createImageSource(): Promise<ImageSourceDto> {
    return this.entityManager.create(new ImageSource());
  }

  updateImageSource(
    id: string,
    dto: Partial<ImageSourceDto>,
  ): Promise<ImageSourceDto | undefined> {
    return this.entityManager.update(ImageSource, { id }, dto);
  }
}
