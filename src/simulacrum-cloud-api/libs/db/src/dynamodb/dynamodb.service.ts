import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import { createConnection, getEntityManager } from '@typedorm/core';
import { DocumentClientV3 } from '@typedorm/document-client';
import { Injectable } from '@nestjs/common';
import { PlaybackTracker } from './entity/playback-tracker.entity';
import { table } from './entity/table';
import { DbAccessService, MediaSourceDto, PlaybackTrackerDto } from '../common';
import { MediaSource } from './entity/media-source.entity';

const documentClient = new DocumentClientV3(
  new DynamoDBClient({
    endpoint: 'http://localhost:8000',
  }),
);

createConnection({
  table: table,
  entities: [PlaybackTracker],
  documentClient,
});

@Injectable()
export class DynamoDbService implements DbAccessService {
  private readonly entityManager = getEntityManager();

  findPlaybackTrackerById(id: string): Promise<PlaybackTrackerDto | undefined> {
    return this.entityManager.findOne(PlaybackTracker, {
      id,
    });
  }

  createPlaybackTracker(): Promise<PlaybackTrackerDto> {
    return this.entityManager.create(new PlaybackTracker());
  }

  updatePlaybackTracker(
    id: string,
    dto: Partial<PlaybackTrackerDto>,
  ): Promise<PlaybackTrackerDto | undefined> {
    return this.entityManager.update(PlaybackTracker, { id }, dto);
  }

  createMediaSource(): Promise<MediaSourceDto> {
    return this.entityManager.create(new MediaSource());
  }
}
