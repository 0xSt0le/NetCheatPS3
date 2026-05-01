# Address Access Logger Runtime Status

## Working v1: TMAPI Write Logger

The TMAPI "Find out what Writes To this address" logger is working as a v1 proof.

Runtime evidence:

- A real DABR write hit was captured at PC `0x0013CDEC`.
- The captured opcode bytes were `93C30058`.
- Repeated hits at the same PC increment the existing row's `Count`.
- The target resumes immediately and remains usable.
- DABR remains armed across hits and continues logging later writes.
- TMAPI documentation says `SNPS3SetDABR` requires all PPU threads stopped, so DABR is only set/cleared at logger start/stop.

The current golden sequence is:

Startup:

1. `ProcessStop`.
2. `SetDABR(rawDabr)` while stopped.
3. `ProcessContinue`.

Hit:

1. `SNPS3Kick` delivers `PPUExcDabrMatch`.
2. The callback parses/copies the event data and queues the hit only.
3. The logger worker returns from the callback.
4. Outside the callback, the worker tries `ThreadContinue(hit.ThreadId)` first.
5. If thread-level resume fails, the worker falls back to `ProcessContinue` once.
6. DABR remains armed.

Stop:

1. `ProcessStop`.
2. `SetDABR(oldDabr)` if saved, otherwise `SetDABR(0)`, while stopped.
3. `ProcessContinue`.

The previous re-arm-on-every-hit model caused hot-writer blind windows and intermittent freezes because it repeatedly did clear/continue/delay/stop/set/continue while writes were still arriving.

Hot writer handling:

- DABR remains armed across hits; the logger no longer clears/re-arms the watchpoint for each hit.
- The TMAPI callback only parses/copies the event and appends it to a small pending-hit queue.
- The worker requests resume before reading opcode bytes or publishing the UI row, keeping the target stopped for the shortest practical time.
- Per-hit resume currently tries `ThreadContinue(hit.ThreadId)` first. This is a guarded runtime test because ProDG manual continue alternates writer PCs on hot addresses, while the previous `ProcessContinue`-only path only delivered `0x0035E7C0` in NetCheatPS3's callback histogram. `ProcessContinue` remains the single fallback.
- Instruction bytes are cached by PC so repeated hits at the same writer instruction do not reread code memory.
- Pending hits are aggregated by unique PC so one hot writer cannot fill the queue with duplicates and starve a different writer PC.
- Repeated same-PC callbacks are coalesced, and the UI count may be batched with `CountDelta` under heavy load.
- A bounded pending-PC queue reduces missed different writer PCs while the worker is busy. If full, the oldest pending PC aggregate is dropped.
- A verbose callback PC histogram logs the first time each new PC is observed. If a PC never appears there, TMAPI/DABR did not deliver it to NetCheatPS3 during that session.
- New callback PCs are now also surfaced as normal logger status text and written to `NetCheatPS3_dabr_logger.log` with watched address, mode, PC, thread, hardware thread, SP, and timestamp.
- On logger stop, `NetCheatPS3_dabr_logger.log` receives a callback PC histogram summary for the watched address.
- Ultra-hot addresses can still stutter because every access triggers a debug exception. The logger is intended to identify writers, not to run indefinitely on a very hot address; stop it once the relevant writer PCs are captured.

Forbidden approaches:

- Do not call target-control APIs from inside the TMAPI target-event callback.
- Do not use stopped-thread polling as a resume mechanism.
- Do not call `ThreadExceptionClean` for CE-style write logging; TMAPI documentation says it causes the thread to exit, and runtime testing confirmed the hit thread disappeared.
- Do not call `ThreadContinue` from polling or guessed thread state. The only permitted thread-level resume experiment is the real `PPUExcDabrMatch` thread id delivered by TMAPI, with `ProcessContinue` as fallback.
- Do not implement read logging until the write path remains stable.

The TMAPI address-access logger proof has confirmed that `SetDABR` can arm a DABR watchpoint on the target.

Runtime testing has also confirmed that `EnableAutoStatusUpdate` and `RegisterTargetEventHandler` return success. Earlier testing did not deliver `PPUExcDabrMatch` events to NetCheatPS3, but that was before the logger pumped TMAPI target events.

TMAPI-E documentation says `SNPS3RegisterTargetEventHandler` callbacks are invoked when `SNPS3Kick()` is called. SDK 370+ requires the kick to happen on the same thread that registered the callback. The logger now uses one dedicated worker thread for target comms initialization, auto-status setup, callback registration, DABR arming, `SNPS3Kick` pumping, event cancellation, DABR restore/clear, and auto-status restore.

Thread/process resume from polling was disabled because guessing from stopped PPU thread state can hard-hang the target. Polling stopped threads must not be used as an auto-resume mechanism, and polling code must not call `ThreadExceptionClean`, `ThreadContinue`, or `ProcessContinue`.

Runtime testing later proved that `PPUExcDabrMatch` events can be delivered and parsed correctly when `SNPS3Kick` is pumped on the callback registration thread. The logger can capture the writer PC and opcode bytes, for example `0013CDEC - 93C30058`.

`ThreadExceptionClean` has been removed from the DABR logger resume path. TMAPI documentation says it clears the exception state of a thread and causes it to exit, and runtime testing showed the DABR-hit thread disappearing after this call. The current guarded hit resume model is: queue the real DABR-hit thread id from `PPUExcDabrMatch`, request `ThreadContinue(hit.ThreadId)` outside the callback, fall back to `ProcessContinue` once if needed, and leave DABR armed.

The logger must not call `ThreadExceptionClean` for CE-style address-access logging. It must also not call `ThreadContinue` for any guessed or polled thread id.

The public result-list menu actions are enabled only when the active API implements the TMAPI address-access logger backend. PS2RD does not expose this feature.

If DABR event delivery still proves unreliable, the next correct research target is `SNPS3SetHWBreakPointData` or the related official ProDG debug exception API used by the debugger, not thread-state guessing.
