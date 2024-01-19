import { Test, TestingModule } from "@nestjs/testing";
import { StartedTestContainer, GenericContainer } from "testcontainers";
import { DynamoDbService } from "./dynamodb.service";
import {
  CreateTableCommand,
  CreateTableCommandInput,
  KeyType,
} from "@aws-sdk/client-dynamodb";
import { MediaSourceDto, ScreenDto } from "../common";
import * as assert from "assert";

const tableParams: CreateTableCommandInput = {
  TableName: "Simulacrum",
  KeySchema: [
    {
      AttributeName: "PK",
      KeyType: KeyType.HASH,
    },
    {
      AttributeName: "SK",
      KeyType: KeyType.RANGE,
    },
  ],
  AttributeDefinitions: [
    {
      AttributeName: "PK",
      AttributeType: "S",
    },
    {
      AttributeName: "SK",
      AttributeType: "S",
    },
    {
      AttributeName: "LSI1SK",
      AttributeType: "S",
    },
    {
      AttributeName: "GSI1PK",
      AttributeType: "S",
    },
    {
      AttributeName: "GSI1SK",
      AttributeType: "S",
    },
  ],
  LocalSecondaryIndexes: [
    {
      IndexName: "LSI1",
      KeySchema: [
        { AttributeName: "PK", KeyType: "HASH" },
        { AttributeName: "LSI1SK", KeyType: "RANGE" },
      ],
      Projection: { ProjectionType: "ALL" },
    },
  ],
  GlobalSecondaryIndexes: [
    {
      IndexName: "GSI1",
      KeySchema: [
        { AttributeName: "GSI1PK", KeyType: "HASH" },
        { AttributeName: "GSI1SK", KeyType: "RANGE" },
      ],
      Projection: { ProjectionType: "ALL" },
      ProvisionedThroughput: {
        ReadCapacityUnits: 10,
        WriteCapacityUnits: 5,
      },
    },
  ],
  ProvisionedThroughput: {
    ReadCapacityUnits: 10,
    WriteCapacityUnits: 5,
  },
};

describe("DynamoDbService", () => {
  let service: DynamoDbService;
  let container: StartedTestContainer;
  let env: typeof process.env;

  const expectUUIDv4 = () => {
    // https://github.com/afram/is-uuid/blob/master/lib/is-uuid.js
    return expect.stringMatching(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[4][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
    );
  };

  beforeEach(async () => {
    // Save environment
    env = { ...process.env };

    // Spin up a DDB local instance
    container = await new GenericContainer("amazon/dynamodb-local:latest")
      .withExposedPorts(8000)
      .start();

    // Update the environment with the container address
    process.env.SIMULACRUM_DDB_ENDPOINT = `http://localhost:${container.getMappedPort(
      8000
    )}`;
    process.env.AWS_REGION = "us-east-1";
    process.env.AWS_ACCESS_KEY_ID = "AccessKeyId";
    process.env.AWS_SECRET_ACCESS_KEY = "SecretAccessKey";
    process.env.AWS_SESSION_TOKEN = "SessionToken";

    // Create service testing module
    const module: TestingModule = await Test.createTestingModule({
      providers: [DynamoDbService],
    }).compile();

    // Create a service instance
    service = module.get<DynamoDbService>(DynamoDbService);

    // Create the table
    await DynamoDbService.client.send(new CreateTableCommand(tableParams));
  }, 15000);

  afterEach(async () => {
    // Stop the container
    await container.stop();

    // Restore environment
    process.env = env;
  }, 15000);

  it("should be defined", () => {
    expect(service).toBeDefined();
  });

  describe("createScreen", () => {
    it("creates a screen in the database and returns the result", async () => {
      // Act: Create a screen
      const screen: Omit<ScreenDto, "id" | "updatedAt"> = {
        territory: 7,
        world: 74,
        position: {
          x: 20,
          y: 21,
          z: 22,
        },
      };

      const result = await service.createScreen(screen);

      // Assert: The result was created with an ID and updatedAt
      expect(result).toEqual(
        expect.objectContaining({
          id: expectUUIDv4(),
          updatedAt: expect.any(Number),
          ...screen,
        })
      );
    });
  });

  describe("createMediaSource", () => {
    describe("blank", () => {
      it("creates a blank media source in the database and returns the result", async () => {
        // Act: Create a media source
        const mediaSource: Omit<MediaSourceDto, "id" | "updatedAt"> = {
          meta: {
            type: "blank",
          },
        };

        const result = await service.createMediaSource(mediaSource);

        // Assert: The result was created with an ID and updatedAt
        expect(result).toEqual(
          expect.objectContaining({
            id: expectUUIDv4(),
            updatedAt: expect.any(Number),
            ...mediaSource,
          })
        );
      });
    });

    describe("image", () => {
      describe("without a URI", () => {
        it("creates an image media source in the database and returns the result", async () => {
          // Act: Create a media source
          const mediaSource: Omit<MediaSourceDto, "id" | "updatedAt"> = {
            meta: {
              type: "image",
            },
          };

          const result = await service.createMediaSource(mediaSource);

          // Assert: The result was created with an ID and updatedAt
          expect(result).toEqual(
            expect.objectContaining({
              id: expectUUIDv4(),
              updatedAt: expect.any(Number),
              meta: {
                uri: "",
                ...mediaSource.meta,
              },
            })
          );
        });
      });

      describe("with a URI", () => {
        it("creates an image media source in the database and returns the result", async () => {
          // Act: Create a media source
          const mediaSource: Omit<MediaSourceDto, "id" | "updatedAt"> = {
            meta: {
              type: "image",
              uri: "http://something.local",
            },
          };

          const result = await service.createMediaSource(mediaSource);

          // Assert: The result was created with an ID and updatedAt
          expect(result).toEqual(
            expect.objectContaining({
              id: expectUUIDv4(),
              updatedAt: expect.any(Number),
              ...mediaSource,
            })
          );
        });
      });
    });

    describe("video", () => {
      describe("without a URI", () => {
        it("creates a video media source in the database and returns the result", async () => {
          // Act: Create a media source
          const mediaSource: Omit<MediaSourceDto, "id" | "updatedAt"> = {
            meta: {
              type: "video",
            },
          };

          const result = await service.createMediaSource(mediaSource);

          // Assert: The result was created
          expect(result).toEqual(
            expect.objectContaining({
              id: expectUUIDv4(),
              updatedAt: expect.any(Number),
              meta: {
                playheadSeconds: 0,
                playheadUpdatedAt: expect.any(Number),
                state: "paused",
                uri: "",
                ...mediaSource.meta,
              },
            })
          );
        });
      });

      describe("with a URI", () => {
        it("creates a video media source in the database and returns the result", async () => {
          // Act: Create a media source
          const mediaSource: Omit<MediaSourceDto, "id" | "updatedAt"> = {
            meta: {
              type: "video",
              uri: "http://something.local",
            },
          };

          const result = await service.createMediaSource(mediaSource);

          // Assert: The result was created
          expect(result).toEqual(
            expect.objectContaining({
              id: expectUUIDv4(),
              updatedAt: expect.any(Number),
              meta: {
                playheadSeconds: 0,
                playheadUpdatedAt: expect.any(Number),
                state: "paused",
                ...mediaSource.meta,
              },
            })
          );
        });
      });
    });
  });

  describe("updateMediaSource", () => {
    describe("image", () => {
      let mediaSource: MediaSourceDto;

      beforeEach(async () => {
        // Arrange: Create an image
        mediaSource = await service.createMediaSource({
          meta: {
            type: "image",
            uri: "http://something.local",
          },
        });
      });

      it("updates the image URI when requested to", async () => {
        // Act: Update the image URI
        const result = await service.updateMediaSource(mediaSource.id, {
          meta: {
            type: "image",
            uri: "http://never.local",
          },
        });

        // Assert: The result has the new URI
        expect(result).toEqual({
          ...mediaSource,
          meta: {
            ...mediaSource.meta,
            uri: "http://never.local",
          },
          updatedAt: expect.any(Number),
        });

        // Assert: The new update timestamp is greater than or equal to the original one
        expect(result?.updatedAt).toBeGreaterThanOrEqual(
          mediaSource.updatedAt ?? Infinity
        );
      });
    });

    describe("video", () => {
      let mediaSource: MediaSourceDto;

      beforeEach(async () => {
        // Arrange: Create a video
        mediaSource = await service.createMediaSource({
          meta: {
            type: "video",
            uri: "http://something.local",
          },
        });
      });

      it("updates the video URI when requested to", async () => {
        // Act: Update the video URI
        const result = await service.updateMediaSource(mediaSource.id, {
          meta: {
            type: "video",
            uri: "http://never.local",
          },
        });

        // Assert: The result has the new URI
        expect(result).toEqual({
          ...mediaSource,
          meta: {
            ...mediaSource.meta,
            uri: "http://never.local",
            // When updating the URI, the playhead should return to 0 automatically
            playheadSeconds: 0,
            // When updating the playhead, the playhead update timestamp should also update
            playheadUpdatedAt: expect.any(Number),
          },
          updatedAt: expect.any(Number),
        });

        // Dummy asserts for type narrowing
        assert(result?.meta.type === "video");
        assert(mediaSource.meta.type === "video");

        // Assert: The new playhead update timestamp is at least the original one
        expect(result.meta.playheadUpdatedAt).toBeGreaterThanOrEqual(
          mediaSource.meta.playheadUpdatedAt ?? Infinity
        );

        // Assert: The new update timestamp is greater than or equal to the original one
        expect(result?.updatedAt).toBeGreaterThanOrEqual(
          mediaSource.updatedAt ?? Infinity
        );
      });

      it("updates the playhead when requested to", async () => {
        // Act: Update the video playhead
        const result = await service.updateMediaSource(mediaSource.id, {
          meta: {
            type: "video",
            playheadSeconds: 20.42,
          },
        });

        // Assert: The result has the new playhead
        expect(result).toEqual({
          ...mediaSource,
          meta: {
            ...mediaSource.meta,
            playheadSeconds: 20.42,
            // When updating the playhead, the playhead update timestamp should also update
            playheadUpdatedAt: expect.any(Number),
          },
          updatedAt: expect.any(Number),
        });

        // Dummy asserts for type narrowing
        assert(result?.meta.type === "video");
        assert(mediaSource.meta.type === "video");

        // Assert: The new playhead update timestamp is at least the original one
        expect(result.meta.playheadUpdatedAt).toBeGreaterThanOrEqual(
          mediaSource.meta.playheadUpdatedAt ?? Infinity
        );

        // Assert: The new update timestamp is greater than or equal to the original one
        expect(result?.updatedAt).toBeGreaterThanOrEqual(
          mediaSource.updatedAt ?? Infinity
        );
      });

      it("updates the play state when requested to", async () => {
        // Act: Update the video play state
        const result = await service.updateMediaSource(mediaSource.id, {
          meta: {
            type: "video",
            state: "playing",
          },
        });

        // Assert: The result has the new play state
        expect(result).toEqual({
          ...mediaSource,
          meta: {
            ...mediaSource.meta,
            state: "playing",
          },
          updatedAt: expect.any(Number),
        });

        // Assert: The new update timestamp is at least the original one
        expect(result?.updatedAt).toBeGreaterThanOrEqual(
          mediaSource.updatedAt ?? Infinity
        );
      });
    });
  });

  describe("findMediaSource", () => {
    let mediaSource: MediaSourceDto;

    beforeEach(async () => {
      // Arrange: Create a media source
      mediaSource = await service.createMediaSource({
        meta: {
          type: "image",
          uri: "http://something.local",
        },
      });
    });

    it("returns a media source if it exists", async () => {
      // Act: Retrieve the media source
      const result = await service.findMediaSource(mediaSource.id);

      // Assert: The media source is exactly what we created
      expect(result).toEqual(mediaSource);
    });

    it("returns undefined if it does not exist", async () => {
      // Act: Retrieve the media source
      const result = await service.findMediaSource("some-random-id");

      // Assert: The media source is undefined
      expect(result).toBeUndefined();
    });
  });

  describe("findAllMediaSources", () => {
    const getRandomMediaSource = (): Omit<
      MediaSourceDto,
      "id" | "updatedAt"
    > => {
      switch (Math.floor(Math.random() * 3)) {
        case 0:
          return { meta: { type: "blank" } };
        case 1:
          return {
            meta: {
              type: "image",
              uri: "http://something.local",
            },
          };
        case 2:
          return {
            meta: {
              type: "video",
              uri: "http://something.local",
            },
          };
        default:
          throw new Error("Out of range.");
      }
    };

    it("returns all media sources", async () => {
      // Arrange: Create some media sources
      const mediaSources = await Promise.all(
        new Array(37)
          .fill(undefined)
          .map(() => service.createMediaSource(getRandomMediaSource()))
      ).then((arr) => arr.sort((a, b) => a.id.localeCompare(b.id)));

      // Act: Retrieve all media sources
      const result = await service
        .findAllMediaSources()
        .then((arr) => arr.sort((a, b) => a.id.localeCompare(b.id)));

      // Assert: We got all of the media sources we created
      expect(result).toEqual(mediaSources);
    });

    it("returns an empty array if there are no media sources", async () => {
      // Act: Retrieve all media sources
      const result = await service.findAllMediaSources();

      // Assert: There are no media sources
      expect(result).toHaveLength(0);
    });
  });

  describe("findScreensByMediaSourceId", () => {
    let mediaSource: MediaSourceDto;

    beforeEach(async () => {
      // Arrange: Create a media source
      mediaSource = await service.createMediaSource({
        meta: {
          type: "image",
          uri: "http://something.local",
        },
      });
    });

    it("returns screens linked to the media source if there are any", async () => {
      // Arrange: Create some screens linked to the media source, and some not
      const screensCount = 24;
      const screens = await Promise.all(
        new Array(screensCount).fill(undefined).map((_, i) =>
          service.createScreen({
            territory: 7,
            world: 74,
            position: {
              x: 20,
              y: 21,
              z: 22,
            },
            mediaSourceId: i % 2 === 0 ? mediaSource.id : undefined,
          })
        )
      ).then((arr) =>
        arr
          .filter((screen) => screen.mediaSourceId)
          .sort((a, b) => a.id.localeCompare(b.id))
      );

      // Validate number of linked screens
      expect(screens).toHaveLength(screensCount / 2);

      // Act: Retrieve all screens linked to the media source
      const result = await service
        .findScreensByMediaSourceId(mediaSource.id)
        .then((arr) => arr.sort((a, b) => a.id.localeCompare(b.id)));

      // Assert: The returned screens are only the ones linked to the media source
      expect(result).toEqual(screens);
    });

    it("returns an empty array if there are no screens for the media source", async () => {
      // Act: Retrieve all screens linked to the media source
      const result = await service
        .findScreensByMediaSourceId(mediaSource.id)
        .then((arr) => arr.sort((a, b) => a.id.localeCompare(b.id)));

      // Assert: The result is empty
      expect(result).toHaveLength(0);
    });
  });
});
