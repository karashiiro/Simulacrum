import { CloudFrontToS3 } from "@aws-solutions-constructs/aws-cloudfront-s3";
import * as cdk from "aws-cdk-lib";
import * as iam from "aws-cdk-lib/aws-iam";
import * as lambda from "aws-cdk-lib/aws-lambda";
import * as nodejs from "aws-cdk-lib/aws-lambda-nodejs";
import * as mediaconvert from "aws-cdk-lib/aws-mediaconvert";
import * as s3 from "aws-cdk-lib/aws-s3";
import * as s3n from "aws-cdk-lib/aws-s3-notifications";
import { Construct } from "constructs";
import path from "path";
import hlsConverterTemplate from "./aws-mediaconvert-template-hls-converter.json";
import { MediaConvertEndpoint } from "./resources/mediaconvert-endpoint";

export class SimulacrumCloudAwsCdkStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const cfs3 = new CloudFrontToS3(this, "CloudFrontS3", {});
    const uploadBucket = new s3.Bucket(this, "UploadBucket");
    const outputBucket = cfs3.s3BucketInterface;

    const hlsTemplate = new mediaconvert.CfnJobTemplate(
      this,
      "HLSConverterTemplate",
      {
        settingsJson: hlsConverterTemplate.Settings,
      }
    );

    const mediaConvertRole = new iam.Role(this, "MediaConvertRole", {
      assumedBy: new iam.ServicePrincipal("mediaconvert.amazonaws.com"),
    });

    const mediaConvertEndpoint = new MediaConvertEndpoint(
      this,
      "MediaConvertEndpoint"
    );

    const mediaConvertDispatcher = new nodejs.NodejsFunction(
      this,
      "MediaConvertDispatcher",
      {
        entry: path.join(__dirname, "mediaconvert-dispatcher.handler.ts"),
        bundling: {
          externalModules: ["@aws-sdk/client-mediaconvert"],
        },
        runtime: lambda.Runtime.NODEJS_18_X,
        environment: {
          MEDIACONVERT_ROLE: mediaConvertRole.roleArn,
          MEDIACONVERT_TEMPLATE: hlsTemplate.attrArn,
          MEDIACONVERT_ENDPOINT: mediaConvertEndpoint.endpoint,
          REGION: cdk.Aws.REGION,
        },
      }
    );

    mediaConvertDispatcher.addToRolePolicy(
      new iam.PolicyStatement({
        actions: ["iam:PassRole"],
        resources: [mediaConvertRole.roleArn],
      })
    );

    mediaConvertDispatcher.addToRolePolicy(
      new iam.PolicyStatement({
        actions: ["mediaconvert:CreateJob"],
        resources: [
          `arn:${cdk.Aws.PARTITION}:mediaconvert:${cdk.Aws.REGION}:${cdk.Aws.ACCOUNT_ID}:*`,
        ],
      })
    );

    outputBucket.grantReadWrite(mediaConvertRole);
    uploadBucket.grantRead(mediaConvertRole);
    uploadBucket.addEventNotification(
      s3.EventType.OBJECT_CREATED,
      new s3n.LambdaDestination(mediaConvertDispatcher)
    );

    new cdk.CfnOutput(this, "MediaConvertRegionEndpoint", {
      value: mediaConvertEndpoint.endpoint,
    });
    new cdk.CfnOutput(this, "CloudFrontDomainName", {
      value: cfs3.cloudFrontWebDistribution.domainName,
    });
    new cdk.CfnOutput(this, "UploadBucketArn", {
      value: uploadBucket.bucketArn ?? "",
    });
  }
}
