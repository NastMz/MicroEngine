import { defineCollection } from 'astro:content';
import { docsSchema } from '@astrojs/starlight/schema';
import { repoDocsLoader } from './lib/repo-docs.mjs';

// The documentation pages are the repository's own Markdown files, listed in src/lib/repo-docs.mjs.
export const collections = {
  docs: defineCollection({ loader: repoDocsLoader(), schema: docsSchema() }),
};
