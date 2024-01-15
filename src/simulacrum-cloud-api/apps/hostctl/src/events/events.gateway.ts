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
import { DbService } from "@simulacrum/db";
import { MediaSourceDto, ScreenDto } from "@simulacrum/db/common";
import { Observable, from, map } from "rxjs";
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
  return new Array(Math.ceil(arr.length / batchSize))
    .fill(undefined)
    .map((_, i) => arr.slice(i * batchSize, (i + 1) * batchSize));
}

@WebSocketGateway()
export class EventsGateway implements OnGatewayConnection, OnGatewayDisconnect {
  private readonly logger = new Logger(EventsGateway.name);

  @WebSocketServer()
  private readonly wss: Server;

  constructor(private readonly db: DbService) {}

  handleConnection() {
    this.logger.log("Got new connection");
  }

  handleDisconnect() {
    this.logger.log("Connection from client ended");
  }

  @SubscribeMessage("SCREEN_CREATE")
  async createScreen(@MessageBody() ev: ScreenCreateEvent): Promise<void> {
    const dto = await this.db.createScreen(ev.screen);
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
    // TODO: Don't load this as a single array (can optimize it later)
    const dtos = await this.db.findScreensByMediaSourceId(ev.mediaSourceId);
    const paged = batchArray(dtos, 10);
    return from(paged).pipe(
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
    // TODO: Don't load this as a single array (can optimize it later)
    const dtos = await this.db.findAllMediaSources();
    const paged = batchArray(dtos, 10);
    return from(paged).pipe(
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
    const dto = await this.db.createMediaSource(ev.mediaSource);
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
    const dto = await this.db.findMediaSource(ev.id);
    if (!dto) {
      throw new WsException("Could not find media source.");
    }

    return {
      event: "VIDEO_SOURCE_SYNC",
      data: {
        mediaSource: dto,
      },
    };
  }

  @SubscribeMessage("VIDEO_SOURCE_PLAY")
  async playMediaSource(
    @MessageBody() ev: VideoSourcePlayRequest
  ): Promise<void> {
    const dto = await this.db.updateMediaSource(ev.id, {
      meta: {
        type: "video",
        state: "playing",
      },
    });
    if (!dto) {
      throw new WsException("Could not find media source.");
    }

    broadcast<VideoSourcePlayBroadcast>(this.wss, {
      event: "VIDEO_SOURCE_PLAY",
      data: {
        mediaSource: dto,
      },
    });
  }

  @SubscribeMessage("VIDEO_SOURCE_PAUSE")
  async pauseMediaSource(
    @MessageBody() ev: VideoSourcePauseRequest
  ): Promise<void> {
    const dto = await this.db.updateMediaSource(ev.id, {
      meta: {
        type: "video",
        state: "paused",
      },
    });
    if (!dto) {
      throw new WsException("Could not find media source.");
    }

    broadcast<VideoSourcePauseBroadcast>(this.wss, {
      event: "VIDEO_SOURCE_PAUSE",
      data: {
        mediaSource: dto,
      },
    });
  }

  @SubscribeMessage("VIDEO_SOURCE_PAN")
  async panMediaSource(
    @MessageBody() ev: VideoSourcePanRequest
  ): Promise<void> {
    const dto = await this.db.updateMediaSource(ev.id, {
      meta: {
        type: "video",
        playheadSeconds: ev.playheadSeconds,
      },
    });
    if (!dto) {
      throw new WsException("Could not find media source.");
    }

    broadcast<VideoSourcePanBroadcast>(this.wss, {
      event: "VIDEO_SOURCE_PAN",
      data: {
        mediaSource: dto,
      },
    });
  }
}
