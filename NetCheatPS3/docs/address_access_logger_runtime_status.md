# Address Access Logger Runtime Status

The TMAPI address-access logger proof has confirmed that `SetDABR` can arm a DABR watchpoint on the target.

The TMAPI target callback path is not yet proven to deliver `PPUExcDabrMatch` events to NetCheatPS3 during runtime testing. The logger keeps the official target-event callback registration and diagnostics, but auto-resume is currently limited to real DABR target events.

Thread/process resume from polling was disabled because guessing from stopped PPU thread state can hard-hang the target. The polling diagnostics worker must not call `ThreadExceptionClean`, `ThreadContinue`, or `ProcessContinue`.

The next correct research target is `SNPS3SetHWBreakPointData` or the exact ProDG debug exception API used by the debugger, not thread-state guessing.
