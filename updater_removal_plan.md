# NetCheatPS3 Updater Removal Plan

## Baseline
- Current HEAD commit: `13f32154c430b9747b9976963433a4182b5bddb4` (`13f3215 Remove legacy Form1 designer backup`)
- Initial `git status --short`: clean
- Pre-plan check: working tree was clean before creating this markdown file.

## Updater inventory

| Path | Symbol/name | Evidence | Classification |
| --- | --- | --- | --- |
| `NetCheatPS3.sln` | `NetCheatPS3Updater` project | Project entry at line 8 references `NetCheatPS3Updater\NetCheatPS3Updater.csproj`; configuration entries for GUID `{0722D1A4-16A6-4F6B-8C28-5ED05CBDF47C}` at lines 54-64. | Definitely updater-only |
| `NetCheatPS3Updater\NetCheatPS3Updater.csproj` | Updater helper executable project | `OutputType` is `Exe`, `RootNamespace`/`AssemblyName` are `NetCheatPS3Updater` at lines 8, 10-11; compiles `Program.cs` and `Properties\AssemblyInfo.cs` at lines 66-67; includes `App.config` at line 70. | Definitely updater-only |
| `NetCheatPS3Updater\NetCheatPS3Updater.csproj` | Post-build copy of updater executable | `PostBuildEvent` at lines 74-77 copies `$(TargetPath)` to `..\NetCheatPS3\bin\x86\Debug\`. | Definitely updater-only |
| `NetCheatPS3Updater\Program.cs` | `NetCheatPS3Updater.Program` | Namespace at line 9; waits for parent PID, copies extracted files, deletes update directory, restarts app with `Process.Start(args[3])` at lines 18-39. | Definitely updater-only |
| `NetCheatPS3Updater\Program.cs` | `ProcessExists(int id)` | Helper at lines 13-15 used by updater wait loop at line 24. | Definitely updater-only |
| `NetCheatPS3Updater\App.config` | Updater runtime config | Only declares .NET 4.8.1 supported runtime at line 5 for the updater executable. | Definitely updater-only |
| `NetCheatPS3Updater\Properties\AssemblyInfo.cs` | Updater assembly metadata | `AssemblyTitle` and `AssemblyProduct` are `NetCheatPS3Updater` at lines 8 and 12; version attributes at lines 35-36. | Definitely updater-only |
| `NetCheatPS3\Form1.cs` | `using System.Net;` | Line 7; search found `WebClient` usage only in updater methods at lines 437 and 457. | Definitely updater-only if updater methods are removed |
| `NetCheatPS3\Form1.cs` | `using Ionic.Zip;` | Line 11; search found `ZipFile`, `ZipEntry`, and `ExtractExistingFileAction` only in `DecompressFile`. | Definitely updater-only if `DecompressFile` is removed |
| `NetCheatPS3\Form1.cs` | `allowForce` field | Line 62; read by `RunUpdateChecker` and toggled by `updateStripMenuItem1_Click` only. | Definitely updater-only |
| `NetCheatPS3\Form1.cs` | `RunUpdateChecker()` | Lines 370-426; calls `CheckForUpdate`, shows `updateForm`, optionally calls `UpdateNetCheatPS3`. | Definitely updater-only |
| `NetCheatPS3\Form1.cs` | `CheckForUpdate()` | Lines 430-446; downloads `NetCheatUpdate.txt` through `WebClient`. | Definitely updater-only |
| `NetCheatPS3\Form1.cs` | `UpdateNetCheatPS3()` | Lines 451-480; downloads update zip, extracts it, copies `NetCheatPS3Updater.exe`, starts the updater executable, kills current process. | Definitely updater-only |
| `NetCheatPS3\Form1.cs` | `DecompressFile(string file, string directory)` | Lines 483-508; uses Ionic.Zip to extract update package. Search found only updater call at line 461. | Definitely updater-only |
| `NetCheatPS3\Form1.cs` | Startup update-check thread | `Main_Load` creates and starts `updateCheckThread` for `RunUpdateChecker` at lines 633-635. | Definitely updater-only |
| `NetCheatPS3\Form1.cs` | Stale commented updater cleanup | Commented cleanup for `.bak` and `updateNC.bat` at lines 638-641. | Definitely updater-only, but low priority because it is commented |
| `NetCheatPS3\Form1.cs` | `updateStripMenuItem1_Click` | Lines 2054-2059; forces manual update check by toggling `allowForce` and calling `RunUpdateChecker`. | Definitely updater-only |
| `NetCheatPS3\Form1.Designer.cs` | `updateStripMenuItem1` field | Declaration/initialization at lines 116 and 1324. | Definitely updater-only designer member |
| `NetCheatPS3\Form1.Designer.cs` | Update menu item in dropdown | `toolStripDropDownButton1.DropDownItems.AddRange` includes `updateStripMenuItem1` at line 1024. | Definitely updater-only designer binding |
| `NetCheatPS3\Form1.Designer.cs` | Update menu item properties and event binding | Lines 1081-1086 set name/text `"Update NetCheat"` and bind click handler `updateStripMenuItem1_Click`. | Definitely updater-only designer binding |
| `NetCheatPS3\updateForm.cs` | `updateForm` dialog | Class at line 12; fields `Title`, `UpdateStr`, `Return` at lines 15-17; yes/no handlers at lines 76-83. Used only by `RunUpdateChecker`. | Definitely updater-only |
| `NetCheatPS3\updateForm.Designer.cs` | Updater confirmation dialog controls | Defines `titleLabel`, `updateBox`, `yesButt`, `noButt`; form name `updateForm`; `Load` and `Resize` bindings at lines 97-99. | Definitely updater-only |
| `NetCheatPS3\updateForm.resx` | Updater dialog icon/resource | `$this.Icon` resource starts at line 121 for `updateForm`. | Definitely updater-only if `updateForm` is removed |
| `NetCheatPS3\NetCheatPS3.csproj` | `updateForm` compile/resource entries | `Compile Include="updateForm.cs"` at line 262, `updateForm.Designer.cs` at line 265, `EmbeddedResource Include="updateForm.resx"` at line 317. | Definitely updater-only |
| `NetCheatPS3\NetCheatPS3.csproj` | Ionic.Zip assembly reference | Reference and hint path at lines 104-105. Search found Ionic.Zip only in updater extraction code. | Definitely updater-only if updater extraction is removed |
| `NetCheatPS3\packages.config` | `Ionic.Zip` NuGet package | Package entry at line 3; search found only updater extraction usage. | Definitely updater-only if no package restore convention needs it retained |
| `NetCheatPS3\NetCheatPS3.csproj` | ClickOnce/publish metadata | `PublishUrl` line 26, `UpdateEnabled=false` line 29, `ApplicationVersion` line 37, bootstrapper packages lines 324-353. | Maybe shared build/publish metadata; remove only after confirming normal build/release does not rely on it |
| `NetCheatPS3\Form1.cs` | Version URL `NetCheatUpdate.txt` | Line 432: `http://netcheat.gamehacking.org/ncUpdater/NetCheatUpdate.txt`. | Definitely updater-only |
| `NetCheatPS3\Form1.cs` | Download URL `ncUpdateDir.zip` | Line 453: `http://netcheat.gamehacking.org/ncUpdater/ncUpdateDir.zip`. | Definitely updater-only |
| `NetCheatPS3\Form1.cs` | External executable reference | Lines 465-473 reference and start `NetCheatPS3Updater.exe`. | Definitely updater-only |
| `Updates.txt` | Historical updater notes and download URL | Line 2 contains legacy update zip URL; lines 60, 65, 84, 119, 137, 142 mention updater/update history. | Maybe documentation/history; leave untouched unless separately requested |

Non-updater search hits to avoid:
- `Process.Start` in `CCAPI-NCAPI\CCAPI.cs`, `TMAPI-NCAPI\API.cs`, `NetCheatPS3\OptionForm.cs`, and `NetCheatPS3\Form1.cs:3494` are API/contact/help flows, not the updater.
- `update` variables in comparator/search/list controls are ordinary UI/progress/update semantics, not the updater.
- API contact URLs, README links, CodeProject comments, and resource XML schema URLs are unrelated.

## Exact removal sequence

Commit 1: Remove startup update-check thread/call.
- In `NetCheatPS3\Form1.cs`, remove the `updateCheckThread` creation/start block in `Main_Load`.
- Remove the adjacent stale commented update cleanup only if it is part of the same updater startup block and no useful non-updater context remains.
- Do not remove updater methods or designer bindings yet.
- Verify startup no longer calls `RunUpdateChecker`.

Commit 2: Remove manual Update menu item and designer binding.
- In `NetCheatPS3\Form1.Designer.cs`, remove `updateStripMenuItem1` initialization, `DropDownItems.AddRange` entry, property block, click binding, and field declaration.
- In `NetCheatPS3\Form1.cs`, remove `updateStripMenuItem1_Click`.
- Verify the options/status dropdown still contains the remaining menu items in the same order, and the designer opens without a missing handler.

Commit 3: Remove updater methods from `Form1.cs`.
- Remove `allowForce`.
- Remove the `#region NetCheat Updater` block: `RunUpdateChecker`, `CheckForUpdate`, `UpdateNetCheatPS3`, and `DecompressFile`.
- Remove now-unused `using System.Net;` and `using Ionic.Zip;`.
- Keep `using System.Diagnostics;` and `using System.IO;` unless a compiler check proves they are unused elsewhere.
- Verify no `RunUpdateChecker`, `CheckForUpdate`, `UpdateNetCheatPS3`, `DecompressFile`, `WebClient`, updater URLs, `ncUpdateDir`, or `NetCheatPS3Updater.exe` references remain in `NetCheatPS3\Form1.cs`.

Commit 4: Remove updater dialog from the app project.
- Delete `NetCheatPS3\updateForm.cs`, `NetCheatPS3\updateForm.Designer.cs`, and `NetCheatPS3\updateForm.resx`.
- Remove the corresponding compile/resource entries from `NetCheatPS3\NetCheatPS3.csproj`.
- Verify no `updateForm` references remain.

Commit 5: Remove updater-only zip dependency.
- Remove the `Ionic.Zip` reference from `NetCheatPS3\NetCheatPS3.csproj`.
- Remove `Ionic.Zip` from `NetCheatPS3\packages.config`; if that leaves `packages.config` empty and no other packages are expected, delete `NetCheatPS3\packages.config` and its `<None Include="packages.config" />` entry.
- Do not delete the repository-level `packages\Ionic.Zip.1.9.1.8` folder in this commit unless it is tracked and no other project references it.

Commit 6: Remove `NetCheatPS3Updater` project from the solution.
- Remove the `NetCheatPS3Updater` project entry from `NetCheatPS3.sln`.
- Remove all solution configuration entries for GUID `{0722D1A4-16A6-4F6B-8C28-5ED05CBDF47C}`.
- Verify the solution still opens and all remaining project GUID blocks are intact.

Commit 7: Delete `NetCheatPS3Updater` project folder/files.
- Delete `NetCheatPS3Updater\NetCheatPS3Updater.csproj`.
- Delete `NetCheatPS3Updater\Program.cs`.
- Delete `NetCheatPS3Updater\App.config`.
- Delete `NetCheatPS3Updater\Properties\AssemblyInfo.cs`.
- Delete the now-empty `NetCheatPS3Updater` folder if possible.

Commit 8: Review stale publish/update metadata.
- Re-check `NetCheatPS3\NetCheatPS3.csproj` metadata: `PublishUrl`, `Install`, `InstallFrom`, `UpdateEnabled`, `UpdateMode`, `UpdateInterval`, `UpdatePeriodically`, `UpdateRequired`, `ApplicationRevision`, `ApplicationVersion`, `PublishWizardCompleted`, `BootstrapperEnabled`, and `BootstrapperPackage` entries.
- Remove only metadata confirmed unrelated to normal Visual Studio build, ClickOnce publishing still in use, or release packaging.
- Leave this commit out if release packaging expectations are unclear.

## Files expected to change

Likely modified:
- `NetCheatPS3\Form1.cs`
- `NetCheatPS3\Form1.Designer.cs`
- `NetCheatPS3\NetCheatPS3.csproj`
- `NetCheatPS3.sln`
- `NetCheatPS3\packages.config` if Ionic.Zip is removed there and the file remains

Likely deleted:
- `NetCheatPS3\updateForm.cs`
- `NetCheatPS3\updateForm.Designer.cs`
- `NetCheatPS3\updateForm.resx`
- `NetCheatPS3Updater\NetCheatPS3Updater.csproj`
- `NetCheatPS3Updater\Program.cs`
- `NetCheatPS3Updater\App.config`
- `NetCheatPS3Updater\Properties\AssemblyInfo.cs`
- Possibly `NetCheatPS3\packages.config` if it becomes empty and the project no longer needs it
- Possibly tracked `packages\Ionic.Zip.1.9.1.8\...` files if they are tracked and confirmed unused by all projects

Likely unchanged unless separately requested:
- `Updates.txt`, because updater mentions there appear to be historical changelog entries.
- `README.md`, because no current updater instructions were found.
- Other API projects with unrelated `Process.Start`, URL, or version references.

## Risk points

- Startup: removing the thread is low risk, but `Main_Load` should still initialize tabs, code types, APIs, plugins, and UI state in the same order.
- Menu designer: removing `updateStripMenuItem1` by hand can leave a stale field, stale event binding, or malformed `DropDownItems.AddRange`.
- Solution loading: removing the updater project requires deleting both the project block and every GUID configuration line.
- Build: removing `updateForm` requires matching `.csproj` compile/resource cleanup.
- Build: removing Ionic.Zip requires confirming no remaining `Ionic.Zip`, `ZipFile`, `ZipEntry`, or `ExtractExistingFileAction` references.
- Release packaging: the updater project currently copies its executable into `NetCheatPS3\bin\x86\Debug`; removing it may affect any manual packaging process that expects that exe.
- Resources: deleting `updateForm.resx` is safe only after the dialog is removed from the project and no designer references remain.
- Publish metadata: ClickOnce/bootstrapper settings may be legacy updater-adjacent metadata, but they can affect Visual Studio publish behavior; remove only after confirming release process.

## Local acceptance tests after implementation

- App starts without creating an update-check thread.
- No startup network request or update prompt appears.
- `Update NetCheat` menu item is gone.
- Solution opens in VS2022.
- Debug build succeeds.
- App launches.
- Connect/attach/search still works.
- No references to `NetCheatPS3Updater` remain except git history or documentation.
- `rg -n "RunUpdateChecker|CheckForUpdate|UpdateNetCheatPS3|updateStripMenuItem1_Click|NetCheatPS3Updater|Update Available|Update NetCheat|ncUpdateDir|NetCheatUpdate" .` returns only intentional documentation/history hits.
- `rg -n "WebClient|Ionic\\.Zip|ZipFile|ZipEntry|ExtractExistingFileAction" NetCheatPS3` returns no updater leftovers.

## Recommended first implementation prompt

```text
Work in this local repo. Make the first small updater-removal change only. Do not commit.

Task:
Remove only the startup update-check thread/call from `NetCheatPS3/Form1.cs`.

Rules:
- Before editing, run `git status --short` and stop if the tree is not clean.
- Do not touch `Form1.Designer.cs`, `.csproj`, `.sln`, resources, or the `NetCheatPS3Updater` folder.
- In `Main_Load`, remove the `System.Threading.Thread updateCheckThread = new System.Threading.Thread(new System.Threading.ThreadStart(RunUpdateChecker));`, `IsBackground`, and `Start()` lines.
- Remove the adjacent stale commented updater cleanup lines only if they are part of that same updater startup block.
- Do not remove `RunUpdateChecker`, `CheckForUpdate`, `UpdateNetCheatPS3`, `updateForm`, or the menu item in this first change.
- After editing, run `git diff -- NetCheatPS3/Form1.cs` and `git status --short`.

Report exactly what changed and do not commit.
```
