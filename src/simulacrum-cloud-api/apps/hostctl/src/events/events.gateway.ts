import { Logger } from "@nestjs/common";
import {
  MessageBody,
  OnGatewayConnection,
  OnGatewayDisconnect,
  SubscribeMessage,
  WebSocketGateway,
  WebSocketServer,
  WsException,
  WsResponse,
} from "@nestjs/websockets";
import { DbService, MediaSourceDto, ScreenDto } from "@simulacrum/db";
import { Observable, from, map, tap } from "rxjs";
import { WebSocketServer as Server } from "ws";
import { broadcast } from "../utils/ws";

interface ScreenCreateEvent {
  screen: Omit<ScreenDto, "id" | "updatedAt">;
}

interface MediaSourceListScreensEvent {
  mediaSourceId: string;
}

interface MediaSourceCreateEvent {
  mediaSource: Omit<MediaSourceDto, "id" | "updatedAt">;
}

interface VideoSourceSyncRequest {
  id: string;
}

interface VideoSourcePlayRequest {
  id: string;
}

interface VideoSourcePauseRequest {
  id: string;
}

interface VideoSourcePanRequest {
  id: string;
  playheadSeconds: number;
}

interface ScreenCreateBroadcast {
  screen: ScreenDto;
}

interface MediaSourceListScreensResponse {
  screens: ScreenDto[];
}

interface MediaSourceListResponse {
  mediaSources: MediaSourceDto[];
}

interface MediaSourceCreateBroadcast {
  mediaSource: MediaSourceDto;
}

interface VideoSourceSyncResponse {
  mediaSource: MediaSourceDto;
}

interface VideoSourcePlayBroadcast {
  mediaSource: MediaSourceDto;
}

interface VideoSourcePauseBroadcast {
  mediaSource: MediaSourceDto;
}

interface VideoSourcePanBroadcast {
  mediaSource: MediaSourceDto;
}

function batchArray<T>(arr: T[], batchSize: number): T[][] {
  if (arr.length === 0) {
    return [[]];
  }

  return new Array(Math.ceil(arr.length / batchSize))
    .fill(undefined)
    .map((_, i) => arr.slice(i * batchSize, (i + 1) * batchSize));
}

@WebSocketGateway()
export class EventsGateway implements OnGatewayConnection, OnGatewayDisconnect {
  private readonly logger = new Logger(EventsGateway.name);

  @WebSocketServer()
  private readonly wss!: Server;

  constructor(private readonly db: DbService) {}

  handleConnection() {
    this.logger.log("Got new connection");
  }

  handleDisconnect() {
    this.logger.log("Connection from client ended");
  }

  @SubscribeMessage("SCREEN_CREATE")
  async createScreen(@MessageBody() ev: ScreenCreateEvent): Promise<void> {
    this.logger.log(
      `Creating new screen: world=${ev.screen.world} territory=${ev.screen.territory} mediaSource=${ev.screen.mediaSourceId}`
    );

    const dto = await this.db.createScreen(ev.screen);

    this.logger.log(`Screen created successfully: id=${dto.id}`);

    broadcast<ScreenCreateBroadcast>(this.wss, {
      event: "SCREEN_CREATE",
      data: {
        screen: dto,
      },
    });
  }

  @SubscribeMessage("MEDIA_SOURCE_LIST_SCREENS")
  async listScreensForMediaSource(
    @MessageBody() ev: MediaSourceListScreensEvent
  ): Promise<Observable<WsResponse<MediaSourceListScreensResponse>>> {
    this.logger.log(
      `Fetching all screens for media source: id=${ev.mediaSourceId}`
    );

    // TODO: Don't load this as a single array (can optimize it later)
    const dtos = await this.db.findScreensByMediaSourceId(ev.mediaSourceId);
    const paged = batchArray(dtos, 10);
    return from(paged).pipe(
      tap((dtos) =>
        this.logger.debug(`Sending page of ${dtos.length} entries to client`)
      ),
      map((dtos) => ({
        event: "MEDIA_SOURCE_LIST_SCREENS",
        data: {
          screens: dtos,
        },
      }))
    );
  }

  @SubscribeMessage("MEDIA_SOURCE_LIST")
  async listMediaSources(): Promise<
    Observable<WsResponse<MediaSourceListResponse>>
  > {
    this.logger.log("Fetching all media sources");

    // TODO: Don't load this as a single array (can optimize it later)
    const dtos = await this.db.findAllMediaSources();
    const paged = batchArray(dtos, 10);
    return from(paged).pipe(
      tap((dtos) =>
        this.logger.debug(`Sending page of ${dtos.length} entries to client`)
      ),
      map((dtos) => ({
        event: "MEDIA_SOURCE_LIST",
        data: {
          mediaSources: dtos,
        },
      }))
    );
  }

  @SubscribeMessage("MEDIA_SOURCE_CREATE")
  async createMediaSource(
    @MessageBody() ev: MediaSourceCreateEvent
  ): Promise<void> {
    this.logger.log(
      `Creating new media source: type=${ev.mediaSource.meta.type}`
    );

    const dto = await this.db.createMediaSource(ev.mediaSource);

    this.logger.log(`Media source created successfully: id=${dto.id}`);

    broadcast<MediaSourceCreateBroadcast>(this.wss, {
      event: "MEDIA_SOURCE_CREATE",
      data: {
        mediaSource: dto,
      },
    });
  }

  @SubscribeMessage("VIDEO_SOURCE_SYNC")
  async syncVideoSource(
    @MessageBody() ev: VideoSourceSyncRequest
  ): Promise<WsResponse<VideoSourceSyncResponse>> {
    this.logger.log(`Syncing playback state for video source: id=${ev.id}`);

    const dto = await this.db.findMediaSource(ev.id);
    if (!dto) {
      throw new WsException("Could not find media source.");
    }

    if (dto.meta.type !== "video") {
      throw new WsException("Media source is not a video.");
    }

    return {
      event: "VIDEO_SOURCE_SYNC",
      data: {
        mediaSource: dto,
      },
    };
  }

  @SubscribeMessage("VIDEO_SOURCE_PLAY")
  async playVideoSource(
    @MessageBody() ev: VideoSourcePlayRequest
  ): Promise<void> {
    this.logger.log(`Setting state to "playing" for video source: id=${ev.id}`);

    // TODO: Combine these into a more efficient "update when" operation
    const dtoInitial = await this.db.findMediaSource(ev.id);
    if (dtoInitial?.meta.type !== "video") {
      throw new WsException("Media source is not a video.");
    }

    const dto = await this.db.updateMediaSource(ev.id, {
      meta: {
        type: "video",
        state: "playing",
      },
    });
    if (!dto) {
      throw new WsException("Could not find media source.");
    }

    this.logger.log("Media source updated successfully");

    broadcast<VideoSourcePlayBroadcast>(this.wss, {
      event: "VIDEO_SOURCE_PLAY",
      data: {
        mediaSource: dto,
      },
    });
  }

  @SubscribeMessage("VIDEO_SOURCE_PAUSE")
  async pauseVideoSource(
    @MessageBody() ev: VideoSourcePauseRequest
  ): Promise<void> {
    this.logger.log(`Setting state to "paused" for video source: id=${ev.id}`);

    const dtoInitial = await this.db.findMediaSource(ev.id);
    if (dtoInitial?.meta.type !== "video") {
      throw new WsException("Media source is not a video.");
    }

    const dto = await this.db.updateMediaSource(ev.id, {
      meta: {
        type: "video",
        state: "paused",
      },
    });
    if (!dto) {
      throw new WsException("Could not find media source.");
    }

    this.logger.log("Media source updated successfully");

    broadcast<VideoSourcePauseBroadcast>(this.wss, {
      event: "VIDEO_SOURCE_PAUSE",
      data: {
        mediaSource: dto,
      },
    });
  }

  @SubscribeMessage("VIDEO_SOURCE_PAN")
  async panVideoSource(
    @MessageBody() ev: VideoSourcePanRequest
  ): Promise<void> {
    this.logger.log(
      `Updating playhead for video source: id=${ev.id} playhead=${ev.playheadSeconds}`
    );

    const dtoInitial = await this.db.findMediaSource(ev.id);
    if (dtoInitial?.meta.type !== "video") {
      throw new WsException("Media source is not a video.");
    }

    const dto = await this.db.updateMediaSource(ev.id, {
      meta: {
        type: "video",
        playheadSeconds: ev.playheadSeconds,
      },
    });
    if (!dto) {
      throw new WsException("Could not find media source.");
    }

    this.logger.log("Media source updated successfully");

    broadcast<VideoSourcePanBroadcast>(this.wss, {
      event: "VIDEO_SOURCE_PAN",
      data: {
        mediaSource: dto,
      },
    });
  }
}
