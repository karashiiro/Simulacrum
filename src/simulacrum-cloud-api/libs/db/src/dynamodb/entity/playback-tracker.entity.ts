import {
  PlaybackTrackerDto,
  PlaybackTrackerState,
} from '@simulacrum/db/common';
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
    GSI1: {
      partitionKey: 'PBTRACKER#{{id}}',
      sortKey: 'PBTRACKER#STATE#{{state}}',
      type: INDEX_TYPE.GSI,
    },
  },
})
export class PlaybackTracker implements PlaybackTrackerDto {
  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.UUID4,
  })
  id: string;

  // TypeDORM seems to be doing something like `if (!default)`, so this needs to be a function to not be falsy
  @Attribute({ default: () => 0 })
  playheadSeconds: number;

  @Attribute({ isEnum: true, default: 'paused' })
  state: PlaybackTrackerState;

  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.EPOCH_DATE,
    autoUpdate: true,
  })
  updatedAt: number;
}
