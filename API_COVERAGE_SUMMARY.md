# MESharp WPF Debug Utility - API Coverage Summary

**Generated**: 2025-10-17
**Purpose**: Comprehensive audit of C# API coverage in the WPF debug utility

---

## Executive Summary

The WPF debug utility provides test coverage for **most** core APIs, but several important classes have partial or missing coverage. This document catalogs all gaps and provides recommendations for complete coverage.

### Coverage Status Overview

| API Class | Coverage | View/Tab | Status |
|-----------|----------|----------|--------|
| Game | ✅ Complete | GameView | All 7 properties displayed |
| Skills | ✅ Complete | SkillsView | All methods + session tracking |
| Interfaces | ✅ Complete | InterfacesView | GetInterfaces() + GetSlayerTask() |
| Objects | ✅ Complete | ObjectsUnifiedView | GetAll(), ByName(), DoAction methods |
| NPC | ✅ Complete | ObjectsUnifiedView | GetAll(), ByName(), DoAction methods |
| **LocalPlayer** | ⚠️ Partial | GameView | **Only name shown - 9 methods missing** |
| **Players** | ❌ None | **NEW TAB NEEDED** | **Entire API not exposed** |
| **Chat** | ⚠️ Partial | ChatView | DequeueEvents() only - **4 methods missing** |
| **Inventory** | ⚠️ Partial | ItemsUnifiedView | Display only - **12 action methods missing** |
| **Equipment** | ⚠️ Partial | ItemsUnifiedView | Display only - **7 methods missing** |
| **Bank** | ⚠️ Partial | ItemsUnifiedView | DepositAll/Except only - **3 methods missing** |
| **Familiar** | ❌ None | **NOT IN ANY VIEW** | **8 methods not exposed** |
| **Loot** | ✅ Complete | ItemsUnifiedView | Display via ItemContainers |
| **MaterialCache** | ✅ Complete | ItemsUnifiedView | Display via ItemContainers |
| **TradeWindow** | ✅ Complete | ItemsUnifiedView | Display via ItemContainers |
| **ItemContainers** | ✅ Complete | ItemsUnifiedView | All container types covered |
| **Focus** | ❌ None | **NOT IN ANY VIEW** | **4 methods not exposed** |

---

## Detailed Gap Analysis

### 1. LocalPlayer API - MAJOR GAPS ⚠️

**Current Coverage** (GameView):
- ✅ `LocalPlayerName` - displayed with reveal/hide toggle

**Missing Coverage** (9 methods):
```csharp
❌ IsLoggedIn()              // bool - login status
❌ GetTilePosition()         // (x,y,z) - integer tile coordinates
❌ GetExactPosition()        // (x,y,z) - float exact coordinates
❌ IsMoving()                // bool - movement status
❌ GetAnimation()            // int - current animation ID
❌ IsInCombat()              // bool - combat status
❌ GetHoverProgress()        // int - action progress bar (0-100)
❌ GetInteractingWith()      // string - current target name
❌ GetInteractingWithId()    // int - current target ID
❌ DistanceTo(x,y,z)         // float - distance calculator
```

**Recommendation**:
Create comprehensive LocalPlayer section in **new Players tab** showing:
- Status indicators (logged in, moving, in combat)
- Position display (tile + exact coordinates)
- Current activity (animation ID, hover progress)
- Target information (name, ID)
- Distance calculator tool

---

### 2. Players API - COMPLETELY MISSING ❌

**Status**: Brand new API created, needs full WPF integration

**Available Methods**:
```csharp
✅ NEWLY CREATED:
   Players.GetAll()                 // List all nearby players
   Players.ByName(string)           // Find player by name
   Players.DoActionAttack(names)    // Attack player
   Players.DoActionFollow(names)    // Follow player
   Players.DoActionTrade(names)     // Trade with player
   Players.DoActionExamine(names)   // Examine player
   Player.Attack()                  // Instance method
   Player.Follow()                  // Instance method
   Player.Trade()                   // Instance method
   Player.Examine()                 // Instance method
```

**Recommendation**:
Create **Players tab** with:
- LocalPlayer section (comprehensive status display)
- Nearby Players list (DataGrid with Name, Distance, Health, Animation, Combat Level)
- Action panel (Attack, Follow, Trade, Examine buttons)
- Filter by distance/name

---

### 3. Chat API - PARTIAL COVERAGE ⚠️

**Current Coverage** (ChatView):
- ✅ `DequeueEvents()` - real-time event capture
- ✅ `SupportsEvents` / `EventsSupportError` - event API status

**Missing Coverage** (4 methods):
```csharp
❌ GetMessages()           // Message[] - retrieve all chat messages
❌ TryFind(text, limit)    // bool - search for specific message
❌ GetFriendChatList()     // string[] - list FC members
❌ PortableTime()          // int - portable timer
```

**Recommendation**:
Add to ChatView:
- **Message History** section with DataGrid showing all messages
- **Search** panel with text input and "Find Message" button
- **Friend Chat** section showing FC member list
- **Portable Timer** display

---

### 4. Inventory API - MISSING ACTIONS ⚠️

**Current Coverage** (ItemsUnifiedView):
- ✅ Item display via ItemContainers
- ✅ Basic refresh functionality

**Missing Coverage** (12 methods):
```csharp
❌ IsOpen                  // bool - inventory interface status
❌ IsFull                  // bool - completely full check
❌ IsEmpty                 // bool - no items check
❌ IsItemSelected          // bool - item selected for "use on"
❌ FreeSlots               // int - number of free slots

Action methods not testable:
❌ Eat(id/name)            // Consume food
❌ Drop(id/name)           // Drop item
❌ Use(id/name)            // Use item
❌ Equip(id/name)          // Equip item
❌ Note(id/name)           // Convert to noted

Query methods:
❌ ContainsId(id)          // Check if inventory has item
❌ CountOf(id/name)        // Get item count
```

**Recommendation**:
Add to Inventory panel in ItemsUnifiedView:
- **Status Bar**: IsOpen, IsFull, IsEmpty, IsItemSelected, FreeSlots
- **Action Buttons**: Test each action method with item ID input
- **Quick Tests**: "Contains Item", "Count Item" with ID input fields

---

### 5. Equipment API - MISSING ACTIONS ⚠️

**Current Coverage** (ItemsUnifiedView):
- ✅ Item display via ItemContainers

**Missing Coverage** (7 methods):
```csharp
❌ IsOpen()                // bool - equipment interface status
❌ OpenInterface()         // bool - open equipment screen
❌ IsEmpty()               // bool - no items equipped
❌ IsFull()                // bool - all slots filled

Action methods:
❌ UnequipById(id)         // Remove equipped item by ID
❌ UnequipByName(name)     // Remove equipped item by name
❌ DoAction(id, action)    // Custom action on equipped item
```

**Recommendation**:
Add to Equipment panel in ItemsUnifiedView:
- **Status Bar**: IsOpen, IsEmpty, IsFull
- **Interface Control**: "Open Equipment" button
- **Unequip Panel**: Item ID/Name input + "Unequip" button
- **Custom Action**: Action index input + "Do Action" button

---

### 6. Bank API - INCOMPLETE ⚠️

**Current Coverage** (ItemsUnifiedView):
- ✅ `DepositAll()` - deposit all items
- ✅ `DepositAllExcept(ids)` - deposit with exceptions
- ✅ Item display via ItemContainers

**Missing Coverage** (3 methods):
```csharp
❌ IsOpen                  // bool - bank interface status
❌ Close()                 // void - close bank
❌ GetStack(id/name)       // ulong - get item stack count
❌ DoActionById()          // Partially exposed, needs better UI
❌ DoActionByName()        // Partially exposed, needs better UI
❌ DoActionInvById()       // Not exposed at all
```

**Recommendation**:
Add to Bank panel in ItemsUnifiedView:
- **Status Display**: "Bank Open: Yes/No"
- **Controls**: "Close Bank" button
- **Item Query**: Item ID/Name input + "Get Stack" button → display count
- **Bank Actions**: Action index + item ID/name → "Do Action" buttons
- **Inventory Actions**: "Do Action on Inventory Item" panel

---

### 7. Familiar API - COMPLETELY MISSING ❌

**Status**: Entire API not exposed anywhere in WPF

**Available Methods**:
```csharp
❌ HasFamiliar()           // bool - familiar summoned check
❌ GetName()               // string - familiar name
❌ GetTimeRemaining()      // int - time left in seconds
❌ CanRenew()              // bool - can renew familiar
❌ GetSpellPoints()        // int - special move points
❌ GetHealth()             // int - familiar health
❌ CastSpecialAttack()     // bool - use special ability
❌ GetItemsDetailed()      // List<ItemContainer> - familiar inventory
```

**Recommendation**:
Add **Familiar section** to ItemsUnifiedView (new collapsible panel):
- **Status Display**:
  - Has Familiar (Yes/No)
  - Name
  - Time Remaining (countdown)
  - Can Renew (Yes/No)
- **Stats**:
  - Spell Points: X / Max
  - Health: X
- **Actions**:
  - "Cast Special Attack" button
- **Storage**:
  - Items table (using GetItemsDetailed)
  - Refresh button

---

### 8. Focus API - COMPLETELY MISSING ❌

**Status**: Window management API used internally by MainWindow but not exposed for testing

**Available Methods**:
```csharp
❌ RegisterManagedThread(threadId)      // Register UI thread
❌ RegisterManagedWindow(hwnd)          // Register window handle
❌ ActivateManagedWindow()              // Bring window to foreground
❌ SetFocusSpoofEnabled(bool)           // Enable/disable focus spoofing
```

**Current Usage**: These are called in MainWindow.xaml.cs (lines 34-105) but not exposed for manual testing

**Recommendation**:
Add **Focus Management** section to SettingsView:
- **Status Display**:
  - Current Thread ID
  - Current Window Handle
  - Focus Spoof Status (Enabled/Disabled)
- **Controls**:
  - "Activate Window" button (test ActivateManagedWindow)
  - "Enable Focus Spoof" / "Disable Focus Spoof" toggle
- **Info Panel**: Explanation of what focus spoofing does

---

## Summary of Required Changes

### New Views/Tabs Needed:
1. **Players Tab** (PlayersView.xaml + PlayersViewModel.cs)
   - LocalPlayer comprehensive status section
   - Nearby players list with actions

### Enhancements to Existing Views:

#### ItemsUnifiedView.xaml:
1. **Inventory Panel** - Add:
   - Status bar (IsOpen, IsFull, IsEmpty, IsItemSelected, FreeSlots)
   - Action test buttons (Eat, Drop, Use, Equip, Note)
   - Query tools (Contains, CountOf)

2. **Equipment Panel** - Add:
   - Status bar (IsOpen, IsEmpty, IsFull)
   - "Open Equipment" button
   - Unequip test panel
   - Custom action panel

3. **Bank Panel** - Add:
   - IsOpen status display
   - "Close Bank" button
   - GetStack query tool
   - Enhanced DoAction panels

4. **Familiar Panel** - Add (NEW):
   - Complete familiar status section
   - Special attack button
   - Familiar inventory display

#### ChatView.xaml:
1. **Message History Section** - Add:
   - GetMessages() → DataGrid
   - Message search panel
   - Friend Chat list
   - Portable timer display

#### SettingsView.xaml:
1. **Focus Management Section** - Add:
   - Focus spoof controls
   - Window activation test
   - Thread/HWND info display

---

## Implementation Priority

### Priority 1 - High Value, High Impact:
1. ✅ **Players Tab** (DONE - new API created)
   - Provides critical PvP/multiplayer functionality testing
   - Consolidates LocalPlayer comprehensive display

2. **Familiar Panel in ItemsUnifiedView**
   - Currently zero coverage of important API
   - Common feature for many scripters

### Priority 2 - Medium Value, Completes Partial Coverage:
3. **Inventory Action Buttons**
   - Makes existing display interactive
   - Tests critical scripting actions

4. **Bank Enhancements**
   - Completes partially-implemented API
   - Common scripting operations

5. **Equipment Actions**
   - Completes gear management testing
   - Common combat script operations

### Priority 3 - Nice to Have, Less Critical:
6. **Chat Message History**
   - Event capture already works well
   - Lower priority for most users

7. **Focus Management Panel**
   - Already working internally
   - Advanced debugging feature

---

## Files Created/Modified Summary

### ✅ Completed (Players API):
1. `ME/MemoryError/MESharp/Exports_Players.cpp` - NEW
2. `C#/csharp_interop/native/Native_Players.cs` - NEW
3. `C#/csharp_interop/csharp_api/Players.cs` - NEW

### 📋 Pending (WPF Integration):
1. `C#/ME_CSharp_Scripts/ME-wpf-debug/WPFScript/ViewModels/PlayersViewModel.cs` - NEW
2. `C#/ME_CSharp_Scripts/ME-wpf-debug/WPFScript/Views/PlayersView.xaml` - NEW
3. `C#/ME_CSharp_Scripts/ME-wpf-debug/WPFScript/Views/PlayersView.xaml.cs` - NEW
4. `C#/ME_CSharp_Scripts/ME-wpf-debug/WPFScript/MainWindow.xaml` - MODIFY (add Players tab)
5. `C#/ME_CSharp_Scripts/ME-wpf-debug/WPFScript/ViewModels/MainWindowViewModel.cs` - MODIFY (wire up Players)

### 📋 Future Enhancements:
6. `ItemsUnifiedView.xaml` - MODIFY (add Familiar, enhance Inv/Eq/Bank)
7. `ItemsUnifiedViewModel.cs` - MODIFY (add action command handlers)
8. `ChatView.xaml` - MODIFY (add message history)
9. `ChatViewModel.cs` - MODIFY (add GetMessages, TryFind, etc.)
10. `SettingsView.xaml` - MODIFY (add Focus section)
11. `SettingsViewModel.cs` - MODIFY (add Focus controls)

---

## Testing Checklist

When implementing changes, verify:

### Players Tab:
- [ ] LocalPlayer status updates in real-time
- [ ] GetTilePosition() shows correct coordinates
- [ ] IsMoving() accurately reflects movement
- [ ] IsInCombat() updates during combat
- [ ] GetInteractingWith() shows current target
- [ ] Players list refreshes and shows nearby players
- [ ] Attack/Follow/Trade/Examine actions work
- [ ] Distance calculations are accurate

### Familiar Panel:
- [ ] HasFamiliar() detects summoned familiar
- [ ] GetName() shows familiar name
- [ ] GetTimeRemaining() counts down correctly
- [ ] GetSpellPoints() and GetHealth() update
- [ ] CastSpecialAttack() triggers ability
- [ ] GetItemsDetailed() shows familiar inventory

### Inventory Actions:
- [ ] Status indicators update correctly
- [ ] Eat() consumes food item
- [ ] Drop() removes item from inventory
- [ ] Use() selects item for "use on"
- [ ] Equip() moves item to equipment
- [ ] Note() converts item to noted form
- [ ] Contains/CountOf queries work

### Bank Enhancements:
- [ ] IsOpen reflects bank state
- [ ] Close() closes bank interface
- [ ] GetStack() returns correct counts
- [ ] DoAction methods work with items

### Equipment Actions:
- [ ] IsOpen/OpenInterface work correctly
- [ ] UnequipById/ByName remove items
- [ ] DoAction allows custom interactions

### Chat Enhancements:
- [ ] GetMessages() retrieves message history
- [ ] TryFind() locates specific messages
- [ ] GetFriendChatList() shows FC members
- [ ] PortableTime() displays timer

### Focus Management:
- [ ] SetFocusSpoofEnabled toggles correctly
- [ ] ActivateManagedWindow brings window forward
- [ ] Thread/HWND info displays correctly

---

## Conclusion

The WPF debug utility has **excellent coverage** for core APIs (Game, Skills, Interfaces, Objects, NPCs) and **good foundation** for item containers. However, several important player-facing and action-based APIs have gaps:

**Critical Gaps**:
- LocalPlayer comprehensive display
- Players API (brand new, needs full UI)
- Familiar API (completely missing)

**Important Enhancements**:
- Inventory/Equipment/Bank action testing
- Chat message history and search

**Nice-to-Have**:
- Focus management testing panel

Implementing these changes will provide **100% test coverage** of the MESharp C# API surface area.
