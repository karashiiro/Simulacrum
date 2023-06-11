import { PlaybackTrackerDto } from './entities';

export interface DbAccessService {
  findPlaybackTrackerById(id: string): Promise<PlaybackTrackerDto | undefined>;

  createPlaybackTracker(playheadSeconds: number): Promise<PlaybackTrackerDto>;
}
