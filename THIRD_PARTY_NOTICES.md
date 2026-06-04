# Third-Party Notices

ArIEC103 does not include external IEC 60870 protocol stack source code.

## Runtime / Framework

- .NET 8 SDK / runtime
- System.IO.Ports NuGet package is used only for serial COM-port I/O in `ArIEC103.Master`.

## Artwork / UI Assets

- `src/ArIEC103.Desktop/Assets/Icons/ariec103-icon.png` is original generated artwork for ArIEC103.
- `src/ArIEC103.Desktop/Resources/ArProtocolIcons.xaml` contains original WPF geometry icons created for this project.

## Protocol Stack Policy

The IEC 60870-5-103 framing, decoding, polling policy, and evidence logic in this repository are implemented as clean-room source code under Apache-2.0.

No source code from lib60870, Open103, Wireshark, commercial IEC protocol stacks, or GPL protocol implementations is included.

## Benchmark-only references

The project documentation may mention public product/protocol pages such as Axon Test5, Wireshark IEC-103 field reference, MZ Automation CS103, JPEmbedded IEC-103, and Open103 for market/feature awareness. No source code, tables, or implementation logic from those projects is included in ArIEC103.
