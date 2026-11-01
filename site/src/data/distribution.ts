// How visitors can get MicroEngine and what state it is in. The engine is open source (MIT) and used from its public
// repository; NuGet packages and `dotnet new` templates are prepared for SDK-1 (docs/guide/sdk.md) but not
// published: when they are, replace `install` with their commands. The version is read from the build.
import { sdkVersion } from '../lib/versions.mjs';

export interface InstallStep { label: string; command: string }

export const sdk = {
  version: sdkVersion,
  stage: 'Alpha',
  summary: 'MicroEngine works and is checked on six platforms, but its contracts, subsystems and tools are still evolving. Public APIs may change incompatibly between Alpha versions; there is no 1.0 and no promise of backward compatibility yet.',
};

export const distribution = {
  status: 'Open source under the MIT License: free for personal and commercial games, no royalties. There are no NuGet packages yet; build it from the public repository with the .NET 10 SDK.',
  install: [
    { label: 'Clone', command: 'git clone https://github.com/NastMz/MicroEngine.git' },
    { label: 'Build and test', command: 'dotnet test MicroEngine.slnx -c Release' },
    { label: 'Run the sample', command: 'dotnet run --project samples/HelloEngine -c Release' },
  ] satisfies InstallStep[],
  startHref: '/docs/getting-started/',
  repository: 'https://github.com/NastMz/MicroEngine',
};
