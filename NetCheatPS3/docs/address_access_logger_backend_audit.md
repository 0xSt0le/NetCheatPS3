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
