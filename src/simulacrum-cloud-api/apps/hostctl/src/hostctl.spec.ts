import { Test, TestingModule } from "@nestjs/testing";
import { INestApplication } from "@nestjs/common";
import { HostctlModule } from "./hostctl.module";
import { CreateTableCommand } from "@aws-sdk/client-dynamodb";
import {
  createDdbLocalTestContainer,
  getDdbLocalProcessEnv,
  ddbLocalTableParams,
  expectUUIDv4,
} from "@simulacrum/db/test";
import { DynamoDbService, MediaSourceDto } from "@simulacrum/db";
import { StartedTestContainer } from "testcontainers";
import { RawData, WebSocket } from "ws";
import { WsResponse } from "@nestjs/websockets";
import { WsAdapter } from "@nestjs/platform-ws";
import * as assert from "assert";

describe("hostctl (e2e)", () => {
  let container: StartedTestContainer;
  let env: typeof process.env;
  let app: INestApplication;
  let client: WebSocket;

  const sendMessage = (message: WsResponse<unknown>): Promise<void> => {
    return new Promise<void>((resolve, reject) => {
      client.send(JSON.stringify(message), (err) => {
        if (err) {
          reject(err);
        }

        resolve();
      });
    });
  };

  beforeEach(async () => {
    // Save environment
    env = { ...process.env };

    // Spin up a DDB local instance
    container = await createDdbLocalTestContainer(4398).start();

    // Update the environment with the container access config
    Object.assign(process.env, getDdbLocalProcessEnv(container));

    // Create service testing module
    const moduleFixture: TestingModule = await Test.createTestingModule({
      imports: [HostctlModule],
    }).compile();

    // Create a table for testing
    await DynamoDbService.client.send(
      new CreateTableCommand(ddbLocalTableParams)
    );

    // Create and start a full application instance
    app = moduleFixture.createNestApplication();
    app.useWebSocketAdapter(new WsAdapter(app));
    await app.listen(4399);

    // Initialize a client instance
    client = new WebSocket("ws://localhost:4399");

    // Wait for the client to connect to the server
    await new Promise<void>((resolve, reject) => {
      const onerror = (err: Error) => reject(err);
      client.once("error", onerror);
      client.once("open", () => {
        client.removeListener("error", onerror);
        resolve();
      });
    });
  }, 30000);

  afterEach(async () => {
    // Disconnect the client
    client.close();

    // Stop the application
    await app.close();

    // Wait a bit so that the AWS SDK can do its thing and we don't get a TimeoutError in the logs
    await new Promise<void>((resolve) => setTimeout(resolve, 5000));

    // Stop the container
    await container.stop();

    // Restore environment
    process.env = env;
  }, 15000);

  // Helper function to send a message and assert on the next received message
  const communicate = (
    message: WsResponse<unknown>,
    onResponse: (response: object) => void
  ): Promise<void> => {
    return new Promise<void>(async (resolve, reject) => {
      const onMessage = (data: RawData) => {
        client.removeListener("message", onMessage);
        try {
          const response = JSON.parse(data.toString());
          onResponse(response);
          resolve();
        } catch (err) {
          reject(err);
        }
      };

      client.on("message", onMessage);
      await sendMessage(message);
    });
  };

  describe("playhead syncing", () => {
    it("syncs correctly for a single client", async () => {
      // Create a video source
      const videoSource = {
        meta: {
          type: "video",
          uri: "http://something.local/wow.m3u8",
        },
      };

      // Send a message to create the video source
      let videoSourceComplete: MediaSourceDto = {
        id: "",
        meta: { type: "blank" },
        updatedAt: 0,
      };
      await communicate(
        {
          event: "MEDIA_SOURCE_CREATE",
          data: {
            mediaSource: videoSource,
          },
        },
        (res) => {
          // Assert: The response is a MEDIA_SOURCE_CREATE and has the created video source
          expect(res).toEqual({
            event: "MEDIA_SOURCE_CREATE",
            data: {
              mediaSource: {
                id: expectUUIDv4(),
                updatedAt: expect.any(Number),
                meta: {
                  ...videoSource.meta,
                  playheadSeconds: 0,
                  playheadUpdatedAt: expect.any(Number),
                  state: "paused",
                },
              },
            },
          });

          videoSourceComplete = (
            res as WsResponse<{ mediaSource: MediaSourceDto }>
          ).data.mediaSource;
        }
      );

      // Create a linked screen
      const screen = {
        territory: 7,
        world: 74,
        position: {
          x: 20,
          y: 21,
          z: 22,
        },
        mediaSourceId: videoSourceComplete.id,
      };

      // Send a message to create the screen
      await communicate(
        {
          event: "SCREEN_CREATE",
          data: {
            screen: screen,
          },
        },
        (res) => {
          // Assert: The response is a SCREEN_CREATE and has the created screen
          expect(res).toEqual({
            event: "SCREEN_CREATE",
            data: {
              screen: {
                ...screen,
                id: expectUUIDv4(),
                updatedAt: expect.any(Number),
              },
            },
          });
        }
      );

      // Dummy assertions to narrow the type
      assert(videoSourceComplete.meta.type === "video");
      assert(videoSourceComplete.meta.state);
      assert(videoSourceComplete.meta.playheadUpdatedAt);
      assert(videoSourceComplete.meta.playheadSeconds !== undefined);

      // Set up some "local" state for the video
      const localVideo = {
        playheadSeconds: videoSourceComplete.meta.playheadSeconds,
        playheadSync: videoSourceComplete.meta.playheadUpdatedAt,
        playState: videoSourceComplete.meta.state,
      };

      expect(localVideo.playState).toEqual("paused");

      // Play the video
      await communicate(
        {
          event: "VIDEO_SOURCE_PLAY",
          data: {
            id: videoSourceComplete.id,
          },
        },
        (res) => {
          // Assert: The response is a VIDEO_SOURCE_PLAY and is now playing
          expect(res).toEqual({
            event: "VIDEO_SOURCE_PLAY",
            data: {
              mediaSource: expect.objectContaining({
                id: videoSourceComplete.id,
                meta: expect.objectContaining({
                  type: "video",
                  playheadSeconds: localVideo.playheadSeconds,
                  state: "playing",
                }),
              }),
            },
          });

          // Type narrowing
          const resTyped = res as WsResponse<{ mediaSource: MediaSourceDto }>;
          assert(resTyped.data.mediaSource.meta.type === "video");
          assert(resTyped.data.mediaSource.meta.state);
          assert(resTyped.data.mediaSource.meta.playheadUpdatedAt);
          assert(resTyped.data.mediaSource.meta.playheadSeconds !== undefined);

          // Update local state
          localVideo.playheadSeconds =
            resTyped.data.mediaSource.meta.playheadSeconds;
          localVideo.playheadSync =
            resTyped.data.mediaSource.meta.playheadUpdatedAt;
          localVideo.playState = resTyped.data.mediaSource.meta.state;
        }
      );

      // Pan forward a few seconds
      await communicate(
        {
          event: "VIDEO_SOURCE_PAN",
          data: {
            id: videoSourceComplete.id,
            playheadSeconds: localVideo.playheadSeconds + 6,
          },
        },
        (res) => {
          // Assert: The response is a VIDEO_SOURCE_PAN and had its playhead updated
          expect(res).toEqual({
            event: "VIDEO_SOURCE_PAN",
            data: {
              mediaSource: expect.objectContaining({
                id: videoSourceComplete.id,
                meta: expect.objectContaining({
                  type: "video",
                  playheadSeconds: localVideo.playheadSeconds + 6,
                  state: localVideo.playState,
                }),
              }),
            },
          });

          // Type narrowing
          const resTyped = res as WsResponse<{ mediaSource: MediaSourceDto }>;
          assert(resTyped.data.mediaSource.meta.type === "video");
          assert(resTyped.data.mediaSource.meta.state);
          assert(resTyped.data.mediaSource.meta.playheadUpdatedAt);
          assert(resTyped.data.mediaSource.meta.playheadSeconds !== undefined);

          // Update local state
          localVideo.playheadSeconds =
            resTyped.data.mediaSource.meta.playheadSeconds;
          localVideo.playheadSync =
            resTyped.data.mediaSource.meta.playheadUpdatedAt;
          localVideo.playState = resTyped.data.mediaSource.meta.state;
        }
      );

      // Pause the video
      await communicate(
        {
          event: "VIDEO_SOURCE_PAUSE",
          data: {
            id: videoSourceComplete.id,
          },
        },
        (res) => {
          // Assert: The response is a VIDEO_SOURCE_PAUSE and had its playhead updated
          expect(res).toEqual({
            event: "VIDEO_SOURCE_PAUSE",
            data: {
              mediaSource: expect.objectContaining({
                id: videoSourceComplete.id,
                meta: expect.objectContaining({
                  type: "video",
                  playheadSeconds: localVideo.playheadSeconds,
                  state: "paused",
                }),
              }),
            },
          });

          // Type narrowing
          const resTyped = res as WsResponse<{ mediaSource: MediaSourceDto }>;
          assert(resTyped.data.mediaSource.meta.type === "video");
          assert(resTyped.data.mediaSource.meta.state);
          assert(resTyped.data.mediaSource.meta.playheadUpdatedAt);
          assert(resTyped.data.mediaSource.meta.playheadSeconds !== undefined);

          // Update local state
          localVideo.playheadSeconds =
            resTyped.data.mediaSource.meta.playheadSeconds;
          localVideo.playheadSync =
            resTyped.data.mediaSource.meta.playheadUpdatedAt;
          localVideo.playState = resTyped.data.mediaSource.meta.state;
        }
      );

      // Maybe we need to sync again
      await communicate(
        {
          event: "VIDEO_SOURCE_SYNC",
          data: {
            id: videoSourceComplete.id,
          },
        },
        (res) => {
          // Assert: The response is a VIDEO_SOURCE_SYNC and nothing important has changed
          expect(res).toEqual({
            event: "VIDEO_SOURCE_SYNC",
            data: {
              mediaSource: expect.objectContaining({
                id: videoSourceComplete.id,
                meta: expect.objectContaining({
                  type: "video",
                  playheadSeconds: localVideo.playheadSeconds,
                  playheadUpdatedAt: localVideo.playheadSync,
                  state: localVideo.playState,
                }),
              }),
            },
          });
        }
      );
    });
  });
});
