import { VideoSourceDto } from './entities';

export interface DbAccessService {
  createVideoSource(): Promise<VideoSourceDto>;

  updateVideoSource(
    id: string,
    dto: Partial<VideoSourceDto>,
  ): Promise<VideoSourceDto | undefined>;
}
