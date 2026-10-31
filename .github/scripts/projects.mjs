#!/usr/bin/env node
// The .NET project graph (docs/ci.md, "What runs for a change"): which test projects and which games a change can
// affect. changes.mjs decides the kinds of check from path rules; this narrows the .NET tests and the native captures
// to what really depends on the changed files, read from the ProjectReference items of every tracked .csproj.
//
// A changed file belongs to the project whose folder contains it most closely. The affected projects are that
// project and every project that references it, directly or through others. A file outside every project (build
// settings, shared props, capture scripts) affects everything: an unknown dependency never narrows a check.
//
//   node .github/scripts/projects.mjs [file ...]   prints the affected test projects and games for those files
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const posix = path.posix;

/** Every tracked .csproj with the projects it references (repository-relative paths, '/' separators). */
export function readGraph(root = process.cwd()) {
  const files = execFileSync('git', ['ls-files', '*.csproj'], { cwd: root, encoding: 'utf8' }).split('\n').filter(Boolean);
  return buildGraph(Object.fromEntries(files.map(file => [file, fs.readFileSync(path.join(root, file), 'utf8')])));
}

/** The graph of the given project files: { projects: { path: { dir, references } } }. */
export function buildGraph(sources) {
  const projects = {};
  for (const [file, text] of Object.entries(sources)) {
    const dir = posix.dirname(file);
    const references = [...text.matchAll(/<ProjectReference\s+Include="([^"]+)"/g)]
      .map(([, include]) => include.replace(/\\/g, '/'))
      // MSBuild properties ($(GameProject)) are resolved at build time; such a project is treated as depending on
      // everything below, which the conservative rules already cover.
      .filter(include => !include.includes('$('))
      .map(include => posix.normalize(posix.join(dir, include)));
    projects[file] = { dir, references };
  }
  return { projects };
}

/** The project that owns `file`, or null when no project folder contains it. */
export function ownerOf(graph, file) {
  let best = null;
  for (const [project, { dir }] of Object.entries(graph.projects)) {
    if ((dir === '.' || file.startsWith(`${dir}/`)) && (!best || dir.length > graph.projects[best].dir.length)) best = project;
  }
  return best;
}

/** `start` and every project that references it, directly or not. */
export function dependents(graph, start) {
  const found = new Set([start]);
  for (let grew = true; grew;) {
    grew = false;
    for (const [project, { references }] of Object.entries(graph.projects)) {
      if (!found.has(project) && references.some(ref => found.has(ref))) { found.add(project); grew = true; }
    }
  }
  return found;
}

export const isTestProject = project => /^tests\/[^/]+\/[^/]+\.csproj$/.test(project);
export const isGameProject = project => /^games\/[^/]+\/[^/]+\.csproj$/.test(project);
const gameId = project => project.split('/')[1];

/**
 * The test projects and games `files` can affect: { tests: 'all' | [project, ...], games: 'all' | [id, ...] }.
 * 'all' when a file is outside every project, or when every test project (every game) is affected.
 */
export function affectedScope(graph, files) {
  const affected = new Set();
  for (const file of files) {
    const owner = ownerOf(graph, file);
    if (!owner) return { tests: 'all', games: 'all' };
    for (const project of dependents(graph, owner)) affected.add(project);
  }
  const all = Object.keys(graph.projects);
  const collapse = (kind, map) => {
    const every = all.filter(kind).sort();
    const hit = every.filter(project => affected.has(project));
    return hit.length === every.length && every.length > 0 ? 'all' : hit.map(map);
  };
  return { tests: collapse(isTestProject, p => p), games: collapse(isGameProject, gameId) };
}

/** How a scope is written to GitHub outputs and evidence keys: 'all' or a comma-separated list ('' for none). */
export const scopeText = value => (value === 'all' ? 'all' : value.join(','));

if (process.argv[1] && fileURLToPath(import.meta.url) === fs.realpathSync(process.argv[1])) {
  const scope = affectedScope(readGraph(), process.argv.slice(2));
  console.log(`tests=${scopeText(scope.tests)}\ngames=${scopeText(scope.games)}`);
}
