#!/usr/bin/env node
// What a public repository would expose (docs/evaluations/public-1.md, "Audit"): every commit on every ref, not only
// the current tree, because making the repository public publishes its whole history. Read-only: it never rewrites
// history or removes a file.
//
//   node .github/scripts/public-audit.mjs [--legacy <ref>] [--rebuild <commit>] [--json]
//
// Reports, with the commit and path but never the matched value:
//   secrets    added lines that look like credentials (tokens, private keys, assigned passwords)
//   files      paths that usually hold credentials or machine settings (.env, keys, keystores, .user files)
//   licence    commits whose LICENSE grants the MIT licence, split into those reachable from --legacy (the
//              previous implementation), those descending from --rebuild (the current implementation) and others
//   authors    distinct author and committer addresses
//   media      binary assets that exist only in history, and whether their folder ever had a notice
// The exit code is 1 when a secret or a credential file is found (a publication blocker), else 0.
import fs from 'node:fs';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

export const secretPatterns = [
  ['GitHub token', /\b(ghp|gho|ghu|ghs|ghr)_[A-Za-z0-9]{30,}\b|\bgithub_pat_[A-Za-z0-9_]{40,}/],
  ['AWS access key', /\bAKIA[0-9A-Z]{16}\b/],
  ['private key', /-----BEGIN (RSA |EC |OPENSSH |DSA |PGP )?PRIVATE KEY/],
  ['Slack token', /\bxox[abprs]-[0-9A-Za-z-]{10,}/],
  ['Google API key', /\bAIza[0-9A-Za-z_-]{35}\b/],
  ['NuGet API key', /\boy2[a-z0-9]{43}\b/],
  ['Vercel token', /\bvercel_[A-Za-z0-9]{24,}\b/i],
  ['assigned secret', /\b(api[_-]?key|secret|password|passwd|token)\b["']?\s*[:=]\s*["'][^"'\s$]{12,}["']/i],
];
// References to secret stores are not secrets.
const reference = /\$\{\{|secrets\.|process\.env|\benv\.|\$env:|example|placeholder|dummy|test-token|wrong-token/i;

// This audit's own tests hold fake secrets on purpose, to prove they are found.
export const fixtureFiles = new Set(['.github/scripts/public-audit.test.mjs']);
export const credentialFile = /(^|\/)(\.env(\..+)?|id_(rsa|dsa|ecdsa|ed25519)|.+\.(pfx|p12|pem|key|keystore|jks|mobileprovision|snk)|.+\.(csproj|sln)\.user|\.vercel\/.+|appsettings\.(Development|Local)\.json)$/i;
const media = /\.(png|jpe?g|gif|bmp|mp3|ogg|wav|flac|ttf|otf|aseprite|psd)$/i;
const notice = /(^|\/)(NOTICE|LICEN[CS]E|CREDITS|provenance)[^/]*$/i;
export const mitGrant = 'Permission is hereby granted, free of charge';

/** The secret kinds an added line looks like (empty when it is clean or only refers to a secret store). */
export function secretKinds(line) {
  if (reference.test(line)) return [];
  return secretPatterns.filter(([, pattern]) => pattern.test(line)).map(([name]) => name);
}

const git = (args, root) => execFileSync('git', args, { cwd: root, encoding: 'utf8', maxBuffer: 1024 * 1024 * 1024 });
const lines = text => text.split('\n').filter(Boolean);

export function audit({ root = process.cwd(), legacy, rebuild } = {}) {
  const commits = lines(git(['rev-list', '--all'], root));
  const result = { commits: commits.length, secrets: [], files: [], licence: {}, authors: [], media: {} };

  // Added lines of every commit; only the commit, path and kind are kept.
  let commit = '', file = '';
  for (const line of git(['log', '--all', '-p', '--no-color', '-U0', '--format=commit %H'], root).split('\n')) {
    if (line.startsWith('commit ')) commit = line.slice(7, 19);
    else if (line.startsWith('+++ b/')) file = line.slice(6);
    else if (line.startsWith('+') && !line.startsWith('+++') && !fixtureFiles.has(file)) {
      for (const kind of secretKinds(line)) result.secrets.push({ commit, file, kind });
    }
  }

  const everPaths = new Set(lines(git(['log', '--all', '--name-only', '--format='], root)));
  result.files = [...everPaths].filter(p => credentialFile.test(p)).sort();

  const mit = commits.filter(c => { try { return git(['show', `${c}:LICENSE`], root).includes(mitGrant); } catch { return false; } });
  const legacySet = legacy ? new Set(lines(git(['rev-list', legacy], root))) : new Set();
  const pushed = new Set(lines(git(['rev-list', '--remotes'], root)));
  const newer = mit.filter(c => !legacySet.has(c));
  const descends = c => { try { execFileSync('git', ['merge-base', '--is-ancestor', rebuild, c], { cwd: root, stdio: 'ignore' }); return true; } catch { return false; } };
  const current = rebuild ? newer.filter(descends) : [];
  result.licence = {
    mit: mit.length, legacy: mit.length - newer.length, other: newer.length, current: current.length,
    currentPushed: current.filter(c => pushed.has(c)).length,
    otherPushed: newer.filter(c => pushed.has(c)).length,
    otherRange: newer.length ? lines(git(['log', '--no-walk', '--format=%ad %h', '--date=short', ...newer], root)).sort().filter((_, i, a) => i === 0 || i === a.length - 1) : [],
    head: (() => { try { return git(['show', 'HEAD:LICENSE'], root).includes(mitGrant) ? 'MIT' : 'not MIT'; } catch { return 'none'; } })(),
  };

  const counts = new Map();
  for (const address of lines(git(['log', '--all', '--format=%ae%n%ce'], root))) counts.set(address, (counts.get(address) ?? 0) + 1);
  result.authors = [...counts].map(([address, n]) => ({ address, commits: n, noreply: /noreply/i.test(address) })).sort((a, b) => b.commits - a.commits);

  const head = new Set(lines(git(['ls-tree', '-r', '--name-only', 'HEAD'], root)));
  const noticed = new Set([...everPaths].filter(p => notice.test(p)).map(p => p.split('/').slice(0, -1).join('/')));
  const historic = [...everPaths].filter(p => media.test(p) && !head.has(p));
  const dirs = new Map();
  for (const p of historic) {
    const dir = p.split('/').slice(0, -1).join('/');
    const covered = [...noticed].some(d => dir === d || dir.startsWith(`${d}/`) || (d && d.startsWith(`${dir}/`)));
    const entry = dirs.get(dir) ?? { dir, files: 0, notice: covered };
    entry.files++;
    dirs.set(dir, entry);
  }
  result.media = { historic: historic.length, folders: [...dirs.values()].sort((a, b) => a.dir.localeCompare(b.dir)) };
  return result;
}

export function report(r) {
  const out = ['### Public repository audit', '', `${r.commits} commits on all refs.`, ''];
  out.push(`**Secrets:** ${r.secrets.length ? `${r.secrets.length} suspect line(s) (values not shown):` : 'none found.'}`);
  for (const s of r.secrets) out.push(`- ${s.commit} \`${s.file}\`: ${s.kind}`);
  out.push('', `**Credential or machine files ever committed:** ${r.files.length ? r.files.map(f => `\`${f}\``).join(', ') : 'none.'}`);
  const l = r.licence;
  out.push('', `**MIT licence:** ${l.mit} commits carry the MIT grant; ${l.legacy} are reachable from the legacy ref, ${l.other} are not` +
    (l.other ? ` (${l.otherPushed} pushed; ${l.otherRange.join(' to ')})` : '') +
    (l.current ? `, of which ${l.current} descend from the rebuild (${l.currentPushed} pushed)` : '') + `. LICENSE at HEAD: ${l.head}.`);
  out.push('', '**Addresses in commit metadata:**', '', '| Address kind | Commits |', '| --- | --- |');
  for (const a of r.authors) out.push(`| ${a.noreply ? `no-reply (${a.address})` : 'personal (not shown)'} | ${a.commits} |`);
  out.push('', `**Media only in history:** ${r.media.historic} file(s).`, '', '| Folder | Files | Notice in history |', '| --- | --- | --- |');
  for (const m of r.media.folders) out.push(`| \`${m.dir || '.'}\` | ${m.files} | ${m.notice ? 'yes' : '**no: review**'} |`);
  return out.join('\n') + '\n';
}

if (process.argv[1] && fileURLToPath(import.meta.url) === fs.realpathSync(process.argv[1])) {
  const args = process.argv.slice(2);
  const legacy = args.includes('--legacy') ? args[args.indexOf('--legacy') + 1] : undefined;
  const rebuild = args.includes('--rebuild') ? args[args.indexOf('--rebuild') + 1] : undefined;
  const result = audit({ legacy, rebuild });
  console.log(args.includes('--json') ? JSON.stringify(result, null, 2) : report(result));
  process.exitCode = result.secrets.length || result.files.length ? 1 : 0;
}
