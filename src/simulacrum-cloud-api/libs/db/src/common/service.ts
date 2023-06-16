import { MediaSourceDto, ScreenDto } from './entities';

export interface DbAccessService {
  findMediaSource(id: string): Promise<MediaSourceDto | undefined>;

  findAllMediaSources(): Promise<MediaSourceDto[]>;

  createMediaSource(
    dto: Omit<MediaSourceDto, 'id' | 'updatedAt'>,
  ): Promise<MediaSourceDto>;

  updateMediaSource(
    id: string,
    dto: Partial<MediaSourceDto>,
  ): Promise<MediaSourceDto | undefined>;

  findScreensByMediaSourceId(mediaSourceId: string): Promise<ScreenDto[]>;

  createScreen(dto: Omit<ScreenDto, 'id' | 'updatedAt'>): Promise<ScreenDto>;
}
