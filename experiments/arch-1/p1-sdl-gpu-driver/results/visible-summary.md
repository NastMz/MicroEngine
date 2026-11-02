## Results (<scratch>\visible)

median (min-max) over the runs of each scenario; the last row is the median over scenarios and its ratio to the first column.

| Driver | runs | exit codes | renderer | SDL | adapter (profile) |
| --- | --- | --- | --- | --- | --- |
| default | 3 | 6/6 zero | direct3d11 | 3.5.0 | NVIDIA GeForce RTX 4070 Ti SUPER |
| gpu | 3 | 6/6 zero | gpu | 3.5.0 | NVIDIA GeForce RTX 4070 Ti SUPER |

### Start-up: process start to first frame (ms)

| Scenario | default | gpu |
| --- | --- | --- |
| MeadowRun/smoke | 1873 (1871-1883) | 2118 (2091-2165) |
| NeonRift/stage1 | 1600 (1571-1686) | 1825 (1823-2018) |
| **median of scenarios** | **1736** | **1971** (x1.14) |

### SDL_CreateRenderer (ms)

| Scenario | default | gpu |
| --- | --- | --- |
| MeadowRun/smoke | 162 (150-170) | 394 (392-413) |
| NeonRift/stage1 | 158 (148-164) | 390 (383-480) |
| **median of scenarios** | **160** | **392** (x2.45) |

### Frame interval p50, unpaced (µs)

| Scenario | default | gpu |
| --- | --- | --- |
| MeadowRun/smoke | 246 (231-251) | 334 (324-363) |
| NeonRift/stage1 | 260 (258-313) | 376 (371-475) |
| **median of scenarios** | **253** | **355** (x1.40) |

### Frame interval p95, unpaced (µs)

| Scenario | default | gpu |
| --- | --- | --- |
| MeadowRun/smoke | 941 (854-1042) | 916 (886-1029) |
| NeonRift/stage1 | 873 (760-1010) | 2078 (1988-2547) |
| **median of scenarios** | **907** | **1497** (x1.65) |

### EndFrame/present p50 (µs)

| Scenario | default | gpu |
| --- | --- | --- |
| MeadowRun/smoke | 65 (59-70) | 178 (176-191) |
| NeonRift/stage1 | 68 (68-84) | 187 (182-234) |
| **median of scenarios** | **67** | **183** (x2.74) |

### Peak commit (MB)

| Scenario | default | gpu |
| --- | --- | --- |
| MeadowRun/smoke | 243 (242-245) | 294 (294-295) |
| NeonRift/stage1 | 124 (124-124) | 174 (174-175) |
| **median of scenarios** | **183** | **234** (x1.27) |

### Peak working set (MB)

| Scenario | default | gpu |
| --- | --- | --- |
| MeadowRun/smoke | 248 (247-248) | 283 (282-283) |
| NeonRift/stage1 | 124 (124-124) | 159 (159-159) |
| **median of scenarios** | **186** | **221** (x1.19) |

### Log lines other than the launcher's own (count over all runs and scenarios)

- **default**
  - 6 x `err: Created renderer: direct3d11`
- **gpu**
  - 6 x `err: WARNING: WinPixEventRuntime.dll is not available. It is required for SDL_Push/PopGPUDebugGroup and SDL_InsertGPUDebugLabel to function correctly. See here for instructions on how to obtain it: ht`
  - 6 x `err: SDL_GPU Driver: D3D12`
  - 6 x `err: D3D12 Adapter: NVIDIA GeForce RTX 4070 Ti SUPER`
  - 6 x `err: D3D12 Driver: 32.0.16.1088`
  - 6 x `err: Created renderer: gpu`
