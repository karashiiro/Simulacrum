export type PlaybackState = 'playing' | 'paused';

export type MediaSourceType = 'image' | 'video';

export interface MediaSourceDto {
  id: string;
  type: MediaSourceType;
  updatedAt: number;
}

export interface ImageSourceDto extends MediaSourceDto {
  uri: string;
}

export interface VideoSourceDto extends MediaSourceDto {
  uri: string;
  playheadSeconds: number;
  state: PlaybackState;
}
