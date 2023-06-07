import { CloudFrontToS3 } from "@aws-solutions-constructs/aws-cloudfront-s3";
import * as cdk from "aws-cdk-lib";
import { Construct } from "constructs";

export class SimulacrumCloudAwsCdkStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    new CloudFrontToS3(this, "SimulacrumCloudFrontS3", {});
  }
}
