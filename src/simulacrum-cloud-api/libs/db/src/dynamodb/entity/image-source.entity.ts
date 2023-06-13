import { ImageSourceDto } from '@simulacrum/db/common';
import {
  AUTO_GENERATE_ATTRIBUTE_STRATEGY,
  Attribute,
  AutoGenerateAttribute,
  Entity,
} from '@typedorm/common';

@Entity({
  name: 'imageSource',
  primaryKey: {
    partitionKey: 'IMAGESRC#{{id}}',
    sortKey: 'IMAGESRC#{{id}}',
  },
})
export class ImageSource implements ImageSourceDto {
  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.UUID4,
  })
  id: string;

  @Attribute()
  uri: string;

  @AutoGenerateAttribute({
    strategy: AUTO_GENERATE_ATTRIBUTE_STRATEGY.EPOCH_DATE,
    autoUpdate: true,
  })
  updatedAt: number;
}
