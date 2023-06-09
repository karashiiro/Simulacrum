import {
  DescribeEndpointsCommand,
  MediaConvertClient,
} from "@aws-sdk/client-mediaconvert";
import type {
  CloudFormationCustomResourceEvent,
  CloudFormationCustomResourceHandler,
  CloudFormationCustomResourceResponse,
  Context,
} from "aws-lambda";
import * as crypto from "node:crypto";

const { REGION } = process.env;

const mediaConvert = new MediaConvertClient({
  region: REGION,
});

const reply = (
  event: CloudFormationCustomResourceEvent,
  context: Context,
  status: "SUCCESS" | "FAILED",
  endpoint?: string
) => {
  return fetch(event.ResponseURL, {
    method: "PUT",
    headers: new Headers([["Content-Type", "application/json"]]),
    body: JSON.stringify({
      Status: status,
      Reason:
        "See the details in CloudWatch Log Stream: " + context.logStreamName,
      PhysicalResourceId: event.LogicalResourceId,
      StackId: event.StackId,
      RequestId: event.RequestId,
      LogicalResourceId: event.LogicalResourceId,
      Data: endpoint
        ? {
            UUID: crypto.randomUUID(),
            Endpoint: endpoint,
          }
        : {
            UUID: crypto.randomUUID(),
          },
    } as CloudFormationCustomResourceResponse),
  });
};

export const handler: CloudFormationCustomResourceHandler = async (
  event,
  context
) => {
  console.log(event);

  try {
    if (event.RequestType === "Create") {
      const { Endpoints } = await mediaConvert.send(
        new DescribeEndpointsCommand({})
      );

      if (!Endpoints?.[0]?.Url) {
        throw new Error("No endpoints were returned by DescribeEndpoints.");
      }

      const regionEndpoint = Endpoints[0].Url;

      console.log(`Found region endpoint: ${regionEndpoint}`);

      await reply(event, context, "SUCCESS", regionEndpoint);
    } else {
      console.log("Not a create event; nothing to do");
      await reply(event, context, "SUCCESS");
    }
  } catch (err) {
    console.error(`Failed to process request: ${err}`);
    await reply(event, context, "FAILED");
  }
};
