import { PlaybackTrackerDto } from './entities';

export interface DbAccessService {
  findPlaybackTrackerById(id: string): Promise<PlaybackTrackerDto | undefined>;

  createPlaybackTracker(): Promise<PlaybackTrackerDto>;

  updatePlaybackTracker(
    id: string,
    dto: Partial<PlaybackTrackerDto>,
  ): Promise<PlaybackTrackerDto | undefined>;
}
