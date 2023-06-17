import { Injectable } from "@nestjs/common";
import { DbAccessService, MediaSourceDto, ScreenDto } from "./common";
import { DynamoDbService } from "./dynamodb/dynamodb.service";

@Injectable()
export class DbService implements DbAccessService {
  constructor(private readonly ddb: DynamoDbService) {}

  findMediaSource(id: string): Promise<MediaSourceDto | undefined> {
    return this.ddb.findMediaSource(id);
  }

  findAllMediaSources(): Promise<MediaSourceDto[]> {
    return this.ddb.findAllMediaSources();
  }

  createMediaSource(
    dto: Omit<MediaSourceDto, "id" | "updatedAt">
  ): Promise<MediaSourceDto> {
    return this.ddb.createMediaSource(dto);
  }

  updateMediaSource(
    id: string,
    dto: Partial<MediaSourceDto>
  ): Promise<MediaSourceDto | undefined> {
    return this.ddb.updateMediaSource(id, dto);
  }

  findScreensByMediaSourceId(mediaSourceId: string): Promise<ScreenDto[]> {
    return this.ddb.findScreensByMediaSourceId(mediaSourceId);
  }

  createScreen(dto: Omit<ScreenDto, "id" | "updatedAt">): Promise<ScreenDto> {
    return this.ddb.createScreen(dto);
  }
}
