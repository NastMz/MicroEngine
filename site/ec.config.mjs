// Expressive Code (the documentation's code blocks), set up as the brand's code treatment (site/design/README.md,
// "Code and screenshots"): code sits on graphite in both themes, under a label strip that names the file and the
// language, like the legend on a module. The strip is a label, not a window: no tabs, no title bar, no dots. The
// strip's layout and the graphite surface are in src/styles/starlight.css; colours are the site's tokens.
import { defineEcConfig } from '@astrojs/starlight/expressive-code';

// The language as the strip names it. Unknown languages show their fence name in capitals.
const languageNames = {
  csharp: 'C#', cs: 'C#', bash: 'Shell', sh: 'Shell', shell: 'Shell', powershell: 'PowerShell', ps1: 'PowerShell',
  xml: 'XML', html: 'HTML', css: 'CSS', yaml: 'YAML', yml: 'YAML', json: 'JSON', js: 'JavaScript',
  javascript: 'JavaScript', ts: 'TypeScript', typescript: 'TypeScript', hlsl: 'HLSL', md: 'Markdown', text: 'Text',
  txt: 'Text', plaintext: 'Text', ini: 'INI', toml: 'TOML', diff: 'Diff',
};

/** Adds the language to the strip above every code block (after the frames plugin has built it). */
function pluginLanguageLabel() {
  return {
    name: 'MicroEngine language label',
    hooks: {
      postprocessRenderedBlock: ({ codeBlock, renderData }) => {
        const header = renderData.blockAst.children?.find(node => node.type === 'element' && node.tagName === 'figcaption');
        const language = codeBlock.language;
        if (!header || !language) return;
        const name = languageNames[language] ?? language.toUpperCase();
        header.children.push({ type: 'element', tagName: 'span', properties: { className: ['lang'] }, children: [{ type: 'text', value: name }] });
      },
    },
  };
}

export default defineEcConfig({
  // One dark theme for both site themes: code panels are graphite everywhere, as on the landing.
  themes: ['github-dark-dimmed'],
  useStarlightDarkModeSwitch: false,
  useStarlightUiThemeColors: false,
  // Syntax colours are lifted until every token reads at 5.5:1 on the code background, comments included.
  minSyntaxHighlightingColorContrast: 5.5,
  plugins: [pluginLanguageLabel()],
  defaultProps: {
    // Shell blocks get the same label strip as code, not a terminal window.
    frame: 'code',
  },
  styleOverrides: {
    borderRadius: 'var(--radius-sm)',
    borderWidth: 'var(--rule-thin)',
    borderColor: 'var(--color-rule)',
    // Resolved inside the code panel, which sets color-scheme: dark (starlight.css), so these are the dark tokens.
    codeBackground: 'var(--color-paper-2)',
    codeForeground: 'var(--color-ink)',
    codeFontFamily: 'var(--font-mono)',
    codeFontSize: 'var(--text-sm)',
    codeLineHeight: '1.6',
    codePaddingBlock: 'var(--space-sm)',
    codePaddingInline: 'var(--space-md)',
    uiFontFamily: 'var(--font-mono)',
    uiFontSize: 'var(--text-xs)',
    uiLineHeight: 'var(--leading-snug)',
    focusBorder: 'var(--color-focus)',
    scrollbarThumbColor: 'var(--color-ink-4)',
    scrollbarThumbHoverColor: 'var(--color-ink-3)',
    frames: {
      shadowColor: 'transparent',
      frameBoxShadowCssValue: 'none',
      editorTabBarBackground: 'var(--color-paper-2)',
      editorTabBarBorderColor: 'var(--color-rule)',
      editorTabBarBorderBottomColor: 'var(--color-rule)',
      editorTabBorderRadius: '0',
      editorActiveTabBackground: 'transparent',
      editorActiveTabForeground: 'var(--color-ink)',
      editorActiveTabBorderColor: 'transparent',
      editorActiveTabIndicatorTopColor: 'transparent',
      editorActiveTabIndicatorBottomColor: 'transparent',
      editorBackground: 'var(--color-paper-2)',
      terminalTitlebarDotsOpacity: '0',
      terminalTitlebarBackground: 'var(--color-paper-2)',
      terminalTitlebarForeground: 'var(--color-ink)',
      terminalTitlebarBorderBottomColor: 'var(--color-rule)',
      terminalBackground: 'var(--color-paper-2)',
      inlineButtonForeground: 'var(--color-ink)',
      inlineButtonBorder: 'var(--color-ink-3)',
      tooltipSuccessBackground: 'var(--color-ok)',
      tooltipSuccessForeground: 'var(--color-paper)',
    },
  },
});
