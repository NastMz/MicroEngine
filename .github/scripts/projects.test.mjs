// Tests for the project graph: node --test .github/scripts/*.test.mjs
import test from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { buildGraph, readGraph, ownerOf, affectedScope } from './projects.mjs';

const ref = to => `<Project><ItemGroup><ProjectReference Include="${to}" /></ItemGroup></Project>`;
const graph = buildGraph({
  'src/Core/Core.csproj': '<Project />',
  'src/Backend/Backend.csproj': ref('../Core/Core.csproj'),
  'games/Alpha/Alpha.csproj': ref('..\\..\\src\\Backend\\Backend.csproj'),
  'games/Beta/Beta.csproj': ref('../../src/Backend/Backend.csproj'),
  'tests/Core.Tests/Core.Tests.csproj': ref('../../src/Core/Core.csproj'),
  'tests/Alpha.Tests/Alpha.Tests.csproj': ref('../../games/Alpha/Alpha.csproj'),
  'tests/Beta.Tests/Beta.Tests.csproj': ref('../../games/Beta/Beta.csproj'),
  'web/Host/Host.csproj': ref('$(GameProject)'),
});

test('a file belongs to the closest project folder', () => {
  assert.equal(ownerOf(graph, 'games/Alpha/Scenes/Title.cs'), 'games/Alpha/Alpha.csproj');
  assert.equal(ownerOf(graph, 'tests/Alpha.Tests/A.cs'), 'tests/Alpha.Tests/Alpha.Tests.csproj');
  assert.equal(ownerOf(graph, 'Directory.Build.props'), null);
  assert.equal(ownerOf(graph, 'games/AlphaBeta/x.cs'), null, 'a folder name prefix is not containment');
});

test('one game: its tests and its captures only', () => {
  assert.deepEqual(affectedScope(graph, ['games/Alpha/Player.cs']), { tests: ['tests/Alpha.Tests/Alpha.Tests.csproj'], games: ['Alpha'] });
});

test('a test project: itself, no game', () => {
  assert.deepEqual(affectedScope(graph, ['tests/Beta.Tests/B.cs']), { tests: ['tests/Beta.Tests/Beta.Tests.csproj'], games: [] });
});

test('the core reaches every project through the references', () => {
  assert.deepEqual(affectedScope(graph, ['src/Core/Scene.cs']), { tests: 'all', games: 'all' });
});

test('the backend reaches the games and their tests, not the core tests', () => {
  assert.deepEqual(affectedScope(graph, ['src/Backend/Window.cs']), {
    tests: ['tests/Alpha.Tests/Alpha.Tests.csproj', 'tests/Beta.Tests/Beta.Tests.csproj'], games: 'all',
  });
});

test('a file outside every project affects everything', () => {
  assert.deepEqual(affectedScope(graph, ['games/Alpha/Player.cs', 'Directory.Build.props']), { tests: 'all', games: 'all' });
});

test('no files, nothing affected', () => {
  assert.deepEqual(affectedScope(graph, []), { tests: [], games: [] });
});

test("this repository's graph: a game change stays inside that game, an engine change reaches all", () => {
  const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
  const real = readGraph(root);
  assert.deepEqual(affectedScope(real, ['games/MeadowRun/MeadowRunGame.cs']), { tests: ['tests/MeadowRun.Tests/MeadowRun.Tests.csproj'], games: ['MeadowRun'] });
  assert.deepEqual(affectedScope(real, ['src/MicroEngine/Core/GameRunner.cs']), { tests: 'all', games: 'all' });
  const backend = affectedScope(real, ['src/MicroEngine.Raylib/RaylibPlatform.cs']);
  assert.equal(backend.games, 'all');
  assert.ok(!backend.tests.includes('tests/MicroEngine.Tests/MicroEngine.Tests.csproj'), 'core tests do not reference the backend');
});
