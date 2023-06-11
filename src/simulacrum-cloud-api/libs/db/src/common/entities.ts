export type PlaybackTrackerState = 'playing' | 'paused';

export interface PlaybackTrackerDto {
  id: string;
  playheadSeconds: number;
  state: PlaybackTrackerState;
  updatedAt: number;
}
