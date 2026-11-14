// node --test packaging/android-launch.test.mjs
import test from 'node:test';
import assert from 'node:assert/strict';
import { quitBeforeFirstUpdate } from '../platforms/android/launch.mjs';

// The log of a first launch on a freshly booted emulator (API 34) that the system closed as it started.
const closedAtOnce = `--------- beginning of main
I/MicroEngine( 3534): microengine: start NeonRift script=menus.script on android-x64
I/MicroEngine( 3534): microengine: done 0 updates, 0 frames, 0 captures, audio available
I/MicroEngine( 3534): microengine: exit 0
E/SDL     ( 3534): SDLActivity thread ends (error=Try to release egl_surface with context probably still active)
`;

test('a run that starts and ends with no update and no error was closed as it started', () => {
  assert.equal(quitBeforeFirstUpdate(closedAtOnce), true);
});

test('a run that played is not taken for one closed as it started', () => {
  const played = `I/MicroEngine( 4013): microengine: start NeonRift script=stage1.script on android-x64
I/MicroEngine( 4013): microengine: capture s1-start.png
I/MicroEngine( 4013): microengine: done 430 updates, 430 frames, 7 captures, audio available
I/MicroEngine( 4013): microengine: exit 0
`;
  assert.equal(quitBeforeFirstUpdate(played), false);
  assert.equal(quitBeforeFirstUpdate(played.replace('done 430 updates', 'done 10 updates')), false);
});

test('a run that failed, never started or has not ended is judged by its own result', () => {
  const failed = closedAtOnce.replace('microengine: exit 0', 'microengine: exit 1');
  assert.equal(quitBeforeFirstUpdate(failed), false);
  assert.equal(quitBeforeFirstUpdate(''), false);
  assert.equal(quitBeforeFirstUpdate('I/MicroEngine( 1): microengine: start NeonRift script=menus.script on android-x64\n'), false);
});
