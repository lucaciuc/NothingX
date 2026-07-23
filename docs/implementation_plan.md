# Refactoring Notes

The code review highlighted several areas of technical debt. This document tracks the planned fixes.

## 1. Remove Debug Code
- File: `NothingX/Protocol/NothingProtocol.cs`
- Task: Remove the empty `catch` block and `File.AppendAllText` call at line 657. It's unsafe in the packet receive loop.

## 2. MainViewModel State Synchronization
- Files: `NothingX/ViewModels/MainViewModel.cs`, `NothingX/MainWindow.xaml`
- Task: Stop duplicating state in `MainViewModel`. Expose `DeviceInfo Device` directly. Update XAML bindings to point to `Device.*` (e.g., `{Binding Device.Battery.Case}`). Delete `SyncFromDevice()`.

## 3. Protocol Commands Cleanup
- File: `NothingX/Protocol/Commands.cs`
- Task: Remove `GET_HOST_VERSION_DEVICE` and `SET_ESSENTIAL_SPACE_STATUS`. They collide with valid EQ commands.

## 4. PacketBuilder Cleanup
- File: `NothingX/Protocol/PacketBuilder.cs`
- Task: Remove unused builder methods (`BuildSet(int, byte)`, `BuildSet(int, byte[])`, `BuildSetBassEnhancer`, `BuildSetSystemAudio`).

## 5. EQ Serialization
- Files: `NothingX/Models/CustomEq.cs`, `NothingX/Models/SimpleEq.cs`
- Task: Move the 13-byte band encoding/decoding logic into `EqBand` to avoid duplicating serialization logic across EQ types.

## 6. View Dispatch & Math Extraction
- Files: `NothingX/ViewModels/MainViewModel.cs`, `NothingX/Controls/CircularEqControl.xaml.cs`
- Task: Create a `DeviceView` enum for XAML visibility converters instead of using strings. Extract repetitive angle calculations from the circular EQ drag handlers.

## 7. Packet Parser Consts
- Files: `NothingX/Protocol/PacketParser.cs`, `NothingX/Protocol/NothingPacket.cs`
- Task: Expose `NothingPacket.MASK_CRC` as `internal` and use it in `PacketParser.cs` instead of the hardcoded `0x20` value.
