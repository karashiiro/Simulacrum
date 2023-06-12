import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import { createConnection, getEntityManager } from '@typedorm/core';
import { DocumentClientV3 } from '@typedorm/document-client';
import { Injectable } from '@nestjs/common';
import { table } from './entity/table';
import { DbAccessService, VideoSourceDto } from '../common';
import { VideoSource } from './entity/video-source.entity';

const documentClient = new DocumentClientV3(
  new DynamoDBClient({
    endpoint: 'http://localhost:8000',
  }),
);

createConnection({
  table: table,
  entities: [VideoSource],
  documentClient,
});

@Injectable()
export class DynamoDbService implements DbAccessService {
  private readonly entityManager = getEntityManager();

  createVideoSource(): Promise<VideoSourceDto> {
    return this.entityManager.create(new VideoSource());
  }

  updateVideoSource(
    id: string,
    dto: Partial<VideoSourceDto>,
  ): Promise<VideoSourceDto | undefined> {
    return this.entityManager.update(VideoSource, { id }, dto);
  }
}
