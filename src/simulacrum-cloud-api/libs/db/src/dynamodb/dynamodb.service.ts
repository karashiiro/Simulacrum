import { DynamoDBClient } from '@aws-sdk/client-dynamodb';
import { createConnection, getEntityManager } from '@typedorm/core';
import { DocumentClientV3 } from '@typedorm/document-client';
import { Injectable } from '@nestjs/common';
import { PlaybackTracker } from './entity/playback-tracker.entity';
import { table } from './entity/table';
import { DbAccessService } from '../common/service';
import { PlaybackTrackerDto } from '../common/entities';

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

  createPlaybackTracker(playheadSeconds: number): Promise<PlaybackTrackerDto> {
    const playbackTracker = new PlaybackTracker();
    playbackTracker.playheadSeconds = playheadSeconds;

    return this.entityManager.create(playbackTracker);
  }
}
