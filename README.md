# NetCheatPS3

NetCheatPS3 is a Windows memory cheating and scanning tool for PS3 targets.
This fork is focused on TMAPI and PS2RD providers, cheat writing, range scanning,
and external plugins.

## Compile

1. Install Visual Studio 2022 with the .NET desktop development workload.
2. Open `NetCheatPS3.sln`.
3. Build the `NetCheatPS3` project.

## Plugins

Plugins allow third-party developers to add features to NetCheatPS3. They are
loaded from the `Plugins` folder and are accessible from the Plugins tab.

## APIs

APIs are providers that connect NetCheatPS3 to a target. APIs can be selected in
the APIs tab. This fork ships with:

- [TMAPI](./TMAPI-NCAPI/API.cs)
- [PS2RD](./PS2RD-NCAPI/API.cs)

## Tips

- Double-click a plugin to open or close it.
- Double-click a code to toggle constant write.
- Ctrl+C on selected search results copies NetCheat code format.
- Search results do not automatically refresh.
- Use Backup Memory before testing risky writes, and Reset Memory to restore a
  verified backup for the selected code.
