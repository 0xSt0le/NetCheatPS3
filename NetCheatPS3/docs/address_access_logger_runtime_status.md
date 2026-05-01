# Address Access Logger Runtime Status

The TMAPI address-access logger proof has confirmed that `SetDABR` can arm a DABR watchpoint on the target.

Runtime testing has also confirmed that `EnableAutoStatusUpdate` and `RegisterTargetEventHandler` return success. Earlier testing did not deliver `PPUExcDabrMatch` events to NetCheatPS3, but that was before the logger pumped TMAPI target events.

TMAPI-E documentation says `SNPS3RegisterTargetEventHandler` callbacks are invoked when `SNPS3Kick()` is called. SDK 370+ requires the kick to happen on the same thread that registered the callback. The logger now uses one dedicated worker thread for target comms initialization, auto-status setup, callback registration, DABR arming, `SNPS3Kick` pumping, event cancellation, DABR restore/clear, and auto-status restore.

Thread/process resume from polling was disabled because guessing from stopped PPU thread state can hard-hang the target. Polling stopped threads must not be used as an auto-resume mechanism, and polling code must not call `ThreadExceptionClean`, `ThreadContinue`, or `ProcessContinue`.

Runtime testing later proved that `PPUExcDabrMatch` events can be delivered and parsed correctly when `SNPS3Kick` is pumped on the callback registration thread. The logger can capture the writer PC and opcode bytes, for example `0013CDEC - 93C30058`.

`ThreadExceptionClean` has been removed from the DABR logger resume path. TMAPI documentation says it clears the exception state of a thread and causes it to exit, and runtime testing showed the DABR-hit thread disappearing after this call. The current resume model is:

1. Log the hit.
2. Clear DABR with `SetDABR(0)`.
3. Request process-level resume with `ProcessContinue`.
4. Re-arm the original DABR after a 250 ms delay if the session is still running.

The logger must not call `ThreadExceptionClean` or `ThreadContinue` for CE-style address-access logging unless a separate, explicitly guarded experiment proves a safe use.

The public result-list menu actions are enabled only when the active API implements the TMAPI address-access logger backend. PS2RD does not expose this feature.

If DABR event delivery still proves unreliable, the next correct research target is `SNPS3SetHWBreakPointData` or the related official ProDG debug exception API used by the debugger, not thread-state guessing.
