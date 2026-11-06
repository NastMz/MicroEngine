// The documentation writes a capability's status in words ("In the repository only, not in a package
// yet"). The site draws those words with the brand's status styles (site/design/README.md, "Present and
// future") without touching the Markdown: markStatusPhrases wraps them in the rendered HTML. Only text outside code,
// headings, links' own markup and SVG is touched, and only where the phrase works as a label.

const skip = new Set(['pre', 'code', 'kbd', 'script', 'style', 'svg', 'textarea', 'title', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6']);

// Each rule: a pattern and the status it marks. The site describes the SDK that contains it as published, so the only
// status the documentation writes in words is work that stays in the repository.
const rules = [
  { status: 'repo', pattern: /\bin the repository(?: only)?, not (?:published|in a package)(?: yet)?/gi },
];
const combined = new RegExp(rules.map(r => `(${r.pattern.source})`).join('|'), 'gi');

function wrap(text) {
  return text.replace(combined, (match, ...groups) => {
    const index = groups.slice(0, rules.length).findIndex(g => g !== undefined);
    const rule = rules[index];
    return `<span class="status-tag" data-status="${rule.status}"><span class="status-mark" aria-hidden="true"></span>${match}</span>`;
  });
}

/** Wraps the status phrases of an HTML fragment in status tags. */
export function markStatusPhrases(html) {
  let depth = 0;
  return html.replace(/(<!--[\s\S]*?-->|<\/?([a-zA-Z][\w-]*)\b[^>]*>)|([^<]+)/g, (token, tag, name, text) => {
    if (text !== undefined) return depth === 0 ? wrap(text) : text;
    if (!name) return token;
    const lower = name.toLowerCase();
    if (skip.has(lower) && !token.endsWith('/>')) depth += token.startsWith('</') ? -1 : 1;
    if (depth < 0) depth = 0;
    return token;
  });
}
