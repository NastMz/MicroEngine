// How visitors can get MicroEngine and what state it is in. The SDK (packages, the microengine tool, the template) is
// on nuget.org (docs/guide/sdk.md); `install` is what a developer runs, checked by sdk/tests/external.mjs --public.
// The version is read from the build.
import { sdkVersion } from '../lib/versions.mjs';

export interface InstallStep { label: string; command: string }

export const sdk = {
  version: sdkVersion,
  stage: 'Alpha',
  summary: 'MicroEngine works and is checked on six platforms, but its contracts, subsystems and tools are still evolving. Public APIs may change incompatibly between Alpha versions; there is no 1.0 and no promise of backward compatibility yet.',
};

export const distribution = {
  status: 'Open source under the MIT License: free for personal and commercial games, no royalties. The SDK is on nuget.org: the engine packages, the microengine tool and a dotnet new template. You need the .NET 10 SDK.',
  install: [
    { label: 'Install the tool', command: 'dotnet tool install -g MicroEngine.Cli --prerelease' },
    { label: 'Create a game', command: 'microengine new MyGame && cd MyGame' },
    { label: 'Run it', command: 'microengine run' },
    { label: 'Package it', command: 'microengine package windows' },
  ] satisfies InstallStep[],
  nuget: 'https://www.nuget.org/packages/MicroEngine.Cli',
  startHref: '/docs/getting-started/',
  repository: 'https://github.com/NastMz/MicroEngine',
};
