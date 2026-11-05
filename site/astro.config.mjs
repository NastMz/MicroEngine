// MicroEngine public site: a static Astro build (no server, no SSR). The landing and demo pages are Astro pages;
// the documentation is Starlight, fed directly from the repository's Markdown (src/lib/repo-docs.mjs).
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import mermaid from 'astro-mermaid';
import { sidebar } from './src/lib/repo-docs.mjs';
import { fixed } from './src/components/brand/geometry.mjs';

// The canonical URL feeds the sitemap, canonical links and Open Graph tags. Vercel exposes the production domain
// at build time; SITE_URL overrides it (for a custom domain), and local builds fall back to the preview server.
const site = process.env.SITE_URL
  ?? (process.env.VERCEL_PROJECT_PRODUCTION_URL ? `https://${process.env.VERCEL_PROJECT_PRODUCTION_URL}` : 'http://localhost:4321');

export default defineConfig({
  site,
  trailingSlash: 'always',
  build: { format: 'directory' },
  // The site reads Markdown and screenshots from the rest of the repository (docs/, web/, games/).
  vite: { server: { fs: { allow: ['..'] } } },
  integrations: [
    // ```mermaid blocks in the repository's Markdown become diagrams, rendered in the browser. mermaid.js loads only
    // on pages that have one; colours follow the documentation theme (starlight.css), so both schemes match.
    // Registered before Starlight so the blocks are diagrams, not code samples.
    mermaid({
      theme: 'dark',
      enableLog: false,
      mermaidConfig: {
        // Mermaid 12 defaults to the ELK layout, a 1.4 MB download; the classic dagre layout suits these diagrams.
        layout: 'dagre',
        fontFamily: "'IBM Plex Mono', ui-monospace, 'Cascadia Mono', Consolas, monospace",
        themeVariables: { fontSize: '13px' },
        // Natural size: wide diagrams scroll sideways on small screens instead of shrinking to unreadable text.
        flowchart: { useMaxWidth: false, wrappingWidth: 420, padding: 12, nodeSpacing: 28, rankSpacing: 36 },
      },
    }),
    starlight({
      title: 'MicroEngine',
      description: 'A compact, code-first 2D game engine for C#/.NET, with four complete pixel-art games that also run in desktop browsers.',
      // The header renders DocsSiteTitle (the lockup); these files stand in wherever Starlight draws its own logo.
      logo: { light: './src/assets/brand/mark-light.svg', dark: './src/assets/brand/mark-dark.svg', alt: '' },
      customCss: ['./src/styles/fonts.css', './src/styles/tokens.css', './src/styles/brand.css', './src/styles/starlight.css'],
      sidebar: sidebar(),
      disable404Route: true,
      pagination: true,
      // Long guides (tutorial, manual, how-to) should not lose to the many short API type pages that share their words.
      pagefind: { ranking: { pageLength: 0.1 } },
      head: [
        { tag: 'meta', attrs: { name: 'theme-color', content: fixed.light.paper, media: '(prefers-color-scheme: light)' } },
        { tag: 'meta', attrs: { name: 'theme-color', content: fixed.dark.paper, media: '(prefers-color-scheme: dark)' } },
        { tag: 'meta', attrs: { property: 'og:image', content: new URL('/og.png', site).href } },
        { tag: 'meta', attrs: { name: 'twitter:card', content: 'summary_large_image' } },
        { tag: 'link', attrs: { rel: 'icon', href: '/favicon.svg', type: 'image/svg+xml' } },
      ],
      // Code blocks: Expressive Code, configured in ec.config.mjs (the brand's label strip on graphite).
      components: {
        SiteTitle: './src/components/DocsSiteTitle.astro',
        // The family legend and socket card above a page, and the status tags in its body (src/components/docs/).
        PageTitle: './src/components/docs/PageTitle.astro',
        MarkdownContent: './src/components/docs/MarkdownContent.astro',
      },
    }),
  ],
});
