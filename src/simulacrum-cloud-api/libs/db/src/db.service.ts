import { Injectable } from '@nestjs/common';
import { DbAccessService, MediaSourceDto, PlaybackTrackerDto } from './common';
import { DynamoDbService } from './dynamodb/dynamodb.service';

@Injectable()
export class DbService implements DbAccessService {
  constructor(private readonly ddb: DynamoDbService) {}

  findPlaybackTrackerById(id: string): Promise<PlaybackTrackerDto | undefined> {
    return this.ddb.findPlaybackTrackerById(id);
  }

  createPlaybackTracker(): Promise<PlaybackTrackerDto> {
    return this.ddb.createPlaybackTracker();
  }

  updatePlaybackTracker(
    id: string,
    dto: Partial<PlaybackTrackerDto>,
  ): Promise<PlaybackTrackerDto | undefined> {
    return this.ddb.updatePlaybackTracker(id, dto);
  }

  createMediaSource(): Promise<MediaSourceDto> {
    return this.ddb.createMediaSource();
  }
}
