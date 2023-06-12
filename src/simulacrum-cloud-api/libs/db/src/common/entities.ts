export type PlaybackTrackerState = 'playing' | 'paused';

export interface PlaybackTrackerDto {
  id: string;
  playheadSeconds: number;
  state: PlaybackTrackerState;
  updatedAt: number;
}

export type MediaSourceType = 'video';

export interface MediaSourceDto {
  id: string;
  playbackTrackerId?: string;
  type: MediaSourceType;
  uri?: string;
  updatedAt: number;
}
