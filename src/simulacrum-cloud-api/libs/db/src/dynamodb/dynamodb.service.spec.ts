import { Test, TestingModule } from "@nestjs/testing";
import { StartedTestContainer, GenericContainer } from "testcontainers";
import { DynamoDbService } from "./dynamodb.service";
import {
  CreateTableCommand,
  CreateTableCommandInput,
  KeyType,
} from "@aws-sdk/client-dynamodb";
import { MediaSourceDto, ScreenDto } from "../common";

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
      .withExposedPorts({ container: 8000, host: 8000 })
      .start();

    // Update the environment with the container address
    process.env.SIMULACRUM_DDB_ENDPOINT = `http://localhost:${container.getMappedPort(
      8000
    )}`;

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
});
