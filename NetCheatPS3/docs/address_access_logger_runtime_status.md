# Address Access Logger Runtime Status

The TMAPI address-access logger proof has confirmed that `SetDABR` can arm a DABR watchpoint on the target.

Runtime testing has also confirmed that `EnableAutoStatusUpdate` and `RegisterTargetEventHandler` return success. Earlier testing did not deliver `PPUExcDabrMatch` events to NetCheatPS3, but that was before the logger pumped TMAPI target events.

TMAPI-E documentation says `SNPS3RegisterTargetEventHandler` callbacks are invoked when `SNPS3Kick()` is called. The logger now runs a background `SNPS3Kick` event pump while DABR is armed.

Thread/process resume from polling was disabled because guessing from stopped PPU thread state can hard-hang the target. Polling stopped threads must not be used as an auto-resume mechanism, and polling code must not call `ThreadExceptionClean`, `ThreadContinue`, or `ProcessContinue`.

The public result-list menu actions are enabled only when the active API implements the TMAPI address-access logger backend. PS2RD does not expose this feature.

If DABR event delivery still proves unreliable, the next correct research target is `SNPS3SetHWBreakPointData` or the related official ProDG debug exception API used by the debugger, not thread-state guessing.
