# TMAPI Hardware Breakpoint Audit

Audit date: 2026-05-01

Local ProDG root searched:

`C:\FAST_Apps\SN Systems\PS3`

Search terms:

`SNPS3SetHWBreakPointData`, `SetHWBreakPointData`, `HWBreakPoint`, `HardwareBreak`, `WatchPoint`, `DataBreak`, `DABR`, `BreakPointData`, `ExceptionClean`, `ThreadException`

File types searched:

`*.h`, `*.hpp`, `*.cs`, `*.txt`, `*.xml`, `*.html`, `*.htm`, `*.lib`, `*.def`

## Summary

`SNPS3SetHWBreakPointData` is exported by both x86 and x64 TMAPI binaries and appears in both SDK import libraries, but no official textual declaration/signature was found in the searched SDK headers or docs. Because the parameter list and data structure layout are not proven, NetCheatPS3 must not add a P/Invoke wrapper for this function yet.

The SDK header does document the older DABR API and the target-specific DABR match event payload:

- `SNPS3SetDABR(HTARGET hTarget, UINT32 uProcessID, UINT64 uAddr)`
- `SNPS3GetDABR(HTARGET hTarget, UINT32 uProcessID, UINT64 *puAddr)`
- `SNPS3_PPU_EXP_DABR_MATCH_DATA`
- `SNPS3ThreadExceptionClean(HTARGET hTarget, UINT32 uPID, UINT64 uTID)`

Runtime testing has shown that `SetDABR` can stop the game with a `DABR_MATCH` in ProDG, but NetCheatPS3 does not receive the managed target callback for that stop. Polling stopped threads was unsafe and is disabled.

## Relevant Matches

### Textual SDK header

`C:\FAST_Apps\SN Systems\PS3\sdk\include\ps3tmapi.h`

- Line 595: DABR event comment: address set as DABR was accessed.
- Line 596: `#define SNPS3_DBG_EVENT_PPU_EXP_DABR_MATCH (0x00000019)`
- Line 819: `SNPS3_PPU_EXP_DABR_MATCH_DATA` documentation block.
- Lines 823-829: DABR event payload:

```c
struct SNPS3_PPU_EXP_DABR_MATCH_DATA {
    UINT64 uPPUThreadID;
    UINT32 uHWThreadNumber;
    UINT64 uPC;
    UINT64 uSP;
};
typedef struct SNPS3_PPU_EXP_DABR_MATCH_DATA SNPS3_PPU_EXP_DABR_MATCH_DATA;
```

- Line 1076: target-specific event union contains `ppu_exc_dabr_match`.
- Line 5867: `SNAPI SNRESULT SNPS3SetDABR(HTARGET hTarget, UINT32 uProcessID, UINT64 uAddr);`
- Line 5892: `SNAPI SNRESULT SNPS3GetDABR(HTARGET hTarget, UINT32 uProcessID, UINT64 *puAddr);`
- Line 5997: `SNAPI SNRESULT SNPS3SetBreakPoint(HTARGET hTarget, UINT32 uUnit, UINT32 uProcessID, UINT64 uThreadID, UINT64 uAddress);`
- Line 6022: `SNAPI SNRESULT SNPS3ClearBreakPoint(HTARGET hTarget, UINT32 uUnit, UINT32 uProcessID, UINT64 uThreadID, UINT64 uAddress);`
- Line 6056: `SNAPI SNRESULT SNPS3GetBreakPoints(HTARGET hTarget, UINT32 uUnit, UINT32 uPID, UINT64 u64TID, UINT32 *puBPCount, UINT64 *au64BPAddress);`
- Line 7013: `SNAPI SNRESULT SNPS3ThreadExceptionClean(HTARGET hTarget, UINT32 uPID, UINT64 uTID);`

No textual declaration for `SNPS3SetHWBreakPointData` was found in this header or other searched textual SDK files.

### Import libraries

`C:\FAST_Apps\SN Systems\PS3\sdk\lib\PS3TMAPI.lib`

- Contains import symbols for `_SNPS3SetHWBreakPointData`, `_SNPS3SetDABR`, `_SNPS3GetDABR`, `_SNPS3ThreadExceptionClean`, `_SNPS3SetBreakPoint`, `_SNPS3ClearBreakPoint`, and `_SNPS3GetBreakPoints`.

`C:\FAST_Apps\SN Systems\PS3\sdk\lib\PS3TMAPIx64.lib`

- Contains import symbols for `SNPS3SetHWBreakPointData`, `SNPS3SetDABR`, `SNPS3GetDABR`, `SNPS3ThreadExceptionClean`, `SNPS3SetBreakPoint`, `SNPS3ClearBreakPoint`, and `SNPS3GetBreakPoints`.

These import-library matches prove symbol presence but not the native function signature or structure layout.

## Existing Export Documents

`NetCheatPS3/docs/tmapi_native_exports_x86.txt`

- Line 262: `SNPS3SetHWBreakPointData` is exported.
- Line 251: `SNPS3SetDABR` is exported.
- Line 86: `SNPS3GetDABR` is exported.
- Line 286: `SNPS3ThreadExceptionClean` is exported.
- Lines 33, 78, 245, 246: `SNPS3ClearBreakPoint`, `SNPS3GetBreakPoints`, `SNPS3SetBreakPoint`, and `SNPS3SetBreakPointThread` are exported.

`NetCheatPS3/docs/tmapi_native_exports_x64.txt`

- Line 262: `SNPS3SetHWBreakPointData` is exported.
- Line 251: `SNPS3SetDABR` is exported.
- Line 86: `SNPS3GetDABR` is exported.
- Line 286: `SNPS3ThreadExceptionClean` is exported.
- Lines 33, 78, 245, 246: `SNPS3ClearBreakPoint`, `SNPS3GetBreakPoints`, `SNPS3SetBreakPoint`, and `SNPS3SetBreakPointThread` are exported.

## Read/Write/Access Mode Support

No official declaration, enum, flag table, or structure for `SNPS3SetHWBreakPointData` was found in the searched local SDK files. Read/write/access mode support is therefore not proven from the local textual SDK evidence.

The existing `SNPS3SetDABR` path accepts a raw DABR value, but runtime testing shows that managed target events are not delivered to NetCheatPS3 for DABR hits in the current backend.

## Clear/Remove API

Confirmed textual APIs:

- `SNPS3GetDABR` reads the current DABR value.
- `SNPS3SetDABR` can clear/replace the DABR by setting a new raw value.
- `SNPS3ClearBreakPoint` clears software breakpoints.

No confirmed clear/remove API specific to `SNPS3SetHWBreakPointData` was found.

## Event/Callback API

Confirmed textual event data:

- `SNPS3_DBG_EVENT_PPU_EXP_DABR_MATCH`
- `SNPS3_PPU_EXP_DABR_MATCH_DATA` with thread id, hardware thread number, PC, and SP.

The managed `ps3tmapi_net.dll` metadata currently documented in `tmapi_managed_methods.txt` includes `RegisterTargetEventHandler`, `TargetEventCallback`, and `TargetSpecificEventType.PPUExcDabrMatch`. Runtime testing has not shown those callbacks arriving for the DABR stop.

## Conclusion

`SNPS3SetHWBreakPointData` is a real exported native TMAPI symbol in both x86 and x64, but its exact official declaration was not found in the local SDK headers/docs. The function must remain audit-only until an official signature and data layout are located.

The public address-access logger UI is disabled because the current DABR path can stop the game but cannot reliably receive events or safely resume execution.
