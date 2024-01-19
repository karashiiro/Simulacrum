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
import { DynamoDbService } from "@simulacrum/db";
import { StartedTestContainer } from "testcontainers";
import { WebSocket } from "ws";
import { WsResponse } from "@nestjs/websockets";
import { WsAdapter } from "@nestjs/platform-ws";

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
  }, 15000);

  afterEach(async () => {
    // TODO: There's a TimeoutError in the AWS SDK after this point, why?

    // Disconnect the client
    client.close();

    // Stop the container
    await container.stop();

    // Stop the application
    await app.close();

    // Restore environment
    process.env = env;
  }, 15000);

  it("works", async () => {
    // Arrange: Create a screen
    const screen = {
      territory: 7,
      world: 74,
      position: {
        x: 20,
        y: 21,
        z: 22,
      },
    };

    // Arrange: Prepare an event handler
    client.once("message", (data) => {
      const message = JSON.parse(data.toString());

      // Assert: The message is a SCREEN_CREATE
      expect(message.event).toEqual("SCREEN_CREATE");

      // Assert: The message has the created screen
      expect(message.data).toEqual({
        ...screen,
        id: expectUUIDv4(),
        updatedAt: expect.any(Number),
      });
    });

    // Act: Send a message to create a screen
    await sendMessage({
      event: "SCREEN_CREATE",
      data: {
        screen: screen,
      },
    });
  });
});
