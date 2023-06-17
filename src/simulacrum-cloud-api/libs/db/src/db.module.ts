import { Module } from "@nestjs/common";
import { DbService } from "./db.service";
import { DynamoDbModule } from "./dynamodb/dynamodb.module";

@Module({
  providers: [DbService],
  exports: [DbService],
  imports: [DynamoDbModule],
})
export class DbModule {}
