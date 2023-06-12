import { VideoSourceDto, PlaybackState } from '@simulacrum/db/common';
import {
  AUTO_GENERATE_ATTRIBUTE_STRATEGY,
  Attribute,
  AutoGenerateAttribute,
  Entity,
} from '@typedorm/common';

@Entity({
  name: 'videoSource',
  primaryKey: {
    partitionKey: 'VIDEOSRC#{{id}}',
    sortKey: 'VIDEOSRC#{{id}}',
  },
})
export class VideoSource implements VideoSourceDto {
  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.UUID4,
  })
  id: string;

  @Attribute()
  uri: string;

  // TypeDORM seems to be doing something like `if (!default)`, so this needs to be a function to not be falsy
  @Attribute({ default: () => 0 })
  playheadSeconds: number;

  @Attribute({ isEnum: true, default: 'paused' })
  state: PlaybackState;

  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.EPOCH_DATE,
    autoUpdate: true,
  })
  updatedAt: number;
}
