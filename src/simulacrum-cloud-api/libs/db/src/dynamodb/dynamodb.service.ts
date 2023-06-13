import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import { createConnection, getEntityManager } from '@typedorm/core';
import { DocumentClientV3 } from '@typedorm/document-client';
import { Injectable } from '@nestjs/common';
import { table } from './entity/table';
import { DbAccessService, MediaMetadata, MediaSourceDto } from '../common';
import { MediaSource } from './entity/media-source.entity';

const documentClient = new DocumentClientV3(
  new DynamoDBClient({
    endpoint: 'http://localhost:8000',
  }),
);

createConnection({
  table: table,
  entities: [MediaSource],
  documentClient,
});

@Injectable()
export class DynamoDbService implements DbAccessService {
  private readonly entityManager = getEntityManager();

  findMediaSource(id: string): Promise<MediaSourceDto | undefined> {
    return this.entityManager.findOne(MediaSource, { id });
  }

  async findAllMediaSources(): Promise<MediaSourceDto[]> {
    const results = await this.entityManager.find(MediaSource, {});
    return results.items;
  }

  createMediaSource(meta: MediaMetadata): Promise<MediaSourceDto> {
    const ms = new MediaSource();
    ms.meta = meta;
    return this.entityManager.create(ms);
  }

  updateMediaSource(
    id: string,
    dto: Partial<MediaSourceDto>,
  ): Promise<MediaSourceDto | undefined> {
    return this.entityManager.update(MediaSource, { id }, dto);
  }
}
