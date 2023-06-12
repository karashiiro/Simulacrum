import { MediaSourceDto, MediaSourceType } from '@simulacrum/db/common';
import {
  Attribute,
  Entity,
  AutoGenerateAttribute,
  AUTO_GENERATE_ATTRIBUTE_STRATEGY,
  INDEX_TYPE,
} from '@typedorm/common';

@Entity({
  name: 'mediaSource',
  primaryKey: {
    partitionKey: 'MEDIASRC#{{id}}',
    sortKey: 'MEDIASRC#{{id}}',
  },
  indexes: {
    GSI1: {
      partitionKey: 'PBTRACKER#{{playbackTrackerId}}',
      sortKey: 'MEDIASRC#TYPE#{{type}}',
      type: INDEX_TYPE.GSI,
    },
  },
})
export class MediaSource implements MediaSourceDto {
  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.UUID4,
  })
  id: string;

  playbackTrackerId?: string;

  @Attribute({ isEnum: true, default: 'video' })
  type: MediaSourceType;

  @Attribute()
  uri?: string;

  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.EPOCH_DATE,
    autoUpdate: true,
  })
  updatedAt: number;
}
