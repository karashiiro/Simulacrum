import {
  CreateTableCommandInput,
  KeyType,
  ProjectionType,
} from "@aws-sdk/client-dynamodb";
import { GenericContainer, StartedTestContainer, Wait } from "testcontainers";

export const ddbLocalTableParams: CreateTableCommandInput = {
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
        { AttributeName: "PK", KeyType: KeyType.HASH },
        { AttributeName: "LSI1SK", KeyType: KeyType.RANGE },
      ],
      Projection: { ProjectionType: ProjectionType.ALL },
    },
  ],
  GlobalSecondaryIndexes: [
    {
      IndexName: "GSI1",
      KeySchema: [
        { AttributeName: "GSI1PK", KeyType: KeyType.HASH },
        { AttributeName: "GSI1SK", KeyType: KeyType.RANGE },
      ],
      Projection: { ProjectionType: ProjectionType.ALL },
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

export const expectUUIDv4 = () => {
  // https://github.com/afram/is-uuid/blob/master/lib/is-uuid.js
  return expect.stringMatching(
    /^[0-9a-f]{8}-[0-9a-f]{4}-[4][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
  );
};

export function getDdbLocalProcessEnv(container: StartedTestContainer) {
  return {
    SIMULACRUM_DDB_ENDPOINT: `http://${container.getHost()}:${container.getMappedPort(
      8000
    )}`,
    AWS_REGION: "us-east-1",
    AWS_ACCESS_KEY_ID: "AccessKeyId",
    AWS_SECRET_ACCESS_KEY: "SecretAccessKey",
    AWS_SESSION_TOKEN: "SessionToken",
  };
}

export function createDdbLocalTestContainer(hostPort: number) {
  return new GenericContainer("amazon/dynamodb-local:latest")
    .withExposedPorts({
      // I keep getting errors like "Error: connect ECONNREFUSED ::1:32845" with no useful
      // stack trace after a couple of tests unless I use a fixed host port.
      container: 8000,
      host: hostPort,
    })
    .withWaitStrategy(
      Wait.forAll([
        Wait.forListeningPorts(),
        Wait.forLogMessage(
          "Initializing DynamoDB Local with the following configuration:"
        ),
      ])
    );
}
