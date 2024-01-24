import { Table, INDEX_TYPE } from "@typedorm/common";

// https://aws.amazon.com/blogs/database/single-table-vs-multi-table-design-in-amazon-dynamodb/
// https://aws.amazon.com/blogs/compute/creating-a-single-table-design-with-amazon-dynamodb/
export const table = new Table({
  name: process.env.SIMULACRUM_DDB_TABLE || "Simulacrum",
  partitionKey: "PK",
  sortKey: "SK",
  indexes: {
    GSI1: {
      type: INDEX_TYPE.GSI,
      partitionKey: "GSI1PK",
      sortKey: "GSI1SK",
    },
    LSI1: {
      type: INDEX_TYPE.LSI,
      sortKey: "LSI1SK",
    },
  },
});
