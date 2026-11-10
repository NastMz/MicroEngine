// Embeds a film's assets/score.json (written by its audio program) into compositions/film.html, between its score
// markers. "Every step" draws the score's steps, and a composition may not fetch at render time, so the data lives in
// the file. Called by films.mjs ("audio").
import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

export function embedScore(filmDirectory) {
  const film = join(filmDirectory, 'compositions', 'film.html');
  const score = JSON.stringify(JSON.parse(readFileSync(join(filmDirectory, 'assets', 'score.json'), 'utf8')));
  const html = readFileSync(film, 'utf8');
  const marked = /\/\*score:start\*\/[\s\S]*?\/\*score:end\*\//;
  if (!marked.test(html)) throw new Error('film.html has no score markers.');
  writeFileSync(film, html.replace(marked, () => `/*score:start*/${score}/*score:end*/`));
  console.log('Embedded the score: ' + JSON.parse(score).voices.length + ' voices.');
}
