import { Injectable } from '@nestjs/common';
import { DbAccessService, VideoSourceDto } from './common';
import { DynamoDbService } from './dynamodb/dynamodb.service';

@Injectable()
export class DbService implements DbAccessService {
  constructor(private readonly ddb: DynamoDbService) {}

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
}
