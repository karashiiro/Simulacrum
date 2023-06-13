export interface ImageMetadata {
  uri: string;
}

export type PlaybackState = 'playing' | 'paused';

export interface VideoMetadata {
  uri: string;
  playheadSeconds: number;
  state: PlaybackState;
}

export type MediaMetadata =
  | ({ type: 'image' } & Partial<ImageMetadata>)
  | ({ type: 'video' } & Partial<VideoMetadata>);

export interface MediaSourceDto {
  id: string;
  meta: MediaMetadata;
  updatedAt: number;
}
