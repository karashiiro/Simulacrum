import { CloudFrontToS3 } from "@aws-solutions-constructs/aws-cloudfront-s3";
import * as cdk from "aws-cdk-lib";
import { Construct } from "constructs";

export class SimulacrumCloudAwsCdkStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const cfs3 = new CloudFrontToS3(this, "CloudFrontS3", {});

    new cdk.CfnOutput(this, "CloudFrontDomainName", {
      value: cfs3.cloudFrontWebDistribution.domainName,
    });
    new cdk.CfnOutput(this, "CloudFrontS3BucketArn", {
      value: cfs3.s3Bucket?.bucketArn ?? "",
    });
  }
}
