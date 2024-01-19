import { MediaSourceType, PlaybackState } from "@simulacrum/db";
import {
  AUTO_GENERATE_ATTRIBUTE_STRATEGY,
  Attribute,
  AutoGenerateAttribute,
  Entity,
} from "@typedorm/common";

@Entity({
  name: "mediaSource",
  primaryKey: {
    partitionKey: "MEDIASRC#{{id}}",
    sortKey: "MEDIASRC#{{id}}",
  },
})
export class MediaSource {
  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.UUID4,
  })
  id: string;

  @Attribute({ isEnum: true })
  type: MediaSourceType;

  @Attribute()
  uri?: string;

  @Attribute()
  playheadSeconds?: number;

  @Attribute()
  playheadUpdatedAt?: number;

  @Attribute({ isEnum: true })
  state?: PlaybackState;

  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.EPOCH_DATE,
    autoUpdate: true,
  })
  updatedAt: number;
}
