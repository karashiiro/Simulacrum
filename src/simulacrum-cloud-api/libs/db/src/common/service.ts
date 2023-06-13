import { VideoSourceDto } from './entities';

export interface DbAccessService {
  findVideoSource(id: string): Promise<VideoSourceDto | undefined>;

  findAllVideoSources(): Promise<VideoSourceDto[]>;

  createVideoSource(): Promise<VideoSourceDto>;

  updateVideoSource(
    id: string,
    dto: Partial<VideoSourceDto>,
  ): Promise<VideoSourceDto | undefined>;
}
