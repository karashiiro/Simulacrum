import { Test, TestingModule } from "@nestjs/testing";
import { EventsGateway } from "./events.gateway";
import { DbAccessService, MediaSourceDto, ScreenDto } from "@simulacrum/db";
import { DbService } from "@simulacrum/db";
import { broadcast } from "../utils/ws";
import crypto from "node:crypto";

jest.mock("../utils/ws");

class TestDbService implements DbAccessService {
  findMediaSource = jest.fn().mockImplementation((id) =>
    Promise.resolve({
      id: id,
      updatedAt: Math.floor(Date.now() / 1000),
      meta: { type: "blank" },
    })
  );
  findAllMediaSources = jest.fn().mockResolvedValue(Promise.resolve([]));
  createMediaSource = jest.fn().mockImplementation((mediaSource) =>
    Promise.resolve({
      ...mediaSource,
      id: crypto.randomUUID(),
      updatedAt: Math.floor(Date.now() / 1000),
    })
  );
  updateMediaSource = jest.fn().mockImplementation((id, mediaSource) =>
    Promise.resolve({
      ...mediaSource,
      id: id,
    })
  );
  findScreensByMediaSourceId = jest.fn().mockResolvedValue(Promise.resolve([]));
  createScreen = jest.fn().mockImplementation((screen) =>
    Promise.resolve({
      ...screen,
      id: crypto.randomUUID(),
      updatedAt: Math.floor(Date.now() / 1000),
    })
  );
}

describe("EventsGateway", () => {
  let gateway: EventsGateway;
  let db: TestDbService;

  beforeEach(async () => {
    jest.resetAllMocks();

    db = new TestDbService();

    const module: TestingModule = await Test.createTestingModule({
      providers: [EventsGateway],
    })
      .useMocker((token) => {
        if (token === DbService) {
          return db;
        }
      })
      .compile();

    gateway = module.get<EventsGateway>(EventsGateway);
  });

  it("should be defined", () => {
    expect(gateway).toBeDefined();
  });

  describe("createScreen", () => {
    it("creates a screen in the database", async () => {
      // Act: Create a screen
      await gateway.createScreen({
        screen: {
          territory: 7,
          world: 74,
          position: {
            x: 20,
            y: 21,
            z: 22,
          },
        },
      });

      // Assert: The DB screen creation method was called once
      expect(db.createScreen).toHaveBeenCalledTimes(1);
    });

    it("broadcasts the screen creation event to all connected clients", async () => {
      // Act: Create a screen
      await gateway.createScreen({
        screen: {
          territory: 7,
          world: 74,
          position: {
            x: 20,
            y: 21,
            z: 22,
          },
        },
      });

      // TODO: test broadcast separately
      // Assert: broadcast was called once
      expect(broadcast).toHaveBeenCalledTimes(1);
    });
  });

  describe("createMediaSource", () => {
    it("creates a media source in the database", async () => {
      // Act: Create a media source
      await gateway.createMediaSource({
        mediaSource: {
          meta: {
            type: "blank",
          },
        },
      });

      // Assert: The DB media source creation method was called once
      expect(db.createMediaSource).toHaveBeenCalledTimes(1);
    });

    it("broadcasts the media source creation event to all connected clients", async () => {
      // Act: Create a media source
      await gateway.createMediaSource({
        mediaSource: {
          meta: {
            type: "blank",
          },
        },
      });

      // Assert: broadcast was called once
      expect(broadcast).toHaveBeenCalledTimes(1);
    });
  });

  describe("listScreensForMediaSource", () => {
    const createScreens = (amount: number): ScreenDto[] => {
      return new Array(amount).fill(undefined).map((_, i) => ({
        id: i.toString(),
        territory: 7,
        world: 74 + i,
        position: {
          x: 20,
          y: 21,
          z: 22,
        },
        updatedAt: 1705284275,
      }));
    };

    it("returns the list of created screens for the media source to the caller", async () => {
      // Arrange: Mock some screens
      const dtos = createScreens(21);

      db.findScreensByMediaSourceId.mockImplementationOnce(() => dtos);

      // Act: Call method
      const results = await gateway.listScreensForMediaSource({
        mediaSourceId: "0",
      });

      // Assert: Result pages include screens, batched into pages of 10
      results.forEach((page) => {
        expect(page).toStrictEqual({
          event: "MEDIA_SOURCE_LIST_SCREENS",
          data: {
            screens: new Array(10)
              .fill(undefined)
              .map(() => dtos.shift())
              .filter((dto) => dto),
          },
        });
      });

      // Assert: broadcast was not called
      expect(broadcast).toHaveBeenCalledTimes(0);

      // Assert: All assertions occurred (pseudo-length check on results)
      expect.assertions(4);
    });
  });

  describe("listMediaSources", () => {
    const createMediaSources = (amount: number): MediaSourceDto[] => {
      return new Array(amount).fill(undefined).map((_, i) => ({
        id: i.toString(),
        meta: { type: "image", uri: "something" },
        updatedAt: 1705333950,
      }));
    };

    it("returns the list of created media sources to the caller", async () => {
      // Arrange: Mock some media sources
      const dtos = createMediaSources(21);

      db.findAllMediaSources.mockImplementationOnce(() => dtos);

      // Act: Call method
      const results = await gateway.listMediaSources();

      // Assert: Result pages include media sources, batched into pages of 10
      results.forEach((page) => {
        expect(page).toStrictEqual({
          event: "MEDIA_SOURCE_LIST",
          data: {
            mediaSources: new Array(10)
              .fill(undefined)
              .map(() => dtos.shift())
              .filter((dto) => dto),
          },
        });
      });

      // Assert: broadcast was not called
      expect(broadcast).toHaveBeenCalledTimes(0);

      // Assert: All assertions occurred (pseudo-length check on results)
      expect.assertions(4);
    });
  });

  describe("syncVideoSource", () => {
    it("returns the current state of the video source", async () => {
      // Arrange: Mock a video source
      const dto: MediaSourceDto = {
        id: "0",
        meta: { type: "video", uri: "something" },
        updatedAt: 1705333950,
      };

      db.findMediaSource.mockResolvedValueOnce(dto);

      // Act: Call method
      const result = await gateway.syncVideoSource({
        id: "0",
      });

      // Assert: Result is expected
      expect(result).toStrictEqual({
        event: "VIDEO_SOURCE_SYNC",
        data: {
          mediaSource: dto,
        },
      });

      // Assert: broadcast was not called
      expect(broadcast).toHaveBeenCalledTimes(0);
    });

    it("throws an error when the target media source could not be found", async () => {
      // Arrange: Mock an empty response
      db.findMediaSource.mockResolvedValueOnce(undefined);

      // Act: Call method
      // Assert: Method throws an error
      await expect(
        gateway.syncVideoSource({
          id: "0",
        })
      ).rejects.toThrowError();
    });

    it("throws an error when the target media source is not a video", async () => {
      // Arrange: Mock an image source
      const dto: MediaSourceDto = {
        id: "0",
        meta: { type: "image", uri: "something" },
        updatedAt: 1705333950,
      };

      db.findMediaSource.mockResolvedValueOnce(dto);

      // Act: Call method
      // Assert: Method throws an error
      await expect(
        gateway.syncVideoSource({
          id: "0",
        })
      ).rejects.toThrowError();
    });
  });

  describe("playVideoSource", () => {
    it("sets the state of the video source to playing and broadcasts the update", async () => {
      // Arrange: Mock a video source
      const dto: MediaSourceDto = {
        id: "0",
        meta: { type: "video", uri: "something" },
        updatedAt: 1705333950,
      };

      db.findMediaSource.mockResolvedValueOnce(dto);

      db.updateMediaSource.mockResolvedValueOnce({
        id: "0",
        meta: { type: "video", uri: "something", state: "playing" },
        updatedAt: 1705333950,
      });

      // Act: Call method
      await gateway.playVideoSource({
        id: "0",
      });

      // Assert: State was updated
      expect(db.updateMediaSource).toHaveBeenCalledWith("0", {
        meta: { type: "video", state: "playing" },
      });

      // Assert: broadcast was called
      expect(broadcast).toHaveBeenCalledTimes(1);
    });

    it("throws an error when the target media source could not be found", async () => {
      // Arrange: Mock an empty response
      db.findMediaSource.mockResolvedValueOnce(undefined);

      // Act: Call method
      // Assert: Method throws an error
      await expect(
        gateway.playVideoSource({
          id: "0",
        })
      ).rejects.toThrowError();

      // Assert: broadcast was not called
      expect(broadcast).toHaveBeenCalledTimes(0);
    });

    it("throws an error when the target media source is not a video", async () => {
      // Arrange: Mock an image source
      const dto: MediaSourceDto = {
        id: "0",
        meta: { type: "image", uri: "something" },
        updatedAt: 1705333950,
      };

      db.findMediaSource.mockResolvedValueOnce(dto);

      // Act: Call method
      // Assert: Method throws an error
      await expect(
        gateway.playVideoSource({
          id: "0",
        })
      ).rejects.toThrowError();

      // Assert: broadcast was not called
      expect(broadcast).toHaveBeenCalledTimes(0);
    });
  });

  describe("pauseVideoSource", () => {
    it("sets the state of the video source to paused and broadcasts the update", async () => {
      // Arrange: Mock a video source
      const dto: MediaSourceDto = {
        id: "0",
        meta: { type: "video", uri: "something" },
        updatedAt: 1705333950,
      };

      db.findMediaSource.mockResolvedValueOnce(dto);

      db.updateMediaSource.mockResolvedValueOnce({
        id: "0",
        meta: { type: "video", uri: "something", state: "paused" },
        updatedAt: 1705333950,
      });

      // Act: Call method
      await gateway.pauseVideoSource({
        id: "0",
      });

      // Assert: State was updated
      expect(db.updateMediaSource).toHaveBeenCalledWith("0", {
        meta: { type: "video", state: "paused" },
      });

      // Assert: broadcast was called
      expect(broadcast).toHaveBeenCalledTimes(1);
    });

    it("throws an error when the target media source could not be found", async () => {
      // Arrange: Mock an empty response
      db.findMediaSource.mockResolvedValueOnce(undefined);

      // Act: Call method
      // Assert: Method throws an error
      await expect(
        gateway.pauseVideoSource({
          id: "0",
        })
      ).rejects.toThrowError();

      // Assert: broadcast was not called
      expect(broadcast).toHaveBeenCalledTimes(0);
    });

    it("throws an error when the target media source is not a video", async () => {
      // Arrange: Mock an image source
      const dto: MediaSourceDto = {
        id: "0",
        meta: { type: "image", uri: "something" },
        updatedAt: 1705333950,
      };

      db.findMediaSource.mockResolvedValueOnce(dto);

      // Act: Call method
      // Assert: Method throws an error
      await expect(
        gateway.pauseVideoSource({
          id: "0",
        })
      ).rejects.toThrowError();

      // Assert: broadcast was not called
      expect(broadcast).toHaveBeenCalledTimes(0);
    });
  });

  describe("panVideoSource", () => {
    it("changes the playhead state of the video source and broadcasts the update", async () => {
      // Arrange: Mock a video source
      const dto: MediaSourceDto = {
        id: "0",
        meta: { type: "video", uri: "something" },
        updatedAt: 1705333950,
      };

      db.findMediaSource.mockResolvedValueOnce(dto);

      db.updateMediaSource.mockResolvedValueOnce({
        id: "0",
        meta: { type: "video", uri: "something", playheadSeconds: 23 },
        updatedAt: 1705333950,
      });

      // Act: Call method
      await gateway.panVideoSource({
        id: "0",
        playheadSeconds: 23,
      });

      // Assert: State was updated
      expect(db.updateMediaSource).toHaveBeenCalledWith("0", {
        meta: { type: "video", playheadSeconds: 23 },
      });

      // Assert: broadcast was called
      expect(broadcast).toHaveBeenCalledTimes(1);
    });

    it("throws an error when the target media source could not be found", async () => {
      // Arrange: Mock an empty response
      db.findMediaSource.mockResolvedValueOnce(undefined);

      // Act: Call method
      // Assert: Method throws an error
      await expect(
        gateway.panVideoSource({
          id: "0",
          playheadSeconds: 23,
        })
      ).rejects.toThrowError();

      // Assert: broadcast was not called
      expect(broadcast).toHaveBeenCalledTimes(0);
    });

    it("throws an error when the target media source is not a video", async () => {
      // Arrange: Mock an image source
      const dto: MediaSourceDto = {
        id: "0",
        meta: { type: "image", uri: "something" },
        updatedAt: 1705333950,
      };

      db.findMediaSource.mockResolvedValueOnce(dto);

      // Act: Call method
      // Assert: Method throws an error
      await expect(
        gateway.panVideoSource({
          id: "0",
          playheadSeconds: 23,
        })
      ).rejects.toThrowError();

      // Assert: broadcast was not called
      expect(broadcast).toHaveBeenCalledTimes(0);
    });
  });
});
