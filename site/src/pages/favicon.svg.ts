// The favicon is the mark, built from the brand geometry (src/components/brand/geometry.mjs). It carries fixed
// colours for the browser's light and dark schemes, since a favicon cannot read the site's tokens.
import { faviconSvg } from '../components/brand/geometry.mjs';

export const GET = () => new Response(faviconSvg(), { headers: { 'Content-Type': 'image/svg+xml' } });
