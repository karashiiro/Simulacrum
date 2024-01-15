import { Test, TestingModule } from "@nestjs/testing";
import { EventsGateway } from "./events.gateway";
import {
  DbAccessService,
  MediaSourceDto,
  ScreenDto,
} from "@simulacrum/db/common";
import { DbService } from "@simulacrum/db";
import { broadcast } from "../utils/ws";

jest.mock("../utils/ws");

class TestDbService implements DbAccessService {
  findMediaSource = jest.fn().mockResolvedValue(Promise.resolve());
  findAllMediaSources = jest.fn().mockResolvedValue(Promise.resolve());
  createMediaSource = jest.fn().mockResolvedValue(Promise.resolve());
  updateMediaSource = jest.fn().mockResolvedValue(Promise.resolve());
  findScreensByMediaSourceId = jest.fn().mockResolvedValue(Promise.resolve());
  createScreen = jest.fn().mockResolvedValue(Promise.resolve());
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
    it("returns the list of created screens for the media source to the caller", async () => {
      // Arrange: Mock some screens
      const dtos: ScreenDto[] = new Array(21).fill(undefined).map((_, i) => ({
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
    it("returns the list of created media sources to the caller", async () => {
      // Arrange: Mock some media sources
      const dtos: MediaSourceDto[] = new Array(21)
        .fill(undefined)
        .map((_, i) => ({
          id: i.toString(),
          meta: { type: "image", uri: "something" },
          updatedAt: 1705333950,
        }));

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
});
