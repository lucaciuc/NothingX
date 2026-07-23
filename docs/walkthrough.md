# Refactoring Changelog

## State Management
- Replaced 20+ duplicated properties in `MainViewModel` by directly exposing `DeviceInfo`. XAML now binds directly to the underlying model (e.g. `{Binding Device.Battery.Case}`).
- This resolves data synchronization delays and significantly reduces boilerplate code.

## UI Components
- Replaced string-based view switching with a strongly-typed `DeviceView` enum.
- Cleaned up `CircularEqControl.xaml.cs` by extracting geometry math and drag delta calculations into isolated methods.
- Shared EQ serialization logic between `SimpleEq.cs` and `CustomEq.cs` by moving it directly into the `EqBand` class.

## Protocol Layer
- Cleaned up `Commands.cs` by removing dead constants that were causing opcode collisions.
- Removed unused packet generation methods from `PacketBuilder.cs`.
- Replaced the hardcoded `0x20` CRC mask in `PacketParser` with `NothingPacket.MASK_CRC`.
- Removed a leftover debug file-write operation (`File.AppendAllText`) from the packet receiver loop in `NothingProtocol.cs`.
