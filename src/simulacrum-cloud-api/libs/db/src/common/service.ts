import { ImageSourceDto, VideoSourceDto } from './entities';

export interface DbAccessService {
  findVideoSource(id: string): Promise<VideoSourceDto | undefined>;

  findAllVideoSources(): Promise<VideoSourceDto[]>;

  createVideoSource(): Promise<VideoSourceDto>;

  updateVideoSource(
    id: string,
    dto: Partial<VideoSourceDto>,
  ): Promise<VideoSourceDto | undefined>;

  findImageSource(id: string): Promise<ImageSourceDto | undefined>;

  findAllImageSources(): Promise<ImageSourceDto[]>;

  createImageSource(): Promise<ImageSourceDto>;

  updateImageSource(
    id: string,
    dto: Partial<ImageSourceDto>,
  ): Promise<ImageSourceDto | undefined>;
}
