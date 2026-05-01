# TMAPI Hot-PC Step-Over Audit

Audit date: 2026-05-01

Goal: determine whether NetCheatPS3 can safely step over a dominant DABR writer PC by temporarily clearing DABR, setting a software breakpoint at `PC + 4`, continuing, handling the breakpoint/trap event, clearing the breakpoint, and restoring DABR.

## Runtime Context

- The stable DABR logger lifecycle remains:
  - Start: `ProcessStop -> SetDABR(raw) while stopped -> ProcessContinue`
  - Hit: callback parses/copies/queues only, worker calls `ProcessContinue`
  - Stop: `ProcessStop -> SetDABR(old/0) while stopped -> ProcessContinue`
- `ThreadContinue(hit.ThreadId)` was tested after real DABR hits and returned `SN_S_OK`, but the callback histogram still only showed the dominant writer PC `0x0035E7C0`.
- `ThreadExceptionClean` remains forbidden because the TMAPI reference says it causes the thread to exit, and runtime testing showed the DABR-hit thread disappearing.

## Verified Local Evidence

### SDK Header

Source: `C:\FAST_Apps\SN Systems\PS3\sdk\include\ps3tmapi.h`

- `SNPS3_DBG_EVENT_PPU_EXP_TRAP` is defined as `0x00000010`.
- `SNPS3_PPU_EXP_TRAP_DATA` is documented with:
  - `UINT64 uPPUThreadID`
  - `UINT32 uHWThreadNumber`
  - `UINT64 uPC`
  - `UINT64 uSP`
- `SNPS3SetBreakPoint` is declared as:
  - `SNAPI SNRESULT SNPS3SetBreakPoint(HTARGET hTarget, UINT32 uUnit, UINT32 uProcessID, UINT64 uThreadID, UINT64 uAddress);`
- `SNPS3ClearBreakPoint` is declared as:
  - `SNAPI SNRESULT SNPS3ClearBreakPoint(HTARGET hTarget, UINT32 uUnit, UINT32 uProcessID, UINT64 uThreadID, UINT64 uAddress);`
- `SNPS3Kick` documentation says callbacks are invoked only when `SNPS3Kick()` is called, and SDK 370+ only kicks callbacks registered in that same thread.

### Managed Metadata

Source: `NetCheatPS3/docs/tmapi_managed_methods.txt`

- `PS3TMAPI.SetBreakPoint(int target, PS3TMAPI.UnitType unit, uint processID, ulong threadID, ulong address)`
- `PS3TMAPI.ClearBreakPoint(int target, PS3TMAPI.UnitType unit, uint processID, ulong threadID, ulong address)`
- `PS3TMAPI.TargetSpecificEventType.PPUExcTrap = 16`

### Existing Parser Compatibility

`TMAPI-NCAPI/TMAPI_NET.cs` already parses PPU exception payloads sequentially as big-endian values. `PPUExcTrap` falls inside the same `IsPPUExceptionEvent` range as `PPUExcDabrMatch`, so the current parser shape appears compatible with the header's trap payload.

## Blockers

The step-over experiment was not implemented because the hard debugger-control rule requires the exact API signature, enum, struct/event layout, callback rules, and required target/thread state to be verified from `TMAPI-E.pdf` or already-proven wrapper code before adding TMAPI target-control behavior.

The local source tree currently has these gaps:

- `TMAPI-NCAPI/TMAPI_NET.cs` does not yet contain proven Set/ClearBreakPoint wrappers.
- The exact `TMAPI-E.pdf` breakpoint preconditions were not verified in this change. The SDK header confirms signatures but does not prove all required stopped/running state transitions for this step-over flow.
- It is not yet runtime-proven that `SNPS3SetBreakPoint` at `PC + 4` will emit `PPUExcTrap` through the same pumped target-event path for this DABR logger session.
- The exact cleanup semantics after a trap hit must be validated before code can safely call `ClearBreakPoint`, restore DABR, and continue the process.

## Decision

Do not implement the hot-PC step-over experiment in this commit.

The safe change is to restore the stable ProcessContinue-only DABR hit resume path and document the required breakpoint/trap evidence. A future implementation should first add compile-only Set/ClearBreakPoint wrappers from the official managed/native signatures, then perform a small isolated runtime test that proves:

1. `SetBreakPoint(UnitType.PPU, processId, threadId/address semantics, PC + 4)` succeeds under documented target state.
2. `SNPS3Kick` delivers a `PPUExcTrap` event for that breakpoint.
3. The trap payload PC/thread/SP parse correctly.
4. `ClearBreakPoint` cleanup is safe.
5. `ProcessContinue` after breakpoint cleanup leaves the target usable.
