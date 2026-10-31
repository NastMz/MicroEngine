// The favicon is the site mark itself, served from its single source in src/assets.
import mark from '../assets/mark.svg?raw';

export const GET = () => new Response(mark, { headers: { 'Content-Type': 'image/svg+xml' } });
