import { MediaMetadata, MediaSourceDto } from '@simulacrum/db/common';
import {
  AUTO_GENERATE_ATTRIBUTE_STRATEGY,
  Attribute,
  AutoGenerateAttribute,
  Entity,
} from '@typedorm/common';

@Entity({
  name: 'mediaSource',
  primaryKey: {
    partitionKey: 'MEDIASRC#{{id}}',
    sortKey: 'MEDIASRC#{{id}}',
  },
})
export class MediaSource implements MediaSourceDto {
  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.UUID4,
  })
  id: string;

  @Attribute()
  meta: MediaMetadata;

  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.EPOCH_DATE,
    autoUpdate: true,
  })
  updatedAt: number;
}
