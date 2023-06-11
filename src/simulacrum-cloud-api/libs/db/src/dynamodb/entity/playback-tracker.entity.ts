import { PlaybackTrackerDto } from '@simulacrum/db/common/entities';
import {
  Attribute,
  Entity,
  AutoGenerateAttribute,
  AUTO_GENERATE_ATTRIBUTE_STRATEGY,
  INDEX_TYPE,
} from '@typedorm/common';

@Entity({
  name: 'playbackTracker',
  primaryKey: {
    partitionKey: 'PBTRACKER#{{id}}',
    sortKey: 'PBTRACKER#{{id}}',
  },
  indexes: {
    LSI1: {
      sortKey: 'PLAYHEAD#UPDATED_AT#{{updatedAt}}',
      type: INDEX_TYPE.LSI,
    },
  },
})
export class PlaybackTracker implements PlaybackTrackerDto {
  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.UUID4,
  })
  id: string;

  @Attribute()
  playheadSeconds: number;

  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.EPOCH_DATE,
    autoUpdate: true,
  })
  updatedAt: number;
}
