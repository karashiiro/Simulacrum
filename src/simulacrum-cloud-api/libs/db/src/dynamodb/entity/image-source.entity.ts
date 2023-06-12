import { ImageSourceDto } from '@simulacrum/db/common';
import { Attribute, Entity, INDEX_TYPE } from '@typedorm/common';
import { MediaSource } from './media-source.entity';

@Entity({
  name: 'imageSource',
  primaryKey: {
    partitionKey: 'IMAGESRC#{{id}}',
    sortKey: 'IMAGESRC#{{id}}',
  },
  indexes: {
    GSI1: {
      partitionKey: 'MEDIASRC#{{id}}',
      sortKey: 'MEDIASRC#TYPE#{{type}}',
      type: INDEX_TYPE.GSI,
    },
  },
})
export class ImageSource extends MediaSource implements ImageSourceDto {
  @Attribute()
  uri: string;
}
