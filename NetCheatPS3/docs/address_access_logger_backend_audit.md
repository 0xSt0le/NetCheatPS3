# Address Access Logger Backend Audit

## Scope

This audit checks whether the current local NetCheatPS3 repository and the locally installed TMAPI stack expose enough debugger support to implement Cheat Engine-style address access logging:

- write logger: log instructions that write to a selected address or range
- read logger: log instructions that read from a selected address or range
- no combined read/write logger in the first implementation

The intended backend must use a real debugger data breakpoint/watchpoint. Memory polling is not an acceptable substitute because it cannot identify the instruction pointer or register context that accessed the address.

## Current app API capabilities

The shared API interface in `NCAppInterface/NCAppInterface.cs` exposes only the normal NetCheat memory and process lifecycle surface:

- `bool GetBytes(ulong address, ref byte[] bytes)`
- `void SetBytes(ulong address, byte[] bytes)`
- `bool Connect()`
- `void Disconnect()`
- `bool Attach()`
- `bool PauseProcess()`
- `bool ContinueProcess()`
- `bool isProcessStopped()`

There is no API contract for:

- setting or clearing code breakpoints
- setting or clearing data/watch breakpoints
- selecting read vs write watchpoint mode
- receiving debugger exception/breakpoint events
- retrieving PPU register context
- retrieving the current instruction pointer/NIP/PC
- single-stepping or resuming a specific stopped thread after a breakpoint

`SearchListView.ModernResultUi.cs` already has the selected result address available for existing result actions, so the UI could supply a target address later. That does not prove backend support.

## Current TMAPI provider capabilities

The local TMAPI provider consists of:

- `TMAPI-NCAPI/API.cs`
- `TMAPI-NCAPI/TMAPI.cs`
- `TMAPI-NCAPI/TMAPI_NET.cs`

The local wrapper exposes these relevant native TMAPI calls:

- `SNPS3ProcessAttach`
  - local wrapper: `PS3TMAPI.ProcessAttach(int target, UnitType unit, uint processID)`
- `SNPS3ProcessContinue`
  - local wrapper: `PS3TMAPI.ProcessContinue(int target, uint processID)`
- `SNPS3ProcessGetMemory`
  - local wrapper: `PS3TMAPI.ProcessGetMemory(int target, UnitType unit, uint processID, ulong threadID, ulong address, ref byte[] buffer)`
- `SNPS3ProcessSetMemory`
  - local wrapper: `PS3TMAPI.ProcessSetMemory(int target, UnitType unit, uint processID, ulong threadID, ulong address, byte[] buffer)`
- `SNPS3ThreadList`
  - local wrapper: `PS3TMAPI.GetThreadList(int target, uint processID, out ulong[] ppuThreadIDs, out ulong[] spuThreadIDs)`
- `SNPS3PPUThreadInfoEx`
  - local wrapper: `PS3TMAPI.GetPPUThreadInfo(int target, uint processID, ulong threadID, out PPUThreadInfo threadInfo)`
- `SNPS3GetProcessInfo`
  - imported in `TMAPI_NET.cs`, but not exposed by a public wrapper in this repo.

`PPUThreadInfo` currently includes only:

- thread id
- priority
- state
- stack address
- stack size
- thread name

It does not expose NIP/PC, LR, CTR, GPRs, CR, XER, or other register state needed for Cheat Engine-style "Extra Info".

## Installed/local TMAPI binaries checked

The requested ProDG paths were checked:

- `C:\Program Files (x86)\SN Systems\PS3\bin\PS3TMAPI.dll`
- `C:\Program Files\SN Systems\PS3\bin\PS3TMAPI.dll`
- `C:\Program Files (x86)\SN Systems\PS3\bin\PS3TMAPIX64.dll`
- `C:\Program Files\SN Systems\PS3\bin\PS3TMAPIX64.dll`
- `C:\Program Files (x86)\SN Systems\PS3\bin\ps3tmapi_net.dll`
- `C:\Program Files\SN Systems\PS3\bin\ps3tmapi_net.dll`

None of those files were present on this machine.

A broader Program Files search for `PS3TMAPI*.dll` and `ps3tmapi_net.dll` also found no local TMAPI DLLs. `dumpbin.exe` is available from VS2022, but there was no local TMAPI DLL to inspect. No managed `ps3tmapi_net.dll` was available for reflection.

The only local DLL found by a repo search was `NCMemBrowser/Be.Windows.Forms.HexBox.dll`, which is unrelated.

## Debug/watchpoint APIs found

No local source wrapper, local managed DLL, or installed native DLL inspection exposed any of the required primitives:

- no data breakpoint/watchpoint setter
- no read-watchpoint mode
- no write-watchpoint mode
- no breakpoint remove/clear API
- no debug event callback or event polling API
- no stopped-thread exception reason API
- no register context API
- no instruction pointer/NIP/PC API
- no hardware DABR-specific API

Exact TMAPI names/signatures for those capabilities were not discoverable from the installed/local artifacts. They should not be guessed.

## Feasibility assessment

### Write watchpoint

Not proven feasible with the current local stack.

The current wrapper can write memory and continue a process, but it cannot set a data breakpoint that traps writes to an address. A write logger should not be implemented until an official TMAPI function for data write breakpoints/watchpoints and debug event delivery is confirmed.

### Read watchpoint

Not proven feasible with the current local stack.

Read watchpoints usually require explicit hardware data breakpoint support and a mode bit distinguishing reads from writes. No such API is present in the local wrapper or discoverable installed files.

### Register context

Not proven feasible with the current local stack.

The current `PPUThreadInfo` wrapper gives only basic thread metadata. It does not include NIP/PC or register values. Extra Info cannot be implemented professionally without a confirmed thread context/register API.

### Instruction bytes near PC

Possible only after PC/NIP is known.

The existing TMAPI memory read path can read process memory via `SNPS3ProcessGetMemory`, so reading instruction bytes around a captured PC should be straightforward once a real breakpoint event supplies the stopped thread and PC. The current stack cannot supply that PC.

## Recommended implementation path

1. Obtain/verify official PS3 TMAPI SDK headers, documentation, or installed DLL exports on a developer machine that has ProDG Target Manager installed.
2. Confirm exact APIs for:
   - setting data breakpoints/watchpoints
   - selecting read vs write access mode
   - clearing breakpoints/watchpoints
   - receiving process/thread debug events
   - retrieving stopped thread register context
   - continuing from a breakpoint without destabilizing the target
3. Add a TMAPI-only backend interface behind the provider, not the generic `IAPI`, until PS2RD support is intentionally designed.
4. Keep the first backend implementation headless and testable:
   - start write watch on address/range
   - stop watch
   - log hit count, thread id, PC/NIP, and raw instruction bytes
   - optionally capture registers if the exact context API is confirmed
5. Only after backend proof, add SearchListView context-menu UI for:
   - "Find What Writes To This Address"
   - "Find What Reads From This Address"

## Hard blockers

- No local TMAPI DLLs were available for export inspection.
- The current source wrapper does not expose breakpoint/watchpoint APIs.
- No exact native signatures for data breakpoints, debug events, or register context were discoverable.
- Implementing this with polling would be misleading and must be avoided.

## Conclusion

Backend support is not proven with the current installed/local artifacts. The feature should not be implemented yet. The next safe step is to verify official TMAPI debugger/watchpoint signatures on a machine with the full ProDG Target Manager SDK installed, then add a narrow TMAPI backend proof before any UI work.

## Re-audit using local ProDG DLLs at C:\FAST_Apps\SN Systems\PS3\bin

### DLLs inspected

The following real local ProDG DLLs were inspected on 2026-04-29:

- `C:\FAST_Apps\SN Systems\PS3\bin\ps3tmapi.dll`
- `C:\FAST_Apps\SN Systems\PS3\bin\ps3tmapix64.dll`
- `C:\FAST_Apps\SN Systems\PS3\bin\ps3tmapi_net.dll`

Native export dumps were saved to:

- `NetCheatPS3/docs/tmapi_native_exports_x86.txt`
- `NetCheatPS3/docs/tmapi_native_exports_x64.txt`

Managed reflection output was saved to:

- `NetCheatPS3/docs/tmapi_managed_methods.txt`

### Native export findings

Both native DLLs expose debugger-related exports that were not present in the repository's trimmed `TMAPI-NCAPI/TMAPI_NET.cs` wrapper:

- `SNPS3SetDABR`
- `SNPS3GetDABR`
- `SNPS3SetHWBreakPointData`
- `SNPS3SetBreakPoint`
- `SNPS3ClearBreakPoint`
- `SNPS3GetBreakPoints`
- `SNPS3SetAndWaitBreakPoint`
- `SNPS3SetBreakPointThread`
- `SNPS3ThreadGetRegisters`
- `SNPS3ThreadSetRegisters`
- `SNPS3ThreadStop`
- `SNPS3ThreadContinue`
- `SNPS3ThreadExceptionClean`
- `SNPS3ProcessStop`
- `SNPS3ProcessContinue`
- `SNPS3RegisterTargetEventHandler`
- `SNPS3CancelTargetEvents`
- `SNPS3GetDebugThreadControlInfo`
- `SNPS3SetDebugThreadControlInfo`

The exports strongly suggest that TMAPI has debugger support for PPU breakpoints, data address breakpoints, target-specific exception events, and register capture.

Native signatures were not inferred from the export table alone. The managed DLL provides reliable signatures for many, but not all, of these APIs.

### Managed method findings

`ps3tmapi_net.dll` is `PS3TMAPI_NET, Version=470.1.3.7`.

Reflection found these exact public managed signatures:

- `PS3TMAPI.SNRESULT SetDABR(int target, uint processID, ulong address)`
- `PS3TMAPI.SNRESULT GetDABR(int target, uint processID, ref ulong address)`
- `PS3TMAPI.SNRESULT SetBreakPoint(int target, PS3TMAPI.UnitType unit, uint processID, ulong threadID, ulong address)`
- `PS3TMAPI.SNRESULT ClearBreakPoint(int target, PS3TMAPI.UnitType unit, uint processID, ulong threadID, ulong address)`
- `PS3TMAPI.SNRESULT GetBreakPoints(int target, PS3TMAPI.UnitType unit, uint processID, ulong threadID, ref ulong[] bpAddresses)`
- `PS3TMAPI.SNRESULT ThreadGetRegisters(int target, PS3TMAPI.UnitType unit, uint processID, ulong threadID, uint[] registerNums, ref ulong[] registerValues)`
- `PS3TMAPI.SNRESULT ThreadSetRegisters(int target, PS3TMAPI.UnitType unit, uint processID, ulong threadID, uint[] registerNums, ulong[] registerValues)`
- `PS3TMAPI.SNRESULT ThreadStop(int target, PS3TMAPI.UnitType unit, uint processID, ulong threadID)`
- `PS3TMAPI.SNRESULT ThreadContinue(int target, PS3TMAPI.UnitType unit, uint processID, ulong threadID)`
- `PS3TMAPI.SNRESULT ThreadExceptionClean(int target, uint processID, ulong threadID)`
- `PS3TMAPI.SNRESULT ProcessStop(int target, uint processID)`
- `PS3TMAPI.SNRESULT ProcessContinue(int target, uint processID)`
- `PS3TMAPI.SNRESULT RegisterTargetEventHandler(int target, PS3TMAPI.TargetEventCallback callback, ref object userData)`
- `PS3TMAPI.SNRESULT CancelTargetEvents(int target)`
- `PS3TMAPI.SNRESULT GetDebugThreadControlInfo(int target, uint processID, ref PS3TMAPI.DebugThreadControlInfo threadCtrlInfo)`
- `PS3TMAPI.SNRESULT SetDebugThreadControlInfo(int target, uint processID, PS3TMAPI.DebugThreadControlInfo threadCtrlInfo, ref uint maxEntries)`

Important reflected event/context types:

- `PS3TMAPI.TargetEventCallback`
  - callback shape: `void Invoke(int target, PS3TMAPI.SNRESULT res, PS3TMAPI.TargetEvent[] targetEventList, object userData)`
- `PS3TMAPI.TargetSpecificEventType`
  - includes `PPUExcDabrMatch = 25`
  - includes `PPUExcDataMAT = 28`
  - includes several other PPU exception event types.
- `PS3TMAPI.TargetSpecificData`
  - includes `PPUException`
  - includes `PPUAlignmentException`
  - includes `PPUDataMatException`
- `PS3TMAPI.PPUExceptionData`
  - fields: `ThreadID`, `HWThreadNumber`, `PC`, `SP`
- `PS3TMAPI.PPUDataMatExceptionData`
  - fields: `ThreadID`, `HWThreadNumber`, `DSISR`, `DAR`, `PC`, `SP`
- `PS3TMAPI.PPUAlignmentExceptionData`
  - fields: `ThreadID`, `HWThreadNumber`, `DSISR`, `DAR`, `PC`, `SP`

The managed DLL also exposes private P/Invoke wrappers for `SetDABR`, `GetDABR`, breakpoint management, target events, thread register access, and thread/process continue/stop methods. This confirms the public managed APIs are backed by native TMAPI exports.

### Comparison with current repository wrapper

The current checked-in `TMAPI-NCAPI/TMAPI_NET.cs` wrapper exposes only a small subset of the managed TMAPI surface:

- process attach/continue
- process get/set memory
- process list
- thread list
- basic PPU thread info
- target connect/disconnect/status

The current wrapper does not declare or expose:

- `SetDABR`
- `GetDABR`
- `ThreadGetRegisters`
- `ThreadSetRegisters`
- `ThreadStop`
- `ThreadContinue`
- `ThreadExceptionClean`
- `ProcessStop`
- `RegisterTargetEventHandler`
- `CancelTargetEvents`
- `TargetSpecificEventType`
- `TargetEvent` / `TargetSpecificEvent` data structures
- `PPUExceptionData`
- `PPUDataMatExceptionData`
- `DebugThreadControlInfo`

Therefore the DLLs appear more capable than the current source wrapper.

### Write watchpoint feasibility

Partially feasible, but not yet proven enough for implementation.

Evidence for feasibility:

- `SetDABR(int target, uint processID, ulong address)` exists.
- `GetDABR(int target, uint processID, ref ulong address)` exists.
- target events include `PPUExcDabrMatch`.
- exception data can include `ThreadID`, `PC`, and `SP`.
- thread register capture exists through `ThreadGetRegisters`.
- existing `ProcessGetMemory` can read instruction bytes near a captured `PC`.

Remaining uncertainty:

- The public managed `SetDABR` signature accepts only an address. It does not expose an explicit read/write/access mode.
- Native `SNPS3SetHWBreakPointData` exists, but no managed public signature for it was found in `ps3tmapi_net.dll`.
- Without official docs or a proven signature for `SNPS3SetHWBreakPointData`, we should not guess how to select write-only behavior.

Conclusion: a general DABR hit logger may be possible, but a true write-only logger is not proven yet.

### Read watchpoint feasibility

Partially feasible, but not yet proven enough for implementation.

Evidence is the same as write watchpoint: DABR and `PPUExcDabrMatch` exist. However, no confirmed read-only mode was found in the public managed API. A read-only logger requires either:

- documented DABR address/mode encoding, or
- a confirmed `SNPS3SetHWBreakPointData` signature that exposes read/write/access flags.

Conclusion: a general DABR hit logger may be possible, but a true read-only logger is not proven yet.

### Register context feasibility

Feasible enough for a backend proof.

The managed DLL exposes:

- `ThreadGetRegisters(int target, UnitType unit, uint processID, ulong threadID, uint[] registerNums, ref ulong[] registerValues)`

The exact register-number constants for NIP/PC, LR, CTR, CR, XER, and GPRs were not identified by name in the reflection output. However, target exception event data already includes `PC` and `SP`, so a minimal first proof can log PC/SP from the event and defer broad register capture until register IDs are confirmed from official headers/docs.

### Instruction bytes near NIP/PC

Feasible after a debug event supplies `PC`.

The existing memory read path can use `SNPS3ProcessGetMemory` / `GetBytes` to read PPU instruction bytes around `PC`. This should be done only after the target is stopped at the exception and before the thread/process is resumed.

### Required wrapper additions if feasible

Do not add these until the exact behavior has been tested on a target. The likely additions are:

1. Add a TMAPI-specific backend, not generic `IAPI`, because PS2RD does not expose equivalent debugger/watchpoint semantics.
2. In `TMAPI-NCAPI/TMAPI_NET.cs`, add the missing managed API declarations by matching `ps3tmapi_net.dll` signatures:
   - `SetDABR`
   - `GetDABR`
   - `RegisterTargetEventHandler`
   - `CancelTargetEvents`
   - target event data types
   - `ThreadGetRegisters`
   - `ThreadExceptionClean`
   - `ThreadContinue`
   - possibly `ProcessStop` / `ProcessContinue`
3. Add a headless TMAPI address-access logger service that can:
   - attach/confirm process
   - set DABR on one address
   - receive `PPUExcDabrMatch`
   - log process id, thread id, PC, SP, hit count, and instruction bytes near PC
   - clean the exception with `ThreadExceptionClean`
   - resume with `ThreadContinue` or `ProcessContinue`
   - clear DABR on stop
4. Only after this proof, add UI context-menu entries.

### Hard blockers and open questions

- No confirmed read/write mode selection was found in the public managed `SetDABR` signature.
- Native `SNPS3SetHWBreakPointData` exists, but its signature and semantics are not confirmed from metadata.
- It is not yet confirmed whether DABR fires on read, write, or both by default.
- It is not yet confirmed which `TargetSpecificData` union field is populated for `PPUExcDabrMatch`.
- Register-number constants for `ThreadGetRegisters` were not identified in reflection output.
- The repo currently uses an older/truncated local TMAPI wrapper. Copying in signatures should be done carefully from reflected metadata, not guessed.

### Recommended next implementation step

Create a TMAPI-only, headless backend proof before UI work:

1. Add compile-only wrappers for the exact public managed signatures confirmed above.
2. Add a small internal service that sets DABR and subscribes to target events.
3. Test against a real attached target with a single known address.
4. Confirm whether `SetDABR` traps reads, writes, or both.
5. Confirm whether `ThreadExceptionClean` plus `ThreadContinue` resumes cleanly.
6. Confirm which event data field carries `PC`, `SP`, and thread id for `PPUExcDabrMatch`.
7. Only then decide whether the app can truthfully offer separate "writes to" and "reads from" modes.

Until those runtime behaviors are confirmed, the UI should not be implemented.
