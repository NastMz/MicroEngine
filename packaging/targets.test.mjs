// Tests of the packaging rules that need no package or container: node --test packaging/*.test.mjs
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { glibcRefusal, requiredFiles, androidNoticeFiles, target } from './targets.mjs';

// What a Linux package printed in a debian:11 container (glibc 2.31) on a hosted runner: the launcher tries sdl, then
// raylib, then sdl-software, and the last failure is the error it exits with.
const fallbackLog = `exit 1
Unable to find image 'debian:11' locally
platform: sdl could not start (platform 'sdl' could not load its native library. Unable to load shared library 'SDL3' or one of its dependencies.
/tmp/p/MeadowRun/SDL3.so: cannot open shared object file: No such file or directory
/lib/x86_64-linux-gnu/libc.so.6: version \`GLIBC_2.33' not found (required by /tmp/p/MeadowRun/libSDL3.so)
libSDL3.so: cannot open shared object file: No such file or directory
); trying raylib
platform: raylib could not start (platform 'raylib' could not load its native library. Unable to load shared library 'raylib' or one of its dependencies.
libX11.so.6: cannot open shared object file: No such file or directory
); trying sdl-software
error: platform 'sdl-software' could not load its native library. Unable to load shared library 'SDL3' or one of its dependencies.
/tmp/p/MeadowRun/SDL3.so: cannot open shared object file: No such file or directory
/lib/x86_64-linux-gnu/libc.so.6: version \`GLIBC_2.33' not found (required by /tmp/p/MeadowRun/libSDL3.so)
libSDL3.so: cannot open shared object file: No such file or directory
`;

test('a launcher that falls back to sdl-software and stops on the missing glibc is a refusal', () => {
  const result = glibcRefusal(fallbackLog);
  assert.equal(result.refused, true);
  assert.equal(result.detail, "GLIBC_2.33' not found");
});

test('the earlier launcher, which stopped at sdl, is still a refusal', () => {
  const log = "exit 1\nerror: platform 'sdl' could not load its native library. /lib/x86_64-linux-gnu/libc.so.6: version `GLIBC_2.34' not found (required by libSDL3.so)\n";
  assert.deepEqual(glibcRefusal(log), { refused: true, detail: "GLIBC_2.34' not found" });
});

test('a game that started is not a refusal', () => {
  const log = "platform: sdl\naudio: available\nexit 0\n";
  assert.equal(glibcRefusal(log).refused, false);
});

test('a missing glibc reported by raylib alone is not SDL refusing', () => {
  const log = "exit 1\nerror: platform 'raylib' could not load its native library. version `GLIBC_2.38' not found\n";
  assert.equal(glibcRefusal(log).refused, false);
});

test('an SDL failure for another reason (no package, a crash) is not the promised refusal', () => {
  assert.equal(glibcRefusal("exit 1\nerror: platform 'sdl-software' could not load its native library. libSDL3.so: cannot open shared object file\n").refused, false);
  assert.equal(glibcRefusal("tar: /packages/MeadowRun-linux-x64.tar.gz: Cannot open: No such file or directory\nexit 127\n").refused, false);
});

test('the refusal must end the process with exit code 1', () => {
  assert.equal(glibcRefusal(fallbackLog.replace('exit 1', 'exit 134')).refused, false);
});

test('what is not a refusal says what was seen instead', () => {
  const result = glibcRefusal('line one\nline two\nline three\nline four\n');
  assert.equal(result.detail, 'line two | line three | line four');
});

// Every package that holds SDL's binaries carries SDL's licence; desktop packages also hold raylib's binaries.
for (const id of ['windows-x64', 'linux-x64', 'macos-arm64']) {
  test(`a ${id} package must carry the SDL and raylib licences`, () => {
    const files = requiredFiles({ game: 'NeonRift', title: 'Neon Rift', applicationId: 'dev.microengine.neonrift' }, target(id));
    assert.ok(files.includes('licenses/SDL-LICENSE.txt'), files.join(', '));
    assert.ok(files.includes('licenses/raylib-LICENSE.txt'), files.join(', '));
  });
}

test('an APK must carry the SDL licence with the other notices', () => {
  assert.ok(androidNoticeFiles.includes('assets/licenses/SDL-LICENSE.txt'));
  assert.ok(androidNoticeFiles.includes('assets/licenses/microengine-LICENSE.txt'));
});
