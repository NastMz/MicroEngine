// MicroEngine's films as the site presents them. The list, the stories and where the files are published come from
// media/films.json, the same file the films are built and published from (media/README.md); the posters are frames of
// the films themselves, kept in src/assets/films. The video files are not part of the site: they are assets of one
// GitHub release of their own, requested only when a visitor presses play.
import type { ImageMetadata } from 'astro';
import catalog from '../../../media/films.json' with { type: 'json' };

export interface Film {
  slug: string;
  title: string;
  /** What the film is about, in two or three words: each one tells a different thing. */
  tells: string;
  story: string;
  seconds: number;
  /** "0:50" */
  length: string;
  poster: ImageMetadata;
  /** The published MP4 (H.264 and AAC, 1920 by 1080). */
  src: string;
}

const posters = import.meta.glob<{ default: ImageMetadata }>('../assets/films/*.png', { eager: true });

function poster(slug: string): ImageMetadata {
  const found = Object.entries(posters).find(([path]) => path.endsWith(`/${slug}.png`));
  if (!found) throw new Error(`films: no poster for '${slug}' in src/assets/films`);
  return found[1].default;
}

const length = (seconds: number) => `${Math.floor(seconds / 60)}:${String(Math.round(seconds % 60)).padStart(2, '0')}`;

export const filmsRelease = `https://github.com/${catalog.repository}/releases/tag/${catalog.release}`;

export const films: Film[] = catalog.films.map(film => ({
  slug: film.slug,
  title: film.title,
  tells: film.tells,
  story: film.story,
  seconds: film.seconds,
  length: length(film.seconds),
  poster: poster(film.slug),
  src: `https://github.com/${catalog.repository}/releases/download/${catalog.release}/${film.slug}.mp4`,
}));
