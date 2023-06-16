export interface ImageMetadata {
  uri: string;
}

export type PlaybackState = 'playing' | 'paused';

export interface VideoMetadata {
  uri: string;
  playheadSeconds: number;
  playheadUpdatedAt: number;
  state: PlaybackState;
}

export type MediaMetadata =
  | { type: 'blank' }
  | ({ type: 'image' } & Partial<ImageMetadata>)
  | ({ type: 'video' } & Partial<VideoMetadata>);

export type MediaSourceType = MediaMetadata['type'];

export interface MediaSourceDto {
  id: string;
  meta: MediaMetadata;
  updatedAt: number;
}

export interface ScreenDto {
  id: string;
  mediaSourceId?: string;
  updatedAt: number;
}
