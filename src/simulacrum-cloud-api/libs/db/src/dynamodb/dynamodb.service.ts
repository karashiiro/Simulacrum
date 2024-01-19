import { DynamoDBClient } from "@aws-sdk/client-dynamodb";
import {
  Connection,
  EntityManager,
  ScanManager,
  createConnection,
  getEntityManager,
  getScanManager,
} from "@typedorm/core";
import { DocumentClientV3 } from "@typedorm/document-client";
import { Injectable } from "@nestjs/common";
import { table } from "./entity/table";
import {
  DbAccessService,
  MediaSourceDto,
  MediaSourceType,
  ScreenDto,
} from "../common";
import { MediaSource } from "./entity/media-source.entity";
import { Screen } from "./entity/screen.entity";

type UnknownMediaSourceDto =
  | MediaSourceDto
  | { meta: Partial<{ type: MediaSourceType }> | MediaSourceDto["meta"] };

/**
 * Sets the type of an opaque media source to the provided type, and
 * asserts that it is the provided discriminated union case of the
 * meta field.
 * @param mediaSource The opaque media source object.
 * @param type The type to set and assert.
 */
function setMediaSourceMetaType<T extends MediaSourceType>(
  mediaSource: UnknownMediaSourceDto,
  type: T
): asserts mediaSource is MediaSourceDto & {
  meta: MediaSourceDto["meta"] & { type: T };
} {
  mediaSource.meta.type = type;
}

function assertExhaustive(x: never) {
  return x;
}

function mediaSourceFromDynamo(mediaSource: MediaSource): MediaSourceDto;

function mediaSourceFromDynamo(
  mediaSource: MediaSource | undefined
): MediaSourceDto | undefined;

/**
 * Converts a media source entity instance from DynamoDB into the common
 * media source object type.
 * @param mediaSource The media source entity instance from DynamoDB.
 * @returns The converted media source object.
 */
function mediaSourceFromDynamo(
  mediaSource: MediaSource | undefined
): MediaSourceDto | undefined {
  if (mediaSource === undefined) {
    return undefined;
  }

  const mediaSourceDto: UnknownMediaSourceDto = {
    id: mediaSource.id,
    meta: {},
    updatedAt: mediaSource.updatedAt,
  };

  setMediaSourceMetaType(mediaSourceDto, mediaSource.type);
  switch (mediaSource.type) {
    case "blank":
      break;
    case "image":
      mediaSourceDto.meta = {
        type: "image",
        uri: mediaSource.uri ?? "",
      };
      break;
    case "video":
      mediaSourceDto.meta = {
        type: "video",
        uri: mediaSource.uri ?? "",
        playheadSeconds: mediaSource.playheadSeconds ?? 0,
        playheadUpdatedAt: mediaSource.playheadUpdatedAt ?? 0,
        state: mediaSource.state ?? "paused",
      };
      break;
    default:
      assertExhaustive(mediaSource.type);
      break;
  }

  return mediaSourceDto;
}

/**
 * Converts a common media source object into a DynamoDB media source entity instance.
 * @param mediaSourceDto The common media source object.
 * @returns A DynamoDB media source entity instance.
 */
function mediaSourceToDynamo(
  mediaSourceDto: Partial<MediaSourceDto>
): MediaSource {
  const ms = new MediaSource();
  if (mediaSourceDto.id) ms.id = mediaSourceDto.id;

  if (!mediaSourceDto.meta) {
    throw new Error('Object does not have field "meta".');
  }

  // Flatten the object metadata into the entity object
  ms.type = mediaSourceDto.meta.type;
  switch (mediaSourceDto.meta.type) {
    case "blank":
      break;
    case "image":
      ms.uri = mediaSourceDto.meta.uri;
      break;
    case "video":
      ms.uri = mediaSourceDto.meta.uri;
      ms.playheadSeconds = mediaSourceDto.meta.playheadSeconds;
      ms.playheadUpdatedAt = mediaSourceDto.meta.playheadUpdatedAt;
      ms.state = mediaSourceDto.meta.state;
      break;
    default:
      assertExhaustive(mediaSourceDto.meta);
      break;
  }

  if (mediaSourceDto.updatedAt) ms.updatedAt = mediaSourceDto.updatedAt;
  return ms;
}

/**
 * Converts a media source entity instance into an update object representing that instance.
 * @param mediaSource The media source entity instance.
 * @returns An update object representing the assigned fields on the entity instance.
 */
function mediaSourceUpdateFromDynamo(
  mediaSource: MediaSource
): Partial<MediaSource> {
  // Sorry not sorry
  return JSON.parse(JSON.stringify(mediaSource));
}

@Injectable()
export class DynamoDbService implements DbAccessService {
  private readonly entityManager: EntityManager;
  private readonly scanManager: ScanManager;

  static client: DynamoDBClient;
  static connection: Connection;

  constructor() {
    if (DynamoDbService.connection === undefined) {
      const ddbClient = new DynamoDBClient({
        endpoint:
          process.env.SIMULACRUM_DDB_ENDPOINT || "http://localhost:8000",
      });

      const documentClient = new DocumentClientV3(ddbClient);

      DynamoDbService.client = ddbClient;
      DynamoDbService.connection = createConnection({
        table: table,
        entities: [MediaSource, Screen],
        documentClient,
      });
    }

    this.entityManager = getEntityManager();
    this.scanManager = getScanManager();
  }

  async findMediaSource(id: string): Promise<MediaSourceDto | undefined> {
    const mediaSource = await this.entityManager.findOne(MediaSource, { id });
    return mediaSourceFromDynamo(mediaSource);
  }

  async findAllMediaSources(): Promise<MediaSourceDto[]> {
    const results = await this.scanManager.find(MediaSource);
    const resultItems = results.items ?? [];
    return resultItems.map((ms) => mediaSourceFromDynamo(ms));
  }

  async createMediaSource(
    dto: Omit<MediaSourceDto, "id" | "updatedAt">
  ): Promise<MediaSourceDto> {
    // Set the update timestamp for the playhead in milliseconds for sync precision
    if (dto.meta.type === "video") {
      dto.meta.playheadUpdatedAt = new Date().valueOf();
    }

    const mediaSource = await this.entityManager.create<MediaSource>(
      mediaSourceToDynamo(dto)
    );
    return mediaSourceFromDynamo(mediaSource);
  }

  async updateMediaSource(
    id: string,
    dto: Partial<MediaSourceDto>
  ): Promise<MediaSourceDto | undefined> {
    // Set the update timestamp for the playhead in milliseconds for sync precision
    const meta = dto.meta;
    if (meta?.type === "video") {
      if (meta?.uri !== undefined) {
        // If the URI changes, the playhead should reset
        meta.playheadSeconds = 0;
      }

      // TODO: Do we also need to update this when the play state changes?
      // If so, we need to update the playhead too, but this might cause client-side
      // jitter since the client may have desynced by this point.

      if (meta?.playheadSeconds !== undefined) {
        // If the playhead changes, the playhead update timestamp should be updated
        meta.playheadUpdatedAt = new Date().valueOf();
      }
    }

    const mediaSource = await this.entityManager.update(
      MediaSource,
      { id },
      mediaSourceUpdateFromDynamo(mediaSourceToDynamo(dto))
    );
    return mediaSourceFromDynamo(mediaSource);
  }

  async findScreensByMediaSourceId(
    mediaSourceId: string
  ): Promise<ScreenDto[]> {
    const results = await this.entityManager.find(
      Screen,
      { mediaSourceId },
      {
        queryIndex: "GSI1",
      }
    );
    return results.items ?? [];
  }

  async createScreen(
    dto: Omit<ScreenDto, "id" | "updatedAt">
  ): Promise<ScreenDto> {
    const screen = new Screen();
    screen.territory = dto.territory;
    screen.world = dto.world;
    screen.position = dto.position;
    screen.mediaSourceId = dto.mediaSourceId;

    const result = await this.entityManager.create<Screen>(screen);
    return result;
  }
}
