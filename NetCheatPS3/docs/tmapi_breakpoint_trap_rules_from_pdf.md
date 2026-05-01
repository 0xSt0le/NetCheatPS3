# TMAPI Breakpoint, Trap, Callback, and Continue Rules

Source: `C:\FAST_Apps\SN Systems\PS3\help\TMAPI-E.pdf`

Extraction date: 2026-05-01

This document records only rules that were found in `TMAPI-E.pdf`. It is intended to separate documented TMAPI behavior from runtime guesses before any further DABR logger or hot-PC step-over work.

## Callback Delivery

### `SNPS3RegisterTargetEventHandler`

PDF page 180:

- Registers a callback for target events such as process/thread create/destroy and exceptions.
- Signature:

```c
SNAPI SNRESULT SNPS3RegisterTargetEventHandler(
    HTARGET hTarget,
    TMAPI_HandleEventCallback pfnCallBack,
    void *pUserData
);
```

- The callback is only invoked when `SNPS3Kick()` is called.
- Callback data starts with `SN_EVENT_TARGET_HDR`.
- Target callback form:

```c
void Callback(
    HTARGET hTarget,
    UINT32 Type,
    UINT32 Param,
    UINT32 Length,
    SNRESULT eResult,
    BYTE* Data,
    void* UserData);
```

### `TMAPI_HandleEventCallback`

PDF pages 259 and 469:

- During operation, TMAPI calls this function for each event received from the target.
- It is necessary to call `SNPS3Kick()` to allow the API to enter its message loop and process pending messages.

### `SNPS3Kick`

PDF page 164:

- `SNPS3Kick()` kicks the event notification queue.
- When callbacks are registered, this function must be called to retrieve events and facilitate callback invocation.
- SDK 370+ behavior: calling `SNPS3Kick()` in a specific thread only kicks callbacks registered in that same thread.

Signature:

```c
SNAPI SNRESULT SNPS3Kick();
```

Return values documented on page 164 include:

- `SN_S_OK`: one or more events processed.
- `SN_S_NO_MSG`: no messages to process.
- `SN_E_TM_NOT_RUNNING`: server shutdown during call.
- `SN_E_DLL_NOT_INITIALISED`: target comms not initialized.

### `SNPS3CancelTargetEvents`

PDF page 25:

```c
SNAPI SNRESULT SNPS3CancelTargetEvents(
    HTARGET hTarget
);
```

Documented results:

- `SN_S_OK`: target events cancelled.
- `SN_S_NO_ACTION`: no callback registered.
- `SN_E_DLL_NOT_INITIALISED`: target comms not initialized.
- `SN_E_BAD_TARGET`: invalid target.

## Target Event Buffer Layout

### `SN_EVENT_TARGET_HDR`

PDF page 264:

```c
struct SN_EVENT_TARGET_HDR {
    UINT32 uSize;
    UINT32 uTargetID;
    UINT32 uEvent;
};
```

Rules:

- For `SN_EVENT_TARGET`, callback `Data` points to `SN_EVENT_TARGET_HDR`.
- Event-specific data follows this header.
- If `uSize` is less than the callback data length, more than one event is available in the buffer.
- Consumers should loop through the buffer and process each event in turn.

### Target-specific debug event layout

PDF page 271:

- When target event type is `SN_TGT_EVENT_TARGET_SPECIFIC`, `SN_EVENT_TARGET_HDR` is followed by:
  1. `SNPS3_DBG_EVENT_HDR`
  2. `SNPS3_DBG_EVENT_DATA`
- `SNPS3_DBG_EVENT_DATA.uEventType` determines how the rest of the data should be interpreted.

PDF page 272:

```c
struct SNPS3_DBG_EVENT_HDR {
    UINT32 uCommandID;
    UINT32 uRequestID;
    UINT32 uDataLength;
    UINT32 uProcessID;
    UINT32 uResult;
};
```

Notes from page 272:

- `uCommandID` always equals fixed value `DBGP_EVENT_NOTIFICATION`.
- `uRequestID` should be ignored.
- `uDataLength` is the debug data length in bytes.
- `uProcessID` is the process ID where relevant to the event.
- `uResult` is always zero and should be ignored.

### `SNPS3_DBG_EVENT_DATA`

PDF page 270:

```c
struct SNPS3_DBG_EVENT_DATA {
    UINT32 uEventType;
    SNPS3_PPU_PROCESS_CREATE_DATA ppu_process_create;
    SNPS3_PPU_PROCESS_EXIT_DATA ppu_process_exit;
    SNPS3_PPU_PROCESS_EXITSPAWN_DATA ppu_process_exitspawn;
    SNPS3_PPU_EXP_TRAP_DATA ppu_exc_trap;
    SNPS3_PPU_EXP_PREV_INT_DATA ppu_exc_prev_int;
    SNPS3_PPU_EXP_ALIGNMENT_DATA ppu_exc_alignment;
    SNPS3_PPU_EXP_ILL_INST_DATA ppu_exc_ill_inst;
    SNPS3_PPU_EXP_TEXT_HTAB_MISS_DATA ppu_exc_text_htab_miss;
    SNPS3_PPU_EXP_TEXT_SLB_MISS_DATA ppu_exc_text_slb_miss;
    SNPS3_PPU_EXP_DATA_HTAB_MISS_DATA ppu_exc_data_htab_miss;
    SNPS3_PPU_EXP_FLOAT_DATA ppu_exc_float;
    SNPS3_PPU_EXP_DATA_SLB_MISS_DATA ppu_exc_data_slb_miss;
    SNPS3_PPU_EXP_DABR_MATCH_DATA ppu_exc_dabr_match;
    SNPS3_PPU_EXP_STOP_DATA ppu_exc_stop;
    SNPS3_PPU_EXP_STOP_INIT_DATA ppu_exc_stop_init;
    SNPS3_PPU_EXP_DATA_MAT_DATA ppu_exc_data_mat;
    ...
};
```

The PDF does not show numeric `uEventType` values on this page. Existing local managed metadata records `PPUExcTrap = 16` and `PPUExcDabrMatch = 25`, but those numeric values are metadata/header evidence rather than PDF text from this extraction.

## PPU Trap Event Payload

### `SNPS3_PPU_EXP_TRAP_DATA`

PDF page 306:

```c
struct SNPS3_PPU_EXP_TRAP_DATA {
    UINT64 uPPUThreadID;
    UINT32 uHWThreadNumber;
    UINT64 uPC;
    UINT64 uSP;
};
```

Documented meaning:

- Data associated with a PPU trap event.
- The PDF page does not provide member descriptions beyond the field names.

## Software Breakpoint APIs

### `SNPS3SetBreakPoint`

PDF page 193:

```c
SNAPI SNRESULT SNPS3SetBreakPoint(
    HTARGET hTarget,
    UINT32 uUnit,
    UINT32 uProcessID,
    UINT64 uThreadID,
    UINT64 uAddress
);
```

Arguments:

- `hTarget`: target handle.
- `uUnit`: unit upon which the process runs, see `ESNPS3UNIT`.
- `uProcessID`: process ID.
- `uThreadID`: thread ID.
- `uAddress`: breakpoint address.

Documented results:

- `SN_S_OK`: breakpoint set successfully.
- `SN_E_DLL_NOT_INITIALISED`: target comms not initialized.
- `SN_E_BAD_TARGET`: invalid target.
- `SN_E_BAD_UNIT`: invalid unit.
- `SN_E_COMMS_ERR`: target communication error.

Documented stopped/running state:

- The page does not state that the process or PPU threads must be stopped before calling `SNPS3SetBreakPoint`.
- The page does not state whether the breakpoint is process-wide for PPU or thread-specific for PPU; it simply includes a `uThreadID` argument.

### `SNPS3ClearBreakPoint`

PDF page 28:

```c
SNAPI SNRESULT SNPS3ClearBreakPoint(
    HTARGET hTarget,
    UINT32 uUnit,
    UINT32 uProcessID,
    UINT64 uThreadID,
    UINT64 uAddress
);
```

Arguments:

- `hTarget`: target handle.
- `uUnit`: unit upon which the process runs, see `ESNPS3UNIT`.
- `uProcessID`: process ID.
- `uThreadID`: thread ID.
- `uAddress`: breakpoint address.

Documented results:

- `SN_S_OK`: breakpoint cleared successfully.
- `SN_E_DLL_NOT_INITIALISED`: target comms not initialized.
- `SN_E_BAD_TARGET`: invalid target.
- `SN_E_BAD_UNIT`: invalid unit.
- `SN_E_COMMS_ERR`: target communication error.

Documented stopped/running state:

- The page does not state that the process or PPU threads must be stopped before calling `SNPS3ClearBreakPoint`.

## DABR API Precondition

### `SNPS3SetDABR`

PDF page 198:

```c
SNAPI SNRESULT SNPS3SetDABR(
    HTARGET hTarget,
    UINT32 uProcessID,
    UINT64 uAddr
);
```

Documented rules:

- Sets a data access breakpoint.
- The process must be loaded with `SNPS3_LOAD_FLAG_ENABLE_DEBUGGING`.
- All PPU threads in the target process should be stopped before calling this function; the PDF points to `SNPS3ProcessStop()`.
- Only one data access breakpoint can be set per process.

This is the explicit stopped-thread precondition currently relevant to the DABR logger lifecycle.

## Stop and Continue Behavior

### `SNPS3ProcessStop`

PDF page 172:

```c
SNAPI SNRESULT SNPS3ProcessStop(
    HTARGET hTarget,
    UINT32 uProcessID
);
```

Documented behavior:

- Stops all threads from the specified process.

Documented results:

- `SN_S_OK`: threads stopped successfully.
- `SN_E_DLL_NOT_INITIALISED`: target comms not initialized.
- `SN_E_BAD_TARGET`: invalid target.
- `SN_E_TM_COMMS_ERR`: TM Server communication error.
- `SN_E_ERROR`: internal error.
- `SN_E_COMMS_ERR`: target communication error.

### `SNPS3ProcessContinue`

PDF page 155:

```c
SNAPI SNRESULT SNPS3ProcessContinue(
    HTARGET hTarget,
    UINT32 uProcessID
);
```

Documented behavior:

- Continues all threads from the specified process.

Documented results:

- `SN_S_OK`: threads continued successfully.
- `SN_E_DLL_NOT_INITIALISED`: target comms not initialized.
- `SN_E_BAD_TARGET`: invalid target.
- `SN_E_ERROR`: internal error.
- `SN_E_COMMS_ERR`: target communication error.

### `SNPS3ThreadContinue`

PDF page 228:

```c
SNAPI SNRESULT SNPS3ThreadContinue(
    HTARGET hTarget,
    UINT32 uUnit,
    UINT32 uProcessID,
    UINT64 uThreadID
);
```

Documented behavior:

- Restarts the specified thread.

Documented results:

- `SN_S_OK`: thread continued successfully.
- `SN_E_DLL_NOT_INITIALISED`: target comms not initialized.
- `SN_E_BAD_TARGET`: invalid target.
- `SN_E_BAD_PARAM`: invalid process ID value.
- `SN_E_COMMS_ERR`: target communication error.

PDF note:

- SPU mode is only included for backwards compatibility and only works if the thread group is stopped; the PDF suggests `SNPS3ClearSPULoopPoint()` with `bCurrentPC` set to true instead. This note is SPU-specific.

### `SNPS3ThreadExceptionClean`

PDF page 229:

```c
SNAPI SNRESULT SNPS3ThreadExceptionClean(
    HTARGET hTarget,
    UINT32 uPID,
    UINT64 uTID
);
```

Documented behavior:

- Clears the exception state of a thread and causes it to exit.

This is why `SNPS3ThreadExceptionClean` must remain forbidden for the DABR write logger path.

## Safety Conclusions For DABR Hot-PC Step-Over

Verified from `TMAPI-E.pdf`:

- Target callbacks require `SNPS3Kick()`.
- SDK 370+ requires `SNPS3Kick()` on the same thread that registered the callback.
- Target callback data can contain multiple `SN_EVENT_TARGET_HDR` events and must be looped by `uSize`.
- Target-specific debug events are `SN_EVENT_TARGET_HDR -> SNPS3_DBG_EVENT_HDR -> SNPS3_DBG_EVENT_DATA`.
- PPU trap event payload is `ThreadID, HWThreadNumber, PC, SP`.
- `SNPS3SetBreakPoint` and `SNPS3ClearBreakPoint` signatures are documented.
- `SNPS3ProcessStop` stops all process threads.
- `SNPS3ProcessContinue` continues all process threads.
- `SNPS3ThreadContinue` restarts one specified thread.
- `SNPS3ThreadExceptionClean` causes the thread to exit and must not be used.
- `SNPS3SetDABR` requires all PPU threads stopped and only one DABR per process.

Not verified from the extracted PDF pages:

- Whether `SNPS3SetBreakPoint` and `SNPS3ClearBreakPoint` are safe while the process is running.
- Whether PPU `SNPS3SetBreakPoint` ignores or uses `uThreadID`.
- Whether a software breakpoint at `PC + 4` for a DABR-hit instruction will reliably produce `PPU_EXP_TRAP` through the same pumped target-event callback path in this NetCheatPS3 session.
- Whether clearing a software breakpoint after a trap requires a stopped process/thread state.

Therefore, any hot-PC step-over implementation should remain blocked until those missing behavioral details are either located in official documentation or proven in a smaller isolated runtime test.
