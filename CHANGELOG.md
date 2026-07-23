# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]
### Added
- **Window State Persistence:** The application now remembers its exact window position and size between restarts. A new JSON-based `AppSettings` config handles saving and loading the `Top`, `Left`, `Width`, `Height`, and `WindowState` properties on app startup and exit. 

### Fixed
- **Dual Connection List Bug:** Fixed a critical bug in `NothingProtocol.cs` where the `GetDualDeviceListAsync()` parser was silently failing. The earbuds compress long Bluetooth device names by setting the Most Significant Bit (MSB) on the length byte. The parser wasn't masking this bit out (`& 0x7F`), which caused the extracted length to overflow the packet boundaries and abort parsing entirely. Multi-device tracking now populates reliably.

### Changed
- **UI Button Unification:** Refactored XAML styles to unify the visual language of all action buttons. Stripped out the mixed red/white default themes and consolidated everything under a single, maintainable "Nothing Pill" style (solid white background, black text, turning red on hover).
