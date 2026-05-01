# Address Access Logger Runtime Status

The TMAPI address-access logger proof has confirmed that `SetDABR` can arm a DABR watchpoint on the target.

Runtime testing has also confirmed that `EnableAutoStatusUpdate` and `RegisterTargetEventHandler` return success. However, the managed TMAPI target callback path has not delivered `PPUExcDabrMatch` events to NetCheatPS3 when the target stops on the DABR hit.

Thread/process resume from polling was disabled because guessing from stopped PPU thread state can hard-hang the target. Polling stopped threads must not be used as an auto-resume mechanism, and polling code must not call `ThreadExceptionClean`, `ThreadContinue`, or `ProcessContinue`.

The public result-list menu actions are disabled at runtime. They show a safe message and do not start `AddressAccessLoggerForm` until the backend is proven.

The feature stays disabled until an exact official hardware-breakpoint/debug-event API is identified and wrapped. The next correct research target is `SNPS3SetHWBreakPointData` or the related official ProDG debug exception API used by the debugger, not thread-state guessing.
