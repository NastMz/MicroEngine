## Results (<scratch>\full)

median (min-max) over the runs of each scenario; the last row is the median over scenarios and its ratio to the first column.

| Driver | runs | exit codes | renderer | SDL | adapter (profile) |
| --- | --- | --- | --- | --- | --- |
| default | 5 | 80/80 zero | direct3d11 | 3.5.0 | NVIDIA GeForce RTX 4070 Ti SUPER |
| gpu | 5 | 80/80 zero | gpu | 3.5.0 | NVIDIA GeForce RTX 4070 Ti SUPER |
| gpu-vulkan | 5 | 80/80 zero | gpu | 3.5.0 | NVIDIA GeForce RTX 4070 Ti SUPER (NVIDIA) |
| gpu-lowpower | 5 | 80/80 zero | gpu | 3.5.0 | AMD Radeon(TM) Graphics |

### Start-up: process start to first frame (ms)

| Scenario | default | gpu | gpu-vulkan | gpu-lowpower |
| --- | --- | --- | --- | --- |
| AssetGallery/tour | 1551 (1526-1794) | 1998 (1904-2422) | 1982 (1931-2164) | 2577 (2506-2708) |
| CourtyardKey/boss | 1833 (1775-2168) | 2230 (2002-2625) | 2301 (2140-2829) | 2419 (2361-3002) |
| CourtyardKey/ending | 1837 (1835-1998) | 2056 (2023-2087) | 2257 (2187-2404) | 2717 (2428-3680) |
| CourtyardKey/fight | 2006 (1861-2277) | 2241 (2083-2618) | 2297 (2268-2760) | 2611 (2500-3051) |
| CourtyardKey/opening | 2007 (1881-2314) | 2161 (2148-2546) | 2447 (2278-2723) | 2711 (2631-3058) |
| CourtyardKey/title | 2033 (1873-2723) | 2265 (2132-2667) | 2329 (2282-2617) | 2656 (2520-3430) |
| MeadowRun/end | 2084 (1979-2216) | 2331 (2216-2546) | 2496 (2319-2658) | 2931 (2640-3927) |
| MeadowRun/menus | 1915 (1832-2015) | 2145 (2114-2159) | 2322 (2198-2446) | 2586 (2425-2836) |
| MeadowRun/showcase | 2125 (2016-2192) | 2234 (2150-2428) | 2391 (2314-2676) | 3451 (2680-5325) |
| MeadowRun/smoke | 2101 (1978-5815) | 2224 (2200-3457) | 2555 (2188-4710) | 2722 (2578-2993) |
| NeonRift/ending | 1555 (1518-1760) | 1782 (1779-1835) | 1942 (1909-2004) | 2240 (2170-2353) |
| NeonRift/menus | 1614 (1557-2443) | 1870 (1793-1980) | 1974 (1909-2189) | 2227 (2185-2499) |
| NeonRift/stage1 | 1561 (1547-1586) | 1847 (1804-2023) | 1998 (1917-2153) | 2258 (2191-2378) |
| TrailDuel/master-battle | 2017 (1974-2125) | 2252 (2234-2410) | 2427 (2328-2544) | 2699 (2641-2748) |
| TrailDuel/newgame | 2110 (2022-2266) | 2293 (2274-2463) | 2513 (2470-3453) | 2752 (2628-3027) |
| TrailDuel/title | 2077 (1975-2704) | 2397 (2246-2667) | 2577 (2342-2632) | 2865 (2756-2943) |
| **median of scenarios** | **2006** | **2227** (x1.11) | **2326** (x1.16) | **2677** (x1.33) |

### SDL_CreateRenderer (ms)

| Scenario | default | gpu | gpu-vulkan | gpu-lowpower |
| --- | --- | --- | --- | --- |
| AssetGallery/tour | 162 (144-234) | 394 (375-596) | 548 (516-684) | 835 (764-923) |
| CourtyardKey/boss | 198 (148-235) | 480 (375-690) | 574 (515-692) | 796 (738-1203) |
| CourtyardKey/ending | 166 (159-169) | 385 (372-404) | 551 (519-680) | 932 (736-1807) |
| CourtyardKey/fight | 164 (147-314) | 402 (378-492) | 552 (521-805) | 828 (758-1102) |
| CourtyardKey/opening | 161 (155-251) | 436 (395-560) | 627 (541-783) | 847 (799-1129) |
| CourtyardKey/title | 220 (149-230) | 449 (400-649) | 570 (523-636) | 847 (754-1312) |
| MeadowRun/end | 163 (151-210) | 398 (373-438) | 652 (522-706) | 1081 (802-1492) |
| MeadowRun/menus | 160 (151-201) | 405 (387-488) | 558 (523-620) | 841 (755-1117) |
| MeadowRun/showcase | 217 (195-259) | 428 (383-479) | 572 (552-687) | 922 (806-1859) |
| MeadowRun/smoke | 212 (173-863) | 455 (447-973) | 631 (557-1494) | 866 (820-950) |
| NeonRift/ending | 164 (144-226) | 398 (382-414) | 550 (527-598) | 829 (773-858) |
| NeonRift/menus | 163 (160-198) | 414 (370-511) | 582 (519-639) | 814 (779-936) |
| NeonRift/stage1 | 156 (147-165) | 385 (372-481) | 583 (521-598) | 787 (763-837) |
| TrailDuel/master-battle | 157 (148-188) | 396 (386-480) | 540 (520-619) | 802 (796-828) |
| TrailDuel/newgame | 158 (150-182) | 401 (394-469) | 613 (541-1229) | 842 (786-983) |
| TrailDuel/title | 148 (145-237) | 399 (387-501) | 615 (521-663) | 924 (782-953) |
| **median of scenarios** | **163** | **401** (x2.46) | **573** (x3.52) | **841** (x5.17) |

### Frame interval p50, unpaced (µs)

| Scenario | default | gpu | gpu-vulkan | gpu-lowpower |
| --- | --- | --- | --- | --- |
| AssetGallery/tour | 283 (281-442) | 404 (385-674) | 236 (235-289) | 1326 (1187-1524) |
| CourtyardKey/boss | 843 (727-1207) | 897 (816-1243) | 730 (678-1163) | 1073 (987-1257) |
| CourtyardKey/ending | 184 (172-191) | 300 (278-307) | 148 (137-166) | 1061 (1049-1076) |
| CourtyardKey/fight | 219 (183-257) | 330 (279-454) | 155 (146-181) | 1053 (995-1102) |
| CourtyardKey/opening | 263 (202-330) | 313 (286-409) | 170 (143-214) | 1079 (1046-1097) |
| CourtyardKey/title | 195 (169-281) | 329 (292-388) | 168 (154-296) | 1126 (1021-1261) |
| MeadowRun/end | 366 (339-406) | 423 (410-430) | 314 (261-386) | 998 (935-1148) |
| MeadowRun/menus | 300 (266-353) | 407 (367-499) | 256 (214-290) | 1098 (1018-1129) |
| MeadowRun/showcase | 265 (245-343) | 487 (387-602) | 245 (213-311) | 1093 (1045-1177) |
| MeadowRun/smoke | 334 (268-520) | 466 (331-664) | 237 (213-313) | 1109 (1054-1294) |
| NeonRift/ending | 352 (339-415) | 464 (455-504) | 304 (293-402) | 1231 (1195-1373) |
| NeonRift/menus | 737 (666-778) | 759 (734-841) | 636 (612-896) | 1226 (1219-1237) |
| NeonRift/stage1 | 264 (260-354) | 401 (390-451) | 257 (239-381) | 1303 (1254-1372) |
| TrailDuel/master-battle | 186 (172-203) | 266 (247-368) | 127 (103-140) | 1044 (961-1085) |
| TrailDuel/newgame | 214 (197-252) | 341 (317-394) | 167 (138-178) | 1067 (1000-1204) |
| TrailDuel/title | 285 (231-330) | 375 (365-496) | 228 (207-236) | 947 (914-1011) |
| **median of scenarios** | **274** | **403** (x1.47) | **237** (x0.86) | **1086** (x3.96) |

### Frame interval p95, unpaced (µs)

| Scenario | default | gpu | gpu-vulkan | gpu-lowpower |
| --- | --- | --- | --- | --- |
| AssetGallery/tour | 1523 (1040-2103) | 1388 (1269-2879) | 1775 (1630-2128) | 2385 (2355-4098) |
| CourtyardKey/boss | 1520 (1101-3311) | 1849 (1694-4388) | 1178 (1063-6959) | 2622 (2525-3336) |
| CourtyardKey/ending | 384 (312-427) | 533 (437-565) | 277 (216-382) | 1734 (1642-1950) |
| CourtyardKey/fight | 714 (583-966) | 919 (857-1813) | 558 (470-690) | 1887 (1642-3346) |
| CourtyardKey/opening | 787 (377-2848) | 636 (518-1410) | 301 (230-420) | 1912 (1635-2340) |
| CourtyardKey/title | 490 (471-1164) | 1929 (1425-2197) | 778 (645-2429) | 4342 (3369-5583) |
| MeadowRun/end | 668 (627-842) | 830 (790-912) | 597 (491-673) | 1952 (1787-3298) |
| MeadowRun/menus | 878 (666-1097) | 848 (793-1616) | 589 (503-695) | 2217 (1707-2331) |
| MeadowRun/showcase | 722 (625-1424) | 2413 (1475-3040) | 845 (710-939) | 2851 (2601-4749) |
| MeadowRun/smoke | 926 (709-32458) | 1654 (872-6110) | 682 (570-3950) | 3917 (1980-12215) |
| NeonRift/ending | 930 (909-1153) | 979 (859-1043) | 870 (807-1228) | 2104 (1946-2494) |
| NeonRift/menus | 1645 (1021-2331) | 1546 (1383-2511) | 1498 (1106-1985) | 2924 (2611-3135) |
| NeonRift/stage1 | 613 (524-859) | 2203 (2129-2385) | 838 (767-1907) | 4244 (3993-5576) |
| TrailDuel/master-battle | 2909 (2799-3276) | 3720 (3057-4587) | 3235 (3038-3669) | 5873 (5694-6162) |
| TrailDuel/newgame | 558 (441-684) | 704 (640-816) | 426 (391-477) | 1713 (1566-3263) |
| TrailDuel/title | 622 (484-969) | 1197 (742-1921) | 566 (451-638) | 2036 (1945-2213) |
| **median of scenarios** | **755** | **1293** (x1.71) | **730** (x0.97) | **2301** (x3.05) |

### EndFrame/present p50 (µs)

| Scenario | default | gpu | gpu-vulkan | gpu-lowpower |
| --- | --- | --- | --- | --- |
| AssetGallery/tour | 68 (63-109) | 188 (173-294) | 41 (40-47) | 1034 (916-1176) |
| CourtyardKey/boss | 107 (90-173) | 232 (195-318) | 79 (69-149) | 293 (245-341) |
| CourtyardKey/ending | 69 (57-77) | 167 (159-172) | 34 (29-48) | 977 (977-1002) |
| CourtyardKey/fight | 74 (66-85) | 191 (168-254) | 44 (42-56) | 855 (839-888) |
| CourtyardKey/opening | 92 (70-125) | 187 (165-239) | 45 (38-61) | 881 (867-882) |
| CourtyardKey/title | 69 (57-103) | 197 (170-228) | 47 (40-87) | 944 (866-986) |
| MeadowRun/end | 82 (78-94) | 200 (180-211) | 53 (40-69) | 826 (743-883) |
| MeadowRun/menus | 82 (72-95) | 190 (175-243) | 52 (42-55) | 811 (778-843) |
| MeadowRun/showcase | 79 (71-94) | 259 (196-308) | 52 (45-69) | 844 (811-883) |
| MeadowRun/smoke | 90 (81-192) | 231 (180-371) | 55 (48-81) | 841 (812-896) |
| NeonRift/ending | 86 (76-101) | 191 (186-236) | 52 (46-82) | 958 (943-1083) |
| NeonRift/menus | 109 (92-130) | 212 (199-259) | 75 (63-102) | 584 (491-621) |
| NeonRift/stage1 | 76 (73-95) | 197 (189-225) | 58 (55-96) | 1026 (984-1074) |
| TrailDuel/master-battle | 108 (105-115) | 191 (177-258) | 54 (41-61) | 922 (884-950) |
| TrailDuel/newgame | 73 (65-88) | 187 (173-207) | 34 (32-53) | 863 (847-903) |
| TrailDuel/title | 86 (63-118) | 196 (175-246) | 49 (41-57) | 719 (715-788) |
| **median of scenarios** | **82** | **194** (x2.36) | **52** (x0.63) | **859** (x10.48) |

### Peak commit (MB)

| Scenario | default | gpu | gpu-vulkan | gpu-lowpower |
| --- | --- | --- | --- | --- |
| AssetGallery/tour | 122 (121-123) | 173 (170-173) | 298 (297-314) | 252 (251-258) |
| CourtyardKey/boss | 180 (179-186) | 227 (226-229) | 362 (360-379) | 320 (318-321) |
| CourtyardKey/ending | 165 (164-168) | 215 (214-215) | 352 (350-354) | 305 (303-307) |
| CourtyardKey/fight | 164 (163-165) | 215 (215-215) | 349 (349-352) | 303 (301-305) |
| CourtyardKey/opening | 166 (165-166) | 216 (215-219) | 355 (354-356) | 301 (300-304) |
| CourtyardKey/title | 162 (162-165) | 214 (212-216) | 349 (348-365) | 301 (301-308) |
| MeadowRun/end | 238 (238-240) | 288 (284-290) | 420 (415-422) | 375 (369-375) |
| MeadowRun/menus | 241 (241-242) | 291 (290-294) | 421 (420-421) | 377 (372-384) |
| MeadowRun/showcase | 237 (224-238) | 288 (287-289) | 415 (413-418) | 366 (364-372) |
| MeadowRun/smoke | 244 (225-244) | 293 (288-294) | 424 (404-426) | 374 (372-376) |
| NeonRift/ending | 123 (123-125) | 173 (172-174) | 300 (299-302) | 282 (271-283) |
| NeonRift/menus | 126 (125-126) | 174 (174-175) | 303 (302-319) | 283 (281-291) |
| NeonRift/stage1 | 124 (124-125) | 175 (173-176) | 301 (301-302) | 281 (278-283) |
| TrailDuel/master-battle | 325 (282-339) | 374 (342-387) | 513 (468-516) | 473 (423-473) |
| TrailDuel/newgame | 273 (271-273) | 322 (311-323) | 446 (438-449) | 403 (389-405) |
| TrailDuel/title | 270 (261-272) | 320 (315-322) | 444 (440-461) | 402 (401-410) |
| **median of scenarios** | **173** | **222** (x1.28) | **358** (x2.07) | **312** (x1.80) |

### Peak working set (MB)

| Scenario | default | gpu | gpu-vulkan | gpu-lowpower |
| --- | --- | --- | --- | --- |
| AssetGallery/tour | 120 (120-120) | 157 (156-158) | 213 (213-248) | 225 (224-240) |
| CourtyardKey/boss | 191 (190-198) | 225 (223-227) | 279 (278-305) | 304 (302-307) |
| CourtyardKey/ending | 174 (173-178) | 212 (212-213) | 269 (268-269) | 294 (294-295) |
| CourtyardKey/fight | 174 (173-174) | 210 (210-211) | 266 (266-269) | 287 (286-290) |
| CourtyardKey/opening | 178 (178-180) | 215 (215-217) | 273 (273-277) | 293 (293-295) |
| CourtyardKey/title | 172 (172-175) | 209 (209-220) | 267 (264-300) | 287 (285-300) |
| MeadowRun/end | 241 (240-241) | 273 (271-277) | 336 (330-337) | 353 (345-354) |
| MeadowRun/menus | 245 (245-246) | 280 (280-281) | 338 (338-339) | 353 (349-369) |
| MeadowRun/showcase | 240 (226-240) | 275 (273-275) | 332 (331-334) | 348 (345-349) |
| MeadowRun/smoke | 247 (229-248) | 282 (279-283) | 343 (322-343) | 356 (355-361) |
| NeonRift/ending | 123 (123-123) | 158 (158-158) | 216 (215-216) | 236 (235-236) |
| NeonRift/menus | 125 (125-125) | 159 (159-170) | 216 (216-250) | 235 (235-251) |
| NeonRift/stage1 | 124 (124-124) | 159 (159-159) | 215 (215-216) | 235 (235-236) |
| TrailDuel/master-battle | 314 (273-327) | 351 (319-361) | 423 (380-424) | 437 (392-439) |
| TrailDuel/newgame | 267 (266-267) | 302 (293-302) | 361 (354-364) | 375 (364-375) |
| TrailDuel/title | 264 (256-266) | 299 (295-311) | 358 (354-393) | 371 (370-388) |
| **median of scenarios** | **184** | **220** (x1.19) | **276** (x1.50) | **299** (x1.62) |

### Log lines other than the launcher's own (count over all runs and scenarios)

- **default**
  - 80 x `err: Created renderer: direct3d11`
- **gpu**
  - 80 x `err: WARNING: WinPixEventRuntime.dll is not available. It is required for SDL_Push/PopGPUDebugGroup and SDL_InsertGPUDebugLabel to function correctly. See here for instructions on how to obtain it: ht`
  - 80 x `err: SDL_GPU Driver: D3D12`
  - 80 x `err: D3D12 Adapter: NVIDIA GeForce RTX 4070 Ti SUPER`
  - 80 x `err: D3D12 Driver: 32.0.16.1088`
  - 80 x `err: Created renderer: gpu`
- **gpu-vulkan**
  - 80 x `err: SDL_GPU Driver: Vulkan`
  - 80 x `err: Vulkan Device: NVIDIA GeForce RTX 4070 Ti SUPER`
  - 80 x `err: Vulkan Driver: NVIDIA 610.88`
  - 80 x `err: Vulkan Conformance: 1.4.3.3`
  - 80 x `err: Created renderer: gpu`
- **gpu-lowpower**
  - 80 x `err: WARNING: WinPixEventRuntime.dll is not available. It is required for SDL_Push/PopGPUDebugGroup and SDL_InsertGPUDebugLabel to function correctly. See here for instructions on how to obtain it: ht`
  - 80 x `err: SDL_GPU Driver: D3D12`
  - 80 x `err: D3D12 Adapter: AMD Radeon(TM) Graphics`
  - 80 x `err: D3D12 Driver: 32.0.21042.62`
  - 80 x `err: Created renderer: gpu`
