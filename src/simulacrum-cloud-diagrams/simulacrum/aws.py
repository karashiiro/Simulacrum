from diagrams import Cluster, Diagram, Edge
from diagrams.aws.compute import Lambda, Fargate
from diagrams.aws.database import Dynamodb
from diagrams.aws.media import ElementalMediaconvert
from diagrams.aws.network import CloudFront, ALB
from diagrams.aws.storage import S3
from diagrams.onprem.client import Client

from simulacrum.common import ServerBoundEdge, ClientBoundEdge, BidirectionalEdge


with Diagram("Simulacrum", filename="simulacrum/aws", show=False):
    client = Client("Client")

    with Cluster("Media Transcoding Pipeline"):
        upload_bucket = S3("Upload Bucket")
        distribution_bucket = S3("Distribution Bucket")
        mediaconvert = ElementalMediaconvert("MediaConvert")

        (
            client
            >> ServerBoundEdge()
            >> upload_bucket
            >> Edge(label="notify")
            >> Lambda("Dispatcher")
            >> Edge(label="dispatch job")
            >> mediaconvert
        )

        mediaconvert >> upload_bucket
        mediaconvert >> distribution_bucket

        client << ClientBoundEdge() << CloudFront("CloudFront") >> distribution_bucket

    with Cluster("API"):
        (
            client
            >> BidirectionalEdge()
            << ALB("ALB")
            >> Fargate("WebSocket API")
            >> Dynamodb("DynamoDB")
        )
