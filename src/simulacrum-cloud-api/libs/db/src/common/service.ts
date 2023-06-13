import { MediaSourceDto } from './entities';

export interface DbAccessService {
  findMediaSource(id: string): Promise<MediaSourceDto | undefined>;

  findAllMediaSources(): Promise<MediaSourceDto[]>;

  createMediaSource(): Promise<MediaSourceDto>;

  updateMediaSource(
    id: string,
    dto: Partial<MediaSourceDto>,
  ): Promise<MediaSourceDto | undefined>;
}
