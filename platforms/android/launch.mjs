// What the log of one scripted run on a device says about its launch (device-checks.mjs).

// The run started, was told to quit before its first update and ended without an error. A scripted run only ends
// that way when the system closes the activity as it starts, which a freshly booted emulator sometimes does; a game
// that fails logs an error and a script that ends has run at least one update.
export function quitBeforeFirstUpdate(log) {
  return /microengine: start/.test(log)
    && /microengine: done 0 updates, 0 frames/.test(log)
    && /microengine: exit 0/.test(log);
}
