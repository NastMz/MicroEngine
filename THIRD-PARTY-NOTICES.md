# Third-party notices

MicroEngine and the software built with it include components made by others. This page lists them with their
licences and the notices those licences ask to keep. It covers the engine's runtime (desktop and browser), the
browser builds of the demo games, the demo games' art and audio, and this website.

MicroEngine's own code is under the MIT License (`LICENSE` in the repository); the four demo games are not, and
keep all rights reserved (`games/LICENSE`). Both notices also ship in every browser package of the demos.

## Engine runtime: every game, desktop and browser

| Component | Use | Licence | Copyright |
| --- | --- | --- | --- |
| Raylib-cs 8.1.0 | C# bindings for raylib (browser packages of games that reference raylib keep these bindings, trimmed, but not raylib itself) | zlib | Copyright (C) 2018-2025 raylib-cs |
| raylib 6.0 | windowing, graphics, input and audio on Windows, Linux and macOS, as the alternative to SDL | zlib/libpng | Copyright (c) 2013-2026 Ramon Santamaria (@raysan5) |
| SDL 3.5 | windowing, graphics, input, touch and audio on Windows, Linux, macOS, Android and iOS, and in browser packages built for SDL (compiled to WebAssembly from SDL's sources) | zlib | Copyright (C) 1997-2026 Sam Lantinga |
| ppy.SDL3-CS 2026.1002.1 | C# bindings for SDL 3 and its native builds | MIT | Copyright (c) 2024 ppy Pty Ltd |
| NVorbis 0.10.5 | OGG Vorbis music decoding in the engine's mixer, on every platform (a dependency of the `MicroEngine` package, used by `MicroEngine.Audio`) | MIT | Copyright (c) 2020 Andrew Ward |
| StbImageSharp 2.30.15 | JPEG, BMP, TGA, GIF, PSD and 16-bit or interlaced PNG decoding (a dependency of the `MicroEngine` package, used by `MicroEngine.Graphics2D`) | public domain (Unlicense) or MIT | StbSharp team (Roman Shapiro), a port of stb_image by Sean Barrett |
| FontStashSharp 1.3.10, FontStashSharp.Base 1.1.9, FontStashSharp.Rasterizers.StbTrueTypeSharp 1.1.9 | TTF and OTF fonts: glyph rasterization and caching (a dependency of the `MicroEngine` package, used by `MicroEngine.Text`) | zlib | FontStashSharp team (Roman Shapiro), based on fontstash by Mikko Mononen |
| StbTrueTypeSharp 1.26.12 | TrueType and OpenType glyph rasterization (for FontStashSharp) | public domain (Unlicense) or MIT | StbSharp team (Roman Shapiro), a port of stb_truetype by Sean Barrett |
| Cyotek.Drawing.BitmapFont 2.0.4 | AngelCode BMFont reading (a dependency of FontStashSharp) | MIT | Copyright (c) 2012-2022 Cyotek Ltd |

Libraries bundled inside raylib and compiled into it:

| Library | Licence | Copyright |
| --- | --- | --- |
| miniaudio, dr_wav, dr_mp3, dr_flac (David Reid) | public domain or MIT No Attribution | — |
| stb_image, stb_image_resize2, stb_image_write, stb_truetype, stb_rect_pack, stb_perlin, stb_vorbis (Sean Barrett) | public domain or MIT | — |
| sinfl, sdefl (Micha Mettke) | public domain or MIT | — |
| jar_mod (Joshua Reisenauer) | public domain | — |
| jar_xm (Joshua Reisenauer), based on libxm (Romain Dalmaso) | WTFPL | — |
| rprand | zlib/libpng | Copyright (c) 2023 Ramon Santamaria (@raysan5) |
| GLFW (desktop builds only) | zlib/libpng | Copyright (c) 2002-2006 Marcus Geelnard; Copyright (c) 2006-2019 Camilla Löwy |
| qoa | MIT | Copyright (c) 2023, Dominic Szablewski |
| qoi | MIT | Copyright (c) 2021, Dominic Szablewski |
| cgltf | MIT | Copyright (c) 2018-2021 Johannes Kuhlmann |
| par_shapes | MIT | Copyright (c) 2019 Philip Rideout |
| tinyobj_loader_c | MIT | Copyright (c) 2016 - 2019 Syoyo Fujita and many contributors |
| m3d | MIT | Copyright (C) 2020 bzt (bztsrc@gitlab) |
| vox_loader | MIT | Copyright (c) 2021 Johann Nadalutti |

## The `microengine` tool's shader compiler

`microengine shaders` ships with the libraries below. It does not ship the HLSL compiler: DXC (the
`Microsoft.Direct3D.DXC` 1.8.2505.32 package: `dxc.exe` and `dxcompiler.dll` under the University of Illinois/NCSA Open
Source License, `dxil.dll` under the Microsoft Software License Terms for the DirectX Shader Compiler, Windows only) is
restored by NuGet into the developer's packages folder on first use, under those terms. Games carry only the compiled
shaders, never the compilers.

| Component | Use | Licence | Copyright |
| --- | --- | --- | --- |
| Silk.NET.SPIRV.Cross 2.22.0, Silk.NET.SPIRV 2.22.0, Silk.NET.Core 2.22.0 | C# bindings for SPIRV-Cross | MIT | Copyright (c) 2021- .NET Foundation and Contributors |
| SPIRV-Cross (native, Silk.NET.SPIRV.Cross.Native 2.22.0) | SPIR-V to MSL translation and resource reflection | Apache License 2.0 | Copyright 2015-2021 Arm Limited and contributors |

## Optional text shaping

Games that add the optional `MicroEngine.Text.Shaping` package also ship HarfBuzz, as a native library on desktops
and phones and linked into the WebAssembly of browser packages. `microengine package` puts its licence file in the
packages of those games.

| Component | Use | Licence | Copyright |
| --- | --- | --- | --- |
| HarfBuzzSharp 14.4.0, HarfBuzzSharp.NativeAssets.* 14.4.0 | C# bindings for HarfBuzz and its native builds for every runtime | MIT | Copyright (c) 2015-2016 Xamarin, Inc.; Copyright (c) 2017-2018 Microsoft Corporation |
| Unicode Character Database 16.0 (the bidirectional classes, as a table generated into `MicroEngine.Text.Shaping`) | right-to-left and left-to-right runs inside a line | Unicode License v3 (below) | Copyright © 1991-2026 Unicode, Inc. |
| HarfBuzz (inside HarfBuzzSharp's native builds) | text shaping: glyph choice and positioning for complex and right-to-left scripts | Old MIT (below) | Copyright © 2010-2012 Google, Inc.; © 2012 Mozilla Foundation; © 2004-2010 Red Hat, Inc.; and the other holders listed below |

## Optional Box2D physics backend

Games that add the optional `MicroEngine.Physics.Box2D` package also ship Box2D, compiled into that backend's native library. Its licence file is shipped inside the package.

| Component | Use | Licence | Copyright |
| --- | --- | --- | --- |
| Box2D 3.2.0 | 2D rigid-body physics: dynamic bodies, collision, queries and the character mover | MIT | Copyright (c) 2022 Erin Catto |

## Browser builds of the demos

Each browser package also contains the .NET runtime compiled to WebAssembly and code generated by Emscripten. The
complete licence files are shipped inside every package, in its `licenses/` folder.

| Component | Licence | Copyright |
| --- | --- | --- |
| .NET runtime and libraries (Microsoft) | MIT; components listed in `licenses/dotnet-THIRD-PARTY-NOTICES.txt` | Copyright (c) .NET Foundation and Contributors |
| Emscripten runtime code | MIT or University of Illinois/NCSA | Copyright (c) 2010-2014 Emscripten authors |

## Native downloads of the demos

Each desktop download (Windows, Linux, macOS) contains the .NET runtime, SDL 3 and raylib; each Android APK contains the
.NET runtime for Android and SDL 3. The engine runtime libraries are listed above; the .NET runtime's complete licence
and third-party notices are shipped inside every package, in its `licenses/` folder (`assets/licenses/` in an APK).

| Component | Licence | Copyright |
| --- | --- | --- |
| .NET runtime and libraries (Microsoft) | MIT; components listed in `licenses/dotnet-THIRD-PARTY-NOTICES.txt` | Copyright (c) .NET Foundation and Contributors |

## Demo game art and audio

Courtyard Key, Trail Duel and Neon Rift use third-party art and audio. Most of it is released under CC0 1.0 (public
domain dedication): credit is not required and is given in each game's asset credits. Trail Duel's village art is
"Serene Village revamped" by LimeZu (https://limezu.itch.io/serenevillagerevamped), licensed under CC BY 4.0
(https://creativecommons.org/licenses/by/4.0/); the colours of the sheets are graded when the game loads them. Each
game's credits list every file, its author and its licence. Meadow Run uses no third-party assets.

## This website

| Component | Licence | Copyright |
| --- | --- | --- |
| Bricolage Grotesque font (display face and the outlined wordmark in the logo and Open Graph image) | SIL Open Font License 1.1 | Copyright 2022 The Bricolage Grotesque Project Authors |
| IBM Plex Sans and IBM Plex Mono fonts | SIL Open Font License 1.1 | Copyright 2017, 2019 IBM Corp. |
| Astro | MIT | Copyright (c) 2021 Fred K. Schott |
| Starlight | MIT | Copyright (c) 2023 Astro contributors |
| Pagefind (search) | MIT | Copyright (c) Pagefind |
| three.js (the 3D scene of the landing) | MIT | Copyright (c) 2010-2025 three.js authors |

## Licence texts

### MIT License

The MIT-licensed components above are provided under these terms, each with its copyright line from the tables:

```text
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### zlib/libpng License

SDL 3, Raylib-cs, raylib, rprand and GLFW are provided under these terms, each with its copyright line from the tables
(every package also ships SDL's own licence file in `licenses/SDL-LICENSE.txt`, `assets/licenses/` in an APK, and
desktop packages raylib's in `licenses/raylib-LICENSE.txt`):

```text
This software is provided "as-is", without any express or implied warranty. In no event
will the authors be held liable for any damages arising from the use of this software.

Permission is granted to anyone to use this software for any purpose, including commercial
applications, and to alter it and redistribute it freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not claim that you
  wrote the original software. If you use this software in a product, an acknowledgment
  in the product documentation would be appreciated but is not required.

  2. Altered source versions must be plainly marked as such, and must not be misrepresented
  as being the original software.

  3. This notice may not be removed or altered from any source distribution.
```

### SIL Open Font License 1.1

```text
SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007
-----------------------------------------------------------

PREAMBLE
The goals of the Open Font License (OFL) are to stimulate worldwide
development of collaborative font projects, to support the font creation
efforts of academic and linguistic communities, and to provide a free and
open framework in which fonts may be shared and improved in partnership
with others.

The OFL allows the licensed fonts to be used, studied, modified and
redistributed freely as long as they are not sold by themselves. The
fonts, including any derivative works, can be bundled, embedded,
redistributed and/or sold with any software provided that any reserved
names are not used by derivative works. The fonts and derivatives,
however, cannot be released under any other type of license. The
requirement for fonts to remain under this license does not apply
to any document created using the fonts or their derivatives.

DEFINITIONS
"Font Software" refers to the set of files released by the Copyright
Holder(s) under this license and clearly marked as such. This may
include source files, build scripts and documentation.

"Reserved Font Name" refers to any names specified as such after the
copyright statement(s).

"Original Version" refers to the collection of Font Software components as
distributed by the Copyright Holder(s).

"Modified Version" refers to any derivative made by adding to, deleting,
or substituting -- in part or in whole -- any of the components of the
Original Version, by changing formats or by porting the Font Software to a
new environment.

"Author" refers to any designer, engineer, programmer, technical
writer or other person who contributed to the Font Software.

PERMISSION & CONDITIONS
Permission is hereby granted, free of charge, to any person obtaining
a copy of the Font Software, to use, study, copy, merge, embed, modify,
redistribute, and sell modified and unmodified copies of the Font
Software, subject to the following conditions:

1) Neither the Font Software nor any of its individual components,
in Original or Modified Versions, may be sold by itself.

2) Original or Modified Versions of the Font Software may be bundled,
redistributed and/or sold with any software, provided that each copy
contains the above copyright notice and this license. These can be
included either as stand-alone text files, human-readable headers or
in the appropriate machine-readable metadata fields within text or
binary files as long as those fields can be easily viewed by the user.

3) No Modified Version of the Font Software may use the Reserved Font
Name(s) unless explicit written permission is granted by the corresponding
Copyright Holder. This restriction only applies to the primary font name as
presented to the users.

4) The name(s) of the Copyright Holder(s) or the Author(s) of the Font
Software shall not be used to promote, endorse or advertise any
Modified Version, except to acknowledge the contribution(s) of the
Copyright Holder(s) and the Author(s) or with their explicit written
permission.

5) The Font Software, modified or unmodified, in part or in whole,
must be distributed entirely under this license, and must not be
distributed under any other license. The requirement for fonts to
remain under this license does not apply to any document created
using the Font Software.

TERMINATION
This license becomes null and void if any of the above conditions are
not met.

DISCLAIMER
THE FONT SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO ANY WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
OF COPYRIGHT, PATENT, TRADEMARK, OR OTHER RIGHT. IN NO EVENT SHALL THE
COPYRIGHT HOLDER BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
INCLUDING ANY GENERAL, SPECIAL, INDIRECT, INCIDENTAL, OR CONSEQUENTIAL
DAMAGES, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF THE USE OR INABILITY TO USE THE FONT SOFTWARE OR FROM
OTHER DEALINGS IN THE FONT SOFTWARE.
```

### HarfBuzz licence (Old MIT)

```text
HarfBuzz is licensed under the so-called "Old MIT" license.  Details follow.
For parts of HarfBuzz that are licensed under different licenses see individual
files names COPYING in subdirectories where applicable.

Copyright © 2010,2011,2012  Google, Inc.
Copyright © 2012  Mozilla Foundation
Copyright © 2011  Codethink Limited
Copyright © 2008,2010  Nokia Corporation and/or its subsidiary(-ies)
Copyright © 2009  Keith Stribley
Copyright © 2009  Martin Hosken and SIL International
Copyright © 2007  Chris Wilson
Copyright © 2006  Behdad Esfahbod
Copyright © 2005  David Turner
Copyright © 2004,2007,2008,2009,2010  Red Hat, Inc.
Copyright © 1998-2004  David Turner and Werner Lemberg

For full copyright notices consult the individual files in the package.


Permission is hereby granted, without written agreement and without
license or royalty fees, to use, copy, modify, and distribute this
software and its documentation for any purpose, provided that the
above copyright notice and the following two paragraphs appear in
all copies of this software.

IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE TO ANY PARTY FOR
DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES
ARISING OUT OF THE USE OF THIS SOFTWARE AND ITS DOCUMENTATION, EVEN
IF THE COPYRIGHT HOLDER HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH
DAMAGE.

THE COPYRIGHT HOLDER SPECIFICALLY DISCLAIMS ANY WARRANTIES, INCLUDING,
BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE.  THE SOFTWARE PROVIDED HEREUNDER IS
ON AN "AS IS" BASIS, AND THE COPYRIGHT HOLDER HAS NO OBLIGATION TO
PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.
```

### Unicode License v3

```text
UNICODE LICENSE V3

COPYRIGHT AND PERMISSION NOTICE

Copyright © 1991-2026 Unicode, Inc.

NOTICE TO USER: Carefully read the following legal agreement. BY
DOWNLOADING, INSTALLING, COPYING OR OTHERWISE USING DATA FILES, AND/OR
SOFTWARE, YOU UNEQUIVOCALLY ACCEPT, AND AGREE TO BE BOUND BY, ALL OF THE
TERMS AND CONDITIONS OF THIS AGREEMENT. IF YOU DO NOT AGREE, DO NOT
DOWNLOAD, INSTALL, COPY, DISTRIBUTE OR USE THE DATA FILES OR SOFTWARE.

Permission is hereby granted, free of charge, to any person obtaining a
copy of data files and any associated documentation (the "Data Files") or
software and any associated documentation (the "Software") to deal in the
Data Files or Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, and/or sell
copies of the Data Files or Software, and to permit persons to whom the
Data Files or Software are furnished to do so, provided that either (a)
this copyright and permission notice appear with all copies of the Data
Files or Software, or (b) this copyright and permission notice appear in
associated Documentation.

THE DATA FILES AND SOFTWARE ARE PROVIDED "AS IS", WITHOUT WARRANTY OF ANY
KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF
THIRD PARTY RIGHTS.

IN NO EVENT SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN THIS NOTICE
BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL DAMAGES,
OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS,
WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION,
ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THE DATA
FILES OR SOFTWARE.

Except as contained in this notice, the name of a copyright holder shall
not be used in advertising or otherwise to promote the sale, use or other
dealings in these Data Files or Software without prior written
authorization of the copyright holder.
```

### Apache License 2.0

SPIRV-Cross is licensed under the Apache License, Version 2.0 (the "License"); you may not use it except in
compliance with the License. You may obtain a copy of the License at https://www.apache.org/licenses/LICENSE-2.0.
Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
specific language governing permissions and limitations under the License.
