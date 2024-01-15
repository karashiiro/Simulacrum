import { Test, TestingModule } from "@nestjs/testing";
import { DbService } from "./db.service";
import { DynamoDbService } from "./dynamodb/dynamodb.service";

describe("DbService", () => {
  let service: DbService;

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      providers: [DbService],
    })
      .useMocker((token) => {
        if (token === DynamoDbService) {
          // TODO
          return class {};
        }
      })
      .compile();

    service = module.get<DbService>(DbService);
  });

  it("should be defined", () => {
    expect(service).toBeDefined();
  });
});
