// How visitors can get MicroEngine. The engine is open source (MIT): today it is used from its public repository;
// when NuGet packages and templates are published, replace `install` with their commands.
export interface InstallStep { label: string; command: string }

export interface Distribution {
  available: boolean;
  /** One sentence on the current state, shown on the landing. */
  status: string;
  /** Shell commands of the public install, in order (only when `available`). */
  install: InstallStep[];
  /** Where the public "get started" call to action points. */
  startHref: string;
}

export const distribution: Distribution = {
  available: true,
  status: 'MicroEngine is open source under the MIT License: free for personal and commercial games, no royalties. There are no NuGet packages yet; build it from the public repository with the .NET 10 SDK.',
  install: [
    { label: 'Clone', command: 'git clone https://github.com/NastMz/MicroEngine.git' },
    { label: 'Build and test', command: 'dotnet test MicroEngine.slnx -c Release' },
    { label: 'Run the sample', command: 'dotnet run --project samples/HelloEngine -c Release' },
  ],
  startHref: '/docs/getting-started/',
};
