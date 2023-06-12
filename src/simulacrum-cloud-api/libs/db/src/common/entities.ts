export interface ImageSourceDto {
  id: string;
  uri: string;
  updatedAt: number;
}

export type PlaybackState = 'playing' | 'paused';

export interface VideoSourceDto {
  id: string;
  uri: string;
  playheadSeconds: number;
  state: PlaybackState;
  updatedAt: number;
}
