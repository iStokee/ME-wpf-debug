# ME WPF Debug Utility

> 🛠️ **Visual debugging and testing interface for MESharp** - Interactive WPF harness for the C# API

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-blue)](https://github.com/your-repo)
[![Hot Reload](https://img.shields.io/badge/Hot%20Reload-✓-brightgreen)](https://github.com/your-repo)

## 📖 Overview

The WPF Debug Utility (`MESharp_DebugUtil.dll`) is a comprehensive visual testing harness for the MESharp C# API. It provides an interactive WPF interface to test, debug, and demonstrate all functionality exposed by `csharp_interop`.

This utility serves dual purposes:
1. **Development Tool**: Test new API features as they're added
2. **API Documentation**: Live examples of how to use the MESharp API

## 🚀 Quick Start

```bash
# 1. Build the utility
cd C#/ME_CSharp_Scripts/ME-wpf-debug/WPFScript
dotnet build -c Debug

# 2. Load in MemoryError
#    - Inject ME into RuneScape 3
#    - Right click the '>" menu and select C#
#    - If the script is in the appropriate folder, it will appear automatically. If not, browse to and select it
#    - Select: %USERPROFILE%\MemoryError\CSharp_scripts\MESharp_DebugUtil.dll
#    - Click "Hot Reload"

# 3. The WPF debug window appears - explore the tabs!
```

**See [Using the API](#-using-the-api) for code examples.**

## ✨ Key Features

### Visual API Testing
- **Multi-Tab Interface**: Organized by game system (Inventory, Bank, NPCs, Players, etc.)
- **Live Data Display**: Real-time visualization of game state
- **Interactive Controls**: Buttons to trigger actions and test functionality
- **Console Output**: See results and debug messages in real-time

### Tab Organization

| Tab | Purpose | Key Features |
|-----|---------|--------------|
| **Inventory** | Test inventory API | View items, search, use/drop/equip, item-on-item actions |
| **Bank** | Test banking API | Open/close bank, deposit/withdraw, search bank |
| **Equipment** | Test equipment API | View equipped items, equip/unequip actions |
| **Skills** | Test skills API | Display all skill levels and XP |
| **NPCs** | Test NPC API | List nearby NPCs, filter, interact |
| **Players** | Test player API | List nearby players, get player info |
| **Objects** | Test object API | Find world objects, interact |
| **Chat** | Test chat API | Send messages, read chat |
| **Actions** | Test action queue | Monitor and test action system |

### Hot Reload Integration

The utility demonstrates proper hot reload implementation:

```csharp
public static class ScriptEntry
{
    // Hot reload entry point
    public static void Initialize()
    {
        // Register shutdown handler for graceful cleanup
        ShutdownMonitor.Token.Register(() => {
            _uiDispatcher?.InvokeShutdown();
        });

        // Start WPF UI on separate thread
        _uiThread = new Thread(UiThreadProc);
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();
    }

    // Graceful shutdown
    public static void Shutdown()
    {
        _uiDispatcher?.InvokeShutdown();
    }
}
```


## 🔨 Building

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 (recommended for XAML designer)
- `csharp_interop.dll` (built first)

### Build Commands

```bash
cd C#/ME_CSharp_Scripts/ME-wpf-debug/WPFScript

# Debug build
dotnet build WPFScript.csproj -c Debug

# Release build
dotnet build WPFScript.csproj -c Release

# Run directly (for local testing without ME injection)
dotnet run -c Debug
```

### PostBuild Actions

Auto-deploys to user scripts folder:

```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="
    :: ensure the target folder exists
    if not exist &quot;%USERPROFILE%\MemoryError\CSharp_scripts\&quot; (
      mkdir &quot;%USERPROFILE%\MemoryError\CSharp_scripts\&quot;
    )

    :: copy the freshly built DLL
    xcopy /Y /I &quot;$(TargetPath)&quot; &quot;%USERPROFILE%\MemoryError\CSharp_scripts\&quot;
  " />
</Target>
```

**Output Location**: `%USERPROFILE%\MemoryError\CSharp_scripts\MESharp_DebugUtil.dll`

## 💻 Using the API

The WPFScript serves as a comprehensive example of MESharp API usage. Here are patterns demonstrated:

### Pattern 1: Inventory Operations

From `InventoryViewModel.cs`:

```csharp
using MESharp.API;

// Check inventory state
if (Inventory.IsOpen && !Inventory.IsEmpty)
{
    // Get all items
    var items = Inventory.GetAll();

    // Find specific items
    var sharks = Inventory.FindByName("Shark");
    var count = Inventory.CountOf("Shark");

    // Perform actions
    if (count > 0)
    {
        Inventory.Eat("Shark");
    }
}
```

### Pattern 2: NPC Interaction

From `NPCViewModel.cs`:

```csharp
using MESharp.API;

// Get all nearby NPCs
var npcs = NPC.GetAll();

// Filter NPCs
var goblins = npcs.Where(n => n.Name.Contains("Goblin")).ToList();

// Interact with NPC
var nearest = goblins.OrderBy(n => n.Distance).FirstOrDefault();
if (nearest != null)
{
    nearest.Interact(1); // Use first menu action
}
```

### Pattern 3: Banking

From `BankViewModel.cs`:

```csharp
using MESharp.API;

// Open bank
if (!Bank.IsOpen)
{
    Objects.FindByName("Bank chest")?.FirstOrDefault()?.Interact(1);
    await Task.Delay(1000); // Wait for bank to open
}

// Deposit items
if (Bank.IsOpen)
{
    Bank.DepositAll();
    // or specific items
    Bank.Deposit("Bronze bar", 10);
}
```

### Pattern 4: Async UI Updates

Proper WPF threading with MESharp:

```csharp
private async void RefreshButton_Click(object sender, RoutedEventArgs e)
{
    // Run API call on background thread
    var items = await Task.Run(() => Inventory.GetAll());

    // Update UI on dispatcher thread
    Application.Current.Dispatcher.Invoke(() =>
    {
        InventoryItems.Clear();
        foreach (var item in items)
        {
            InventoryItems.Add(new InventoryItemModel(item));
        }
    });
}
```

### Pattern 5: Graceful Shutdown

Using cancellation tokens:

```csharp
private CancellationTokenSource _cts = new();

public void Initialize()
{
    // Register for shutdown signal
    ShutdownMonitor.Token.Register(() => {
        _cts.Cancel();
    });

    // Main loop with cancellation support
    Task.Run(async () => {
        while (!_cts.Token.IsCancellationRequested)
        {
            UpdateGameState();
            await Task.Delay(1000, _cts.Token);
        }
    }, _cts.Token);
}
```

## 📦 Dependencies

```xml
<PackageReference Include="Costura.Fody" Version="6.0.0" />
<PackageReference Include="MahApps.Metro" Version="2.4.11" />
<PackageReference Include="MahApps.Metro.IconPacks" Version="6.1.0" />
<PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.135" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="McMaster.NETCore.Plugins" Version="2.0.0" />
```

### Costura.Fody

Embeds all dependencies into a single DLL for easy deployment:

```xml
<FodyWeavers>
  <Costura />
</FodyWeavers>
```

This means you only need to deploy `MESharp_DebugUtil.dll` - all dependencies (MahApps, IconPacks, etc.) are embedded.

## csharp_interop Reference

**CRITICAL**: The project references `csharp_interop.dll` but does NOT copy it to output:

```xml
<ItemGroup>
  <!-- Reference csharp_interop but DON'T copy it to output by default (ME loads it) -->
  <Reference Include="csharp_interop">
    <HintPath>..\..\..\csharp_interop\bin\Debug\net8.0-windows\csharp_interop.dll</HintPath>
    <Private>false</Private>  <!-- DON'T copy to output -->
    <EmbedInteropTypes>False</EmbedInteropTypes>
  </Reference>
</ItemGroup>
```

**Why**: ME loads `csharp_interop.dll` from its own directory. If scripts copy their own version, you get duplicate assembly loading which breaks hot reload.

**See**: `DUPLICATE_ASSEMBLY_FIX.md` in the root directory for details.

## 🚀 Loading the Utility

### Via MemoryError ImGui

1. Inject ME into RuneScape 3 client
2. Open ME's ImGui menu (typically Insert key)
3. Navigate to "Load .NET Script"
4. Select `%USERPROFILE%\MemoryError\CSharp_scripts\MESharp_DebugUtil.dll`
5. Click "Hot Reload"

### Via Orbit Integration (Optional)

The utility can optionally integrate with Orbit for embedded display:

```csharp
// From ScriptEntry.cs - Optional Orbit integration
Type? orbitApiType = Type.GetType("Orbit.OrbitAPI, Orbit");
if (orbitApiType != null)
{
    var registerMethod = orbitApiType.GetMethod("RegisterScriptWindow");
    var sessionId = registerMethod?.Invoke(null, new object[] {
        windowHandle,
        "MESharp Debug",
        null
    });
}
```

If Orbit is running, the debug window embeds as a tab. Otherwise, it runs standalone.

## 📋 Features by Tab

### Inventory Tab
- View all inventory items with icons
- Search/filter items
- Use, drop, equip, eat actions
- Item-on-item combinations
- Selection state display
- Stack amounts, noted items

### Bank Tab
- View bank contents
- Deposit/withdraw items
- Deposit all / withdraw all
- Search bank
- Pin/favorite management
- Tab navigation

### Equipment Tab
- Display all equipment slots
- See equipped item stats
- Equip/unequip actions
- Slot-based display (head, body, legs, etc.)

### Skills Tab
- All 28 skills displayed
- Current level vs boosted level
- XP progress bars
- Total level calculation
- XP/hour tracking (if implemented)

### NPCs Tab
- List all nearby NPCs
- Filter by name
- Sort by distance
- Display NPC stats (health, combat level)
- Interaction buttons
- Hover highlighting

### Players Tab
- List nearby players
- Player names and combat levels
- Distance calculation
- Equipment visible
- Clan/group indicators

### Objects Tab
- Find world objects
- Filter by name/ID
- Interaction testing
- Coordinate display
- Object state (open/closed doors, etc.)

### Chat Tab
- Read game chat
- Send chat messages
- Filter by channel (public, private, clan, etc.)
- Chat history
- Command testing

## ⚙️ Development Workflow

### Adding a New Tab

1. **Create View**: `Views/NewFeatureView.xaml`
2. **Create ViewModel**: `ViewModels/NewFeatureViewModel.cs`
3. **Create Models**: `Models/NewFeatureModel.cs` (if needed)
4. **Add to MainWindow**: Add TabItem to `MainWindow.xaml`
5. **Wire DataContext**: Bind ViewModel in code-behind
6. **Test**: Build and load in ME

### Example: Adding a "Prayers" Tab

```csharp
// Models/PrayerModel.cs
public class PrayerModel
{
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public int Level { get; set; }
}

// ViewModels/PrayersViewModel.cs
public class PrayersViewModel : INotifyPropertyChanged
{
    public ObservableCollection<PrayerModel> Prayers { get; } = new();

    public void RefreshPrayers()
    {
        Prayers.Clear();
        var prayers = Prayer.GetAll(); // MESharp API call
        foreach (var p in prayers)
        {
            Prayers.Add(new PrayerModel {
                Name = p.Name,
                IsActive = p.IsActive,
                Level = p.RequiredLevel
            });
        }
    }
}

// Views/PrayersView.xaml
<UserControl>
    <DataGrid ItemsSource="{Binding Prayers}" />
</UserControl>
```

## Testing New API Features

When a new API is added to `csharp_interop`:

1. **Add Test Button**: Create button in appropriate tab
2. **Add Event Handler**: Wire up click handler
3. **Call API**: Invoke new API method
4. **Display Result**: Show result in UI or console
5. **Handle Errors**: Wrap in try-catch, show errors
6. **Document**: Add XML comments to demonstrate usage

Example:

```csharp
private void TestNewFeature_Click(object sender, RoutedEventArgs e)
{
    try
    {
        var result = NewAPI.TestMethod();
        ResultTextBox.Text = $"Success: {result}";
        Console.WriteLine($"[TEST] New API returned: {result}");
    }
    catch (Exception ex)
    {
        ResultTextBox.Text = $"Error: {ex.Message}";
        Console.WriteLine($"[ERROR] Test failed: {ex}");
    }
}
```

## Architecture Notes

### MVVM Pattern

The utility follows MVVM (Model-View-ViewModel):

- **Model**: Pure data classes representing game entities
- **View**: XAML UI definitions
- **ViewModel**: Business logic, API calls, data binding

### Dependency Injection

Uses Microsoft.Extensions.DependencyInjection:

```csharp
// From ScriptEntry.cs
ScriptRuntime.ConfigureServices(services =>
{
    services.AddSingleton<WpfScriptShell>();
});

// Access services
var shell = ScriptRuntime.Services.GetRequiredService<WpfScriptShell>();
```

### Threading Model

- **Main Thread**: WPF UI thread (STA)
- **Background Threads**: API calls and long-running operations
- **Dispatcher**: Marshals updates back to UI thread

```csharp
// Call API on background thread
await Task.Run(() => {
    var data = Inventory.GetAll();

    // Marshal UI update to dispatcher
    Application.Current.Dispatcher.Invoke(() => {
        UpdateUI(data);
    });
});
```

## Common Patterns

### Auto-Refresh Timer

```csharp
private DispatcherTimer _refreshTimer;

private void StartAutoRefresh()
{
    _refreshTimer = new DispatcherTimer();
    _refreshTimer.Interval = TimeSpan.FromSeconds(1);
    _refreshTimer.Tick += (s, e) => RefreshData();
    _refreshTimer.Start();
}
```

### Error Handling

```csharp
private void SafeApiCall(Action action)
{
    try
    {
        action();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] {ex.Message}");
        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

### Async Button Click

```csharp
private async void ActionButton_Click(object sender, RoutedEventArgs e)
{
    var button = (Button)sender;
    button.IsEnabled = false;

    try
    {
        await Task.Run(() => PerformLongAction());
    }
    finally
    {
        button.IsEnabled = true;
    }
}
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| **Window doesn't appear** | Check console for errors, verify Initialize() is called |
| **API calls fail** | Ensure ME is injected and running |
| **UI freezes** | Move API calls to background threads with Task.Run() |
| **Hot reload fails after first edit** | Check for duplicate csharp_interop.dll (see DUPLICATE_ASSEMBLY_FIX.md) |
| **Missing dependencies** | Clean + rebuild, Costura should embed all deps |
| **Can't find window** | Check taskbar, window may be off-screen or minimized |

## Performance Tips

- **Throttle API Calls**: Don't poll too frequently (max ~1-2 times per second)
- **Lazy Load Tabs**: Only refresh active tab's data
- **Virtualize Large Lists**: Use `VirtualizingStackPanel` for 100+ items
- **Dispose Timers**: Stop timers when tabs aren't visible

## Platform

- **Target**: `.NET 8.0-windows`
- **UI Framework**: WPF
- **Assembly Name**: `MESharp_DebugUtil`
- **Platform**: `x64` only

## See Also

- [csharp_interop README](../../csharp_interop/README.md) - API layer documentation
- [CLAUDE.md](../../../CLAUDE.md) - Full project architecture
- [wpf_template.txt](../../../wpf_template.txt) - Template for new WPF scripts
- [HOT_RELOAD_TEST_PLAN.md](../../../HOT_RELOAD_TEST_PLAN.md) - Testing hot reload
