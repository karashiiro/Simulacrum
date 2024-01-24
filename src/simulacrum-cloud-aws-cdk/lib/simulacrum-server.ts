import assert from "assert";
import { Aws } from "aws-cdk-lib";
import { DockerImageAsset, Platform } from "aws-cdk-lib/aws-ecr-assets";
import {
  ContainerImage,
  CpuArchitecture,
  OperatingSystemFamily,
} from "aws-cdk-lib/aws-ecs";
import { ApplicationLoadBalancedFargateService } from "aws-cdk-lib/aws-ecs-patterns";
import { Role, ServicePrincipal } from "aws-cdk-lib/aws-iam";
import { Construct } from "constructs";
import { workspaceRootSync } from "workspace-root";

export interface SimulacrumServerProps {
  tableName: string;
}

export class SimulacrumServer extends Construct {
  readonly executionRole: Role;

  readonly service: ApplicationLoadBalancedFargateService;

  constructor(scope: Construct, id: string, props: SimulacrumServerProps) {
    super(scope, id);

    // Get the repository root
    const wsRoot = workspaceRootSync();
    assert(wsRoot);

    // Build the container image
    const imageAsset = new DockerImageAsset(this, "SimulacrumImage", {
      directory: wsRoot,
      file: "src/simulacrum-cloud-api/Dockerfile",
    });

    // Create the execution role
    this.executionRole = new Role(this, "SimulacrumServiceRole", {
      assumedBy: new ServicePrincipal("ecs-tasks.amazonaws.com"),
    });

    // Create a service with the built container image
    this.service = new ApplicationLoadBalancedFargateService(
      this,
      "SimulacrumService",
      {
        taskImageOptions: {
          image: ContainerImage.fromDockerImageAsset(imageAsset),
          containerPort: 3000,
          environment: {
            SIMULACRUM_DDB_ENDPOINT: `https://dynamodb.${Aws.REGION}.amazonaws.com`,
            SIMULACRUM_DDB_TABLE: props.tableName,
            NO_COLOR: "1",
          },
          executionRole: this.executionRole,
        },
        memoryLimitMiB: 1024,
        desiredCount: 1,
        cpu: 512,
      }
    );

    this.service.targetGroup.configureHealthCheck({
      path: "/health",
    });
  }
}
