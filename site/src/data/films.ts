// MicroEngine's films as the site presents them. The list, the stories and where the films are published come from
// media/films.json, the same file the films are built and published from (media/README.md); the posters are frames of
// the films themselves, kept in src/assets/films. The videos are not part of the site: they play from MicroEngine's
// YouTube channel, and nothing of YouTube is requested before a visitor presses play.
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
  /** The film's page on YouTube: where a card leads without JavaScript. */
  watch: string;
  /** The player for the film, opened only when a card is pressed (the privacy-enhanced embed, starting at once). */
  embed: string;
}

const posters = import.meta.glob<{ default: ImageMetadata }>('../assets/films/*.png', { eager: true });

function poster(slug: string): ImageMetadata {
  const found = Object.entries(posters).find(([path]) => path.endsWith(`/${slug}.png`));
  if (!found) throw new Error(`films: no poster for '${slug}' in src/assets/films`);
  return found[1].default;
}

const length = (seconds: number) => `${Math.floor(seconds / 60)}:${String(Math.round(seconds % 60)).padStart(2, '0')}`;

export const films: Film[] = catalog.films.map(film => ({
  slug: film.slug,
  title: film.title,
  tells: film.tells,
  story: film.story,
  seconds: film.seconds,
  length: length(film.seconds),
  poster: poster(film.slug),
  watch: `https://www.youtube.com/watch?v=${film.youtube}`,
  embed: `https://www.youtube-nocookie.com/embed/${film.youtube}?autoplay=1&rel=0&playsinline=1`,
}));
