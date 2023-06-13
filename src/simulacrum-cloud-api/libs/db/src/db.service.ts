import { Injectable } from '@nestjs/common';
import { DbAccessService, MediaMetadata, MediaSourceDto } from './common';
import { DynamoDbService } from './dynamodb/dynamodb.service';

@Injectable()
export class DbService implements DbAccessService {
  constructor(private readonly ddb: DynamoDbService) {}

  findMediaSource(id: string): Promise<MediaSourceDto | undefined> {
    return this.ddb.findMediaSource(id);
  }

  findAllMediaSources(): Promise<MediaSourceDto[]> {
    return this.ddb.findAllMediaSources();
  }

  createMediaSource(meta: MediaMetadata): Promise<MediaSourceDto> {
    return this.ddb.createMediaSource(meta);
  }

  updateMediaSource(
    id: string,
    dto: Partial<MediaSourceDto>,
  ): Promise<MediaSourceDto | undefined> {
    return this.ddb.updateMediaSource(id, dto);
  }
}
