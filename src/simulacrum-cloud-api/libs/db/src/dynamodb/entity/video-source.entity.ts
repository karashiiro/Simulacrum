import { VideoSourceDto, PlaybackState } from '@simulacrum/db/common';
import { Attribute, Entity, INDEX_TYPE } from '@typedorm/common';
import { MediaSource } from './media-source.entity';

@Entity({
  name: 'videoSource',
  primaryKey: {
    partitionKey: 'VIDEOSRC#{{id}}',
    sortKey: 'VIDEOSRC#{{id}}',
  },
  indexes: {
    GSI1: {
      partitionKey: 'MEDIASRC#{{id}}',
      sortKey: 'MEDIASRC#TYPE#{{type}}',
      type: INDEX_TYPE.GSI,
    },
  },
})
export class VideoSource extends MediaSource implements VideoSourceDto {
  @Attribute()
  uri: string;

  // TypeDORM seems to be doing something like `if (!default)`, so this needs to be a function to not be falsy
  @Attribute({ default: () => 0 })
  playheadSeconds: number;

  @Attribute({ isEnum: true, default: 'paused' })
  state: PlaybackState;
}
