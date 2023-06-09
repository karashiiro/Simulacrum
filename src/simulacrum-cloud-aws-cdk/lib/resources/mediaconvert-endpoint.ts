import * as cdk from "aws-cdk-lib";
import * as iam from "aws-cdk-lib/aws-iam";
import * as lambda from "aws-cdk-lib/aws-lambda";
import * as nodejs from "aws-cdk-lib/aws-lambda-nodejs";
import { Construct } from "constructs";
import path from "path";

export class MediaConvertEndpoint extends Construct {
  readonly endpoint: string;

  constructor(scope: Construct, id: string) {
    super(scope, id);

    const customResourceRole = new iam.Role(
      this,
      "MediaConvertEndpointCustomResourceRole",
      {
        assumedBy: new iam.ServicePrincipal("lambda.amazonaws.com"),
      }
    );

    const customResourcePolicy = new iam.Policy(
      this,
      "MediaConvertEndpointCustomResourcePolicy",
      {
        statements: [
          new iam.PolicyStatement({
            actions: ["mediaconvert:DescribeEndpoints"],
            resources: [
              `arn:${cdk.Aws.PARTITION}:mediaconvert:${cdk.Aws.REGION}:${cdk.Aws.ACCOUNT_ID}:*`,
            ],
          }),
          new iam.PolicyStatement({
            actions: [
              "logs:CreateLogGroup",
              "logs:CreateLogStream",
              "logs:PutLogEvents",
            ],
            resources: ["*"],
          }),
        ],
      }
    );
    customResourcePolicy.attachToRole(customResourceRole);

    const customResourceLambda = new nodejs.NodejsFunction(
      this,
      "MediaConvertEndpointCustomResource",
      {
        entry: path.join(__dirname, "mediaconvert-endpoint.handler.ts"),
        bundling: {
          externalModules: ["@aws-sdk/client-mediaconvert"],
        },
        runtime: lambda.Runtime.NODEJS_18_X,
        environment: {
          REGION: cdk.Aws.REGION,
        },
        timeout: cdk.Duration.seconds(30),
        role: customResourceRole,
      }
    );

    customResourceLambda.node.addDependency(customResourceRole);
    customResourceLambda.node.addDependency(customResourcePolicy);

    const customResourceEndpoint = new cdk.CustomResource(this, "Endpoint", {
      serviceToken: customResourceLambda.functionArn,
    });

    this.endpoint = customResourceEndpoint.getAttString("Endpoint");
  }
}
