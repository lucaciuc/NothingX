# Nothing X Bluetooth Protocol Quirks

During the development of this Windows port, several quirks and undocumented behaviors of the Nothing Bluetooth protocol were discovered through packet logging and reverse engineering. This document serves to explain these behaviors for future contributors.

## 1. Push Notifications and the "Request" Bitmask
The official Nothing X app defines push notification events using a base of `0xE000` (e.g., `EVENT_NOISE_REDUCTION_LEVEL_CHANGED = 0xE003`).
However, when the earbuds actively push these events to the PC, they transmit the command with the Most Significant Bit (MSB, `0x8000`) cleared. 
- **Sent by Earbuds:** `0x6003` (MSB cleared)
- **Expected by App constants:** `0xE003` (MSB set)

**The Fix:** When parsing incoming packets, the application must treat these push notifications as "Requests" originating from the earbuds. By applying the `0x8000` bitmask (e.g., `packet.RequestCommand = Command | 0x8000`), `0x6003` is correctly translated back into `0xE003`, allowing it to accurately match the protocol's expected command constants.

## 2. Unsolicited Query Responses (Real-time Syncing)
Not all real-time state changes are pushed through the standard `0xE000` notification pipeline. Certain features, when toggled directly via the touch controls on the earbuds, will push unsolicited query responses instead of standard events.

For example, when a user toggles Low Latency (Game Mode) or Spatial Audio from the earbuds:
- **Low Latency Mode:** Earbuds spontaneously send `0x4041` (the response code for `GET_HOST_LAG_MODE = 0xC041`), usually with an `fsn` (Frame Sequence Number) of `255`.
- **Spatial Audio:** Earbuds spontaneously send `0x404F` (the response code for `GET_SPATIAL_AUDIO = 0xC04F`) with an `fsn` of `255`.

**The Fix:** The protocol parser must handle these unsolicited query responses. If a response arrives that does not match any pending requests from the host PC, it must be intercepted and treated as a push notification. Re-applying the request bit allows the app to update its local state models and sync the WPF UI in real-time.

## 3. Legacy vs Modern Bass Enhancer Commands
There is a discrepancy in how different Nothing/CMF models handle the "Ultra Bass" setting:
- **Modern Models:** Use `SET_BASS_ENHANCER_MODE (0xF057)` with a straightforward payload (e.g., `0x01` for on, followed by the raw level `0x01-0x05`).
- **Legacy / CMF Models:** Use a different command (`SET_BASS_ENHANCER = 0xF051`). In this mode, the payload scales the level in multiples of 5 (e.g., Level 1 = `0x05`, Level 2 = `0x0A`). 

The application must dynamically probe the device upon connection (e.g., via querying `GET_BASS_BOOST = 0xC04E`) to determine whether it requires legacy scaling and the legacy command structure.
