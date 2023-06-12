import { VideoSourceDto } from './entities';

export interface DbAccessService {
  findAllVideoSources(): Promise<VideoSourceDto[]>;

  createVideoSource(): Promise<VideoSourceDto>;

  updateVideoSource(
    id: string,
    dto: Partial<VideoSourceDto>,
  ): Promise<VideoSourceDto | undefined>;
}
