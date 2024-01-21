import { Injectable, Logger } from "@nestjs/common";
import { DbAccessService, MediaSourceDto, ScreenDto } from "./common";
import { DynamoDbService } from "./dynamodb/dynamodb.service";

@Injectable()
export class DbService implements DbAccessService {
  private readonly logger = new Logger(DbService.name);

  constructor(private readonly ddb: DynamoDbService) {}

  findMediaSource(id: string): Promise<MediaSourceDto | undefined> {
    this.logger.debug(`Fetching media source with ID "${id}"`);
    return this.ddb.findMediaSource(id);
  }

  findAllMediaSources(): Promise<MediaSourceDto[]> {
    this.logger.debug("Fetching all media sources");
    return this.ddb.findAllMediaSources();
  }

  createMediaSource(
    dto: Omit<MediaSourceDto, "id" | "updatedAt">
  ): Promise<MediaSourceDto> {
    this.logger.debug(`Creating media source of type "${dto.meta.type}"`);
    return this.ddb.createMediaSource(dto);
  }

  updateMediaSource(
    id: string,
    dto: Partial<MediaSourceDto>
  ): Promise<MediaSourceDto | undefined> {
    this.logger.debug(`Updating media source with ID "${id}"`);
    return this.ddb.updateMediaSource(id, dto);
  }

  findScreensByMediaSourceId(mediaSourceId: string): Promise<ScreenDto[]> {
    this.logger.debug(
      `Fetching all screens with media source ID "${mediaSourceId}"`
    );
    return this.ddb.findScreensByMediaSourceId(mediaSourceId);
  }

  createScreen(dto: Omit<ScreenDto, "id" | "updatedAt">): Promise<ScreenDto> {
    this.logger.debug(
      `Creating screen at (x=${dto.position.x} y=${dto.position.y} z=${dto.position.z} tt=${dto.territory} w=${dto.world} ms=${dto.mediaSourceId})`
    );
    return this.ddb.createScreen(dto);
  }
}
