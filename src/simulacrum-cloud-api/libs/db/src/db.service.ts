import { Injectable } from '@nestjs/common';
import { DbAccessService } from './common/service';
import { DynamoDbService } from './dynamodb/dynamodb.service';
import { PlaybackTrackerDto } from './common/entities';

@Injectable()
export class DbService implements DbAccessService {
  constructor(private readonly ddb: DynamoDbService) {}

  findPlaybackTrackerById(id: string): Promise<PlaybackTrackerDto | undefined> {
    return this.ddb.findPlaybackTrackerById(id);
  }

  createPlaybackTracker(playheadSeconds: number): Promise<PlaybackTrackerDto> {
    return this.ddb.createPlaybackTracker(playheadSeconds);
  }
}
