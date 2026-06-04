# Third-Party Notices

ArIEC103 does not include external IEC 60870 protocol stack source code.

## Runtime / Framework

- .NET 8 SDK / runtime
- System.IO.Ports NuGet package is used only for serial COM-port I/O in `ArIEC103.Master`.

## Artwork / UI Assets

- `src/ArIEC103.Desktop/Assets/Icons/iec103-icon.png` and `src/ArIEC103.Desktop/Assets/Icons/iec103-app.ico` are original generated artwork for ArIEC103.
- `src/ArIEC103.Desktop/Resources/LucideIcons.xaml` contains Lucide-style outline geometries ported to WPF for local rendering. Lucide Icons are distributed under the ISC license.

## Protocol Stack Policy

The IEC 60870-5-103 framing, decoding, polling policy, and evidence logic in this repository are implemented as clean-room source code under Apache-2.0.

No source code from lib60870, Open103, Wireshark, commercial IEC protocol stacks, or GPL protocol implementations is included.

## Benchmark-only references

The project documentation may mention public product/protocol pages such as Axon Test5, Wireshark IEC-103 field reference, MZ Automation CS103, JPEmbedded IEC-103, and Open103 for market/feature awareness. No source code, tables, or implementation logic from those projects is included in ArIEC103.

## Lucide Icons

Lucide Icons are used as simple outline geometry references for the WPF command rail.

License: ISC

Copyright (c) for Lucide contributors.

Permission to use, copy, modify, and/or distribute this software for any purpose with or without fee is hereby granted, provided that the copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

## Generated Application Icon
The `IEC / 103` application icon was generated as a project-owned raster asset. Inter font used for internal rasterized IEC 103 icon generation is not redistributed as a font file in this repository.
