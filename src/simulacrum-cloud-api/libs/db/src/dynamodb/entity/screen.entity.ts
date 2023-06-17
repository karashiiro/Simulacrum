import { Position, ScreenDto } from "@simulacrum/db/common";
import {
  AUTO_GENERATE_ATTRIBUTE_STRATEGY,
  Attribute,
  AutoGenerateAttribute,
  Entity,
  INDEX_TYPE,
} from "@typedorm/common";

@Entity({
  name: "screen",
  primaryKey: {
    partitionKey: "SCREEN#{{id}}",
    sortKey: "SCREEN#{{id}}",
  },
  indexes: {
    GSI1: {
      partitionKey: "MEDIASRC#{{mediaSourceId}}",
      sortKey: "SCREEN#{{id}}",
      type: INDEX_TYPE.GSI,
    },
  },
})
export class Screen implements ScreenDto {
  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.UUID4,
  })
  id: string;

  @Attribute()
  territory: number;

  @Attribute()
  position: Position;

  @Attribute()
  mediaSourceId?: string;

  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.EPOCH_DATE,
    autoUpdate: true,
  })
  updatedAt: number;
}
