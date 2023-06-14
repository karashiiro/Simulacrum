import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import {
  createConnection,
  getEntityManager,
  getScanManager,
} from '@typedorm/core';
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
  private readonly scanManager = getScanManager();

  findMediaSource(id: string): Promise<MediaSourceDto | undefined> {
    return this.entityManager.findOne(MediaSource, { id });
  }

  async findAllMediaSources(): Promise<MediaSourceDto[]> {
    const results = await this.scanManager.find(MediaSource);
    return results.items ?? [];
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
    // Set the update timestamp for the playhead in milliseconds for sync precision
    const meta = dto.meta;
    if (meta?.type === 'video' && meta.playheadSeconds != null) {
      meta.playheadUpdatedAt = new Date().valueOf();
    }

    return this.entityManager.update(MediaSource, { id }, dto);
  }
}
