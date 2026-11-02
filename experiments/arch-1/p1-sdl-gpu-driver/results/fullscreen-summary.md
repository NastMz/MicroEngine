## Results (<scratch>\fullscreen)

median (min-max) over the runs of each scenario; the last row is the median over scenarios and its ratio to the first column.

| Driver | runs | exit codes | renderer | SDL | adapter (profile) |
| --- | --- | --- | --- | --- | --- |
| default | 1 | 2/2 zero | direct3d11 | 3.5.0 | NVIDIA GeForce RTX 4070 Ti SUPER |
| gpu | 1 | 2/2 zero | gpu | 3.5.0 | NVIDIA GeForce RTX 4070 Ti SUPER |
| gpu-vulkan | 1 | 2/2 zero | gpu | 3.5.0 | NVIDIA GeForce RTX 4070 Ti SUPER (NVIDIA) |

### Start-up: process start to first frame (ms)

| Scenario | default | gpu | gpu-vulkan |
| --- | --- | --- | --- |
| MeadowRun/smoke | 2008 (2008-2008) | 2157 (2157-2157) | 2425 (2425-2425) |
| NeonRift/stage1 | 1817 (1817-1817) | 2269 (2269-2269) | 2254 (2254-2254) |
| **median of scenarios** | **1913** | **2213** (x1.16) | **2339** (x1.22) |

### SDL_CreateRenderer (ms)

| Scenario | default | gpu | gpu-vulkan |
| --- | --- | --- | --- |
| MeadowRun/smoke | 191 (191-191) | 417 (417-417) | 616 (616-616) |
| NeonRift/stage1 | 197 (197-197) | 594 (594-594) | 751 (751-751) |
| **median of scenarios** | **194** | **506** (x2.60) | **684** (x3.52) |

### Frame interval p50, unpaced (µs)

| Scenario | default | gpu | gpu-vulkan |
| --- | --- | --- | --- |
| MeadowRun/smoke | 270 (270-270) | 10001 (10001-10001) | 330 (330-330) |
| NeonRift/stage1 | 403 (403-403) | 9986 (9986-9986) | 318 (318-318) |
| **median of scenarios** | **337** | **9994** (x29.70) | **324** (x0.96) |

### Frame interval p95, unpaced (µs)

| Scenario | default | gpu | gpu-vulkan |
| --- | --- | --- | --- |
| MeadowRun/smoke | 652 (652-652) | 10354 (10354-10354) | 978 (978-978) |
| NeonRift/stage1 | 1272 (1272-1272) | 11284 (11284-11284) | 1023 (1023-1023) |
| **median of scenarios** | **962** | **10819** (x11.25) | **1001** (x1.04) |

### EndFrame/present p50 (µs)

| Scenario | default | gpu | gpu-vulkan |
| --- | --- | --- | --- |
| MeadowRun/smoke | 74 (74-74) | 9823 (9823-9823) | 116 (116-116) |
| NeonRift/stage1 | 115 (115-115) | 9774 (9774-9774) | 107 (107-107) |
| **median of scenarios** | **95** | **9799** (x103.69) | **112** (x1.18) |

### Peak commit (MB)

| Scenario | default | gpu | gpu-vulkan |
| --- | --- | --- | --- |
| MeadowRun/smoke | 244 (244-244) | 302 (302-302) | 431 (431-431) |
| NeonRift/stage1 | 123 (123-123) | 180 (180-180) | 308 (308-308) |
| **median of scenarios** | **184** | **241** (x1.31) | **369** (x2.01) |

### Peak working set (MB)

| Scenario | default | gpu | gpu-vulkan |
| --- | --- | --- | --- |
| MeadowRun/smoke | 248 (248-248) | 288 (288-288) | 349 (349-349) |
| NeonRift/stage1 | 124 (124-124) | 161 (161-161) | 223 (223-223) |
| **median of scenarios** | **186** | **225** (x1.21) | **286** (x1.54) |

### Log lines other than the launcher's own (count over all runs and scenarios)

- **default**
  - 2 x `err: Created renderer: direct3d11`
- **gpu**
  - 2 x `err: WARNING: WinPixEventRuntime.dll is not available. It is required for SDL_Push/PopGPUDebugGroup and SDL_InsertGPUDebugLabel to function correctly. See here for instructions on how to obtain it: ht`
  - 2 x `err: SDL_GPU Driver: D3D12`
  - 2 x `err: D3D12 Adapter: NVIDIA GeForce RTX 4070 Ti SUPER`
  - 2 x `err: D3D12 Driver: 32.0.16.1088`
  - 2 x `err: Created renderer: gpu`
- **gpu-vulkan**
  - 2 x `err: SDL_GPU Driver: Vulkan`
  - 2 x `err: Vulkan Device: NVIDIA GeForce RTX 4070 Ti SUPER`
  - 2 x `err: Vulkan Driver: NVIDIA 610.88`
  - 2 x `err: Vulkan Conformance: 1.4.3.3`
  - 2 x `err: Created renderer: gpu`
