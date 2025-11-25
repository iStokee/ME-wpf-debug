# ME WPF Debug Utility

Interactive WPF harness for exercising the MESharp C# API. It’s a tabbed, hot‑reloadable UI you can use to poke every exposed endpoint while you iterate.

## Overview
- Builds to `MESharp_DebugUtil.dll`, intended to load via MemoryError.
- Shows live game state (inventory, bank, NPCs, objects, navigation, etc.).
- Includes example code paths for common actions so you can copy/paste patterns into your own scripts.

## Quick Start
```bash
# From repo root
cd ME-wpf-debug/WPFScript
dotnet build -c Debug
```

Load it in MemoryError:
1) Inject ME into RS3.
2) In the C# loader, select `%USERPROFILE%\MemoryError\CSharp_scripts\MESharp_DebugUtil.dll`.
3) Hit “Hot Reload” to launch the window.

## Build Notes
- Requires .NET 8.0 SDK; VS 2022 recommended for XAML editing.
- Post-build copies the DLL to `%USERPROFILE%\MemoryError\CSharp_scripts\`.
- `csharp_interop.dll` is referenced but not copied; ME provides it to avoid duplicate loads.

Common commands:
```bash
dotnet build WPFScript.csproj -c Release
dotnet run   WPFScript.csproj -c Debug   # local UI without ME injection
```

## Tabs at a Glance
- **Inventory / Bank / Equipment / Skills**: Inspect, search, and trigger common actions.
- **NPCs / Players / Objects**: Nearby entities with filtering and interaction buttons.
- **Navigation**: Walk/teleport testing, logs, and waypoint helpers.
- **Chat**: Send/receive with channel filters.
- **Settings**: Theme controls and UI toggles.

## Hot Reload Pattern
`ScriptEntry.Initialize` spins up the WPF UI on its own STA thread and hooks shutdown:
```csharp
ShutdownMonitor.Token.Register(() => _uiDispatcher?.InvokeShutdown());
_uiThread = new Thread(UiThreadProc) { IsBackground = true };
_uiThread.SetApartmentState(ApartmentState.STA);
_uiThread.Start();
```
`ScriptEntry.Shutdown` just calls back into the dispatcher to close cleanly.

## API Usage Examples
```csharp
// Inventory sample
var sharks = Inventory.FindByName("Shark");
if (sharks.Any()) Inventory.Eat("Shark");

// NPC interaction
var nearest = NPC.GetAll().OrderBy(n => n.Distance).FirstOrDefault();
nearest?.Interact(1);

// Banking
if (Bank.IsOpen) { Bank.DepositAll(); Bank.Deposit("Bronze bar", 10); }
```

## Troubleshooting
- No window? Check console output and ensure `Initialize()` ran.
- API calls failing? Verify ME is injected and `csharp_interop.dll` is loaded.
- Hot reload flakiness? Make sure there’s only one copy of `csharp_interop.dll` (see `DUPLICATE_ASSEMBLY_FIX.md`).
- UI stutter? Move heavy API calls to `Task.Run` and marshal back via dispatcher.

## Notes
- Target: `.NET 8.0-windows`, WPF, x64.
- Dependencies are embedded with Costura; the single DLL is all you need to deploy (besides ME’s runtime assemblies).

That’s it—build, load, and start testing.
