import type { S3Handler } from "aws-lambda";
import {
  CreateJobCommand,
  MediaConvertClient,
} from "@aws-sdk/client-mediaconvert";
import * as crypto from "node:crypto";

const {
  MEDIACONVERT_ROLE,
  MEDIACONVERT_TEMPLATE,
  MEDIACONVERT_ENDPOINT,
  REGION,
} = process.env;

const mediaConvert = new MediaConvertClient({
  region: REGION,
  endpoint: MEDIACONVERT_ENDPOINT,
});

export const handler: S3Handler = async (event, _context) => {
  console.log(event);

  await Promise.allSettled(
    event.Records.filter(
      (r) =>
        r.eventSource === "aws:s3" && r.eventName.startsWith("ObjectCreated")
    ).map(async (record) => {
      const objectUri = `s3://${record.s3.bucket.name}/${record.s3.object.key}`;
      try {
        await mediaConvert.send(
          new CreateJobCommand({
            ClientRequestToken: crypto.randomUUID(),
            Role: MEDIACONVERT_ROLE,
            Settings: {
              Inputs: [
                {
                  FileInput: objectUri,
                },
              ],
            },
            JobTemplate: MEDIACONVERT_TEMPLATE,
          })
        );

        console.log(`Created MediaConvert job for object ${objectUri}`);
      } catch (err) {
        console.error(
          `Failed to create MediaConvert job for object ${objectUri}\n${err}`
        );
      }
    })
  );
};
