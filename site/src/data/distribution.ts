// How visitors can get MicroEngine and what state it is in. The SDK (packages, the microengine tool, the template) is
// prepared and checked but not on nuget.org yet (docs/guide/sdk.md): `install` builds it into a local feed. When the
// owner publishes a release, replace `install` with `dotnet tool install -g MicroEngine.Cli --prerelease` and
// `microengine new`, and drop the "not on nuget.org" wording. The version is read from the build.
import { sdkVersion } from '../lib/versions.mjs';

export interface InstallStep { label: string; command: string }

export const sdk = {
  version: sdkVersion,
  stage: 'Alpha',
  summary: 'MicroEngine works and is checked on six platforms, but its contracts, subsystems and tools are still evolving. Public APIs may change incompatibly between Alpha versions; there is no 1.0 and no promise of backward compatibility yet.',
};

export const distribution = {
  status: 'Open source under the MIT License: free for personal and commercial games, no royalties. The SDK (NuGet packages, the microengine tool and a dotnet new template) is ready but not on nuget.org yet: build it from the public repository with the .NET 10 SDK.',
  install: [
    { label: 'Clone', command: 'git clone https://github.com/NastMz/MicroEngine.git' },
    { label: 'Build the SDK packages', command: 'dotnet pack MicroEngine/MicroEngine.slnx -c Release -o ~/microengine-feed' },
    { label: 'Use them as a package source', command: 'dotnet nuget add source ~/microengine-feed --name microengine-local' },
    { label: 'Install the tool', command: 'dotnet tool install -g MicroEngine.Cli --prerelease' },
    { label: 'Create and run a game', command: 'microengine new MyGame && cd MyGame && microengine run' },
  ] satisfies InstallStep[],
  startHref: '/docs/getting-started/',
  repository: 'https://github.com/NastMz/MicroEngine',
};
