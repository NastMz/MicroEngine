// How visitors can get MicroEngine. Today nothing is distributed, so the site offers the demos, the documentation
// and the roadmap. When packages and templates are published, set `available` and fill `install`: the landing's
// "Getting MicroEngine" section and calls to action switch to the install steps without a redesign.
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
  available: false,
  status: 'MicroEngine is not distributed to external developers yet: there is no package to install today. NuGet packages and project templates are planned.',
  install: [],
  startHref: '/docs/getting-started/',
};
