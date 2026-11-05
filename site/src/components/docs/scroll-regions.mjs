// Tables and Mermaid diagrams in the documentation scroll sideways when they are wider than the column (Starlight
// draws every table as a scrolling block; a wide diagram scrolls inside its <pre>). A keyboard user can scroll such a
// region only when it can take focus (WCAG 2.1.1), so each one gets tabindex="0" in the rendered HTML: it works
// without JavaScript and the focus ring shows where the arrow keys will scroll. Code blocks are left alone: Expressive
// Code makes them focusable itself when they overflow.

/** Adds tabindex="0" to every <table> and Mermaid <pre> of an HTML fragment that has no tabindex yet. */
export function focusableScrollRegions(html) {
  return html.replace(/<(table|pre class="mermaid")(?=[\s>])(?![^>]*\btabindex=)/g, '<$1 tabindex="0"');
}
