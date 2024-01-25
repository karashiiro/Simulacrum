import assert from "assert";
import { Aws, CfnOutput, Duration } from "aws-cdk-lib";
import { WebSocketApi, WebSocketStage } from "aws-cdk-lib/aws-apigatewayv2";
import { WebSocketLambdaIntegration } from "aws-cdk-lib/aws-apigatewayv2-integrations";
import { DockerImageAsset } from "aws-cdk-lib/aws-ecr-assets";
import {
  DockerImageCode,
  DockerImageFunction,
  IFunction,
} from "aws-cdk-lib/aws-lambda";
import { Construct } from "constructs";
import { workspaceRootSync } from "workspace-root";

export interface SimulacrumServerProps {
  tableName: string;
}

export class SimulacrumServer extends Construct {
  readonly handler: IFunction;

  constructor(scope: Construct, id: string, props: SimulacrumServerProps) {
    super(scope, id);

    // Get the repository root
    const wsRoot = workspaceRootSync();
    assert(wsRoot);

    // Build the container image
    const imageAsset = new DockerImageAsset(this, "SimulacrumImage", {
      directory: wsRoot,
      file: "src/simulacrum-cloud-api/Dockerfile.lambda",
    });

    // Create a service with the built container image
    this.handler = new DockerImageFunction(this, "SimulacrumServiceFunction", {
      code: DockerImageCode.fromEcr(imageAsset.repository, {
        tagOrDigest: imageAsset.imageTag,
      }),
      environment: {
        SIMULACRUM_DDB_ENDPOINT: `https://dynamodb.${Aws.REGION}.amazonaws.com`,
        SIMULACRUM_DDB_TABLE: props.tableName,
        NO_COLOR: "1",
      },
      timeout: Duration.seconds(15),
    });

    const wsApi = new WebSocketApi(this, "SimulacrumServiceAPI", {
      description: "The Simulacrum WebSocket API.",
      defaultRouteOptions: {
        integration: new WebSocketLambdaIntegration(
          "DefaultIntegration",
          this.handler
        ),
      },
      connectRouteOptions: {
        integration: new WebSocketLambdaIntegration(
          "ConnectIntegration",
          this.handler
        ),
      },
      disconnectRouteOptions: {
        integration: new WebSocketLambdaIntegration(
          "DisconnectIntegration",
          this.handler
        ),
      },
    });

    wsApi.grantManageConnections(this.handler);

    const prodStage = new WebSocketStage(this, "ProdStage", {
      webSocketApi: wsApi,
      stageName: "prod",
      autoDeploy: true,
    });

    new CfnOutput(this, "ServiceURL", {
      value: prodStage.url,
    });
  }
}
