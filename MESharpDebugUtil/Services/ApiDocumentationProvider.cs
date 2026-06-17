using MESharp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MESharp.Services
{
    /// <summary>
    /// Provides comprehensive documentation for API classes with examples and detailed descriptions.
    /// </summary>
    public static class ApiDocumentationProvider
    {
        private static readonly Dictionary<string, ApiClassDocumentation> _documentation = new();

        static ApiDocumentationProvider()
        {
            InitializeDocumentation();
        }

        public static ApiClassDocumentation GetDocumentation(Type classType)
        {
            if (_documentation.TryGetValue(classType.Name, out var doc))
            {
                return doc;
            }

            // Return basic documentation if custom doc doesn't exist
            return CreateBasicDocumentation(classType);
        }

        private static void InitializeDocumentation()
        {
            _documentation["Dungeoneering"] = new ApiClassDocumentation
            {
                ClassName = "Dungeoneering",
                Namespace = "MESharp.API",
                Summary = "Unified dungeoneering helper surface for room signals, party hints, floor inference, and room graph state.",
                Description = @"The Dungeoneering class is the single entry point for the debug-oriented dungeoneering API. It wraps the lower-level signal classifiers, probe builders, and room-graph store behind one class so scripts and the docs browser do not need to understand the internal helper split.

**Use it for:**
• Classifying nearby dungeon objects
• Building room signal snapshots
• Inferring party and floor context
• Reading and clearing the current room graph snapshot",
                Category = "Specialized APIs",
                RelatedClasses = new List<string> { "Objects", "Players", "Chat", "Skills" },
                Methods = new List<ApiMethodDoc>
                {
                    new ApiMethodDoc
                    {
                        Name = "GetRoomSignals",
                        Summary = "Build a classified snapshot of nearby dungeoneering-relevant room signals.",
                        ReturnType = "DgRoomSignalsResult",
                        ReturnDescription = "A snapshot containing origin tile, max distance, and classified room items.",
                        Signature = "public static DgRoomSignalsResult GetRoomSignals(double maxDistance = 20, int maxCount = 120, bool includeNpcs = true)",
                        IsStatic = true,
                        Category = "Signals"
                    },
                    new ApiMethodDoc
                    {
                        Name = "GetPartyCandidates",
                        Summary = "Collect nearby player candidates and optional friend-chat members for dungeon party context.",
                        ReturnType = "DgPartyResult",
                        ReturnDescription = "A snapshot of nearby player candidates and friend-chat names.",
                        Signature = "public static DgPartyResult GetPartyCandidates(double maxDistance = 35, int maxCount = 12, bool includeFriendChat = true)",
                        IsStatic = true,
                        Category = "Party"
                    },
                    new ApiMethodDoc
                    {
                        Name = "GetFloorHints",
                        Summary = "Infer floor and complexity hints from recent chat and nearby dungeon context.",
                        ReturnType = "DgFloorHintsResult",
                        ReturnDescription = "A snapshot of inferred floor hints, ring state, and recent relevant messages.",
                        Signature = "public static DgFloorHintsResult GetFloorHints(int maxMessages = 20)",
                        IsStatic = true,
                        Category = "Floor"
                    },
                    new ApiMethodDoc
                    {
                        Name = "GetRoomGraph",
                        Summary = "Read the latest published room graph snapshot.",
                        ReturnType = "DgRoomGraphSnapshot",
                        ReturnDescription = "The most recent room graph snapshot stored by the dungeoneering graph helpers.",
                        Signature = "public static DgRoomGraphSnapshot GetRoomGraph()",
                        IsStatic = true,
                        Category = "Graph"
                    },
                    new ApiMethodDoc
                    {
                        Name = "ClearRoomGraph",
                        Summary = "Reset the stored room graph snapshot back to an empty state.",
                        ReturnType = "void",
                        ReturnDescription = "No return value.",
                        Signature = "public static void ClearRoomGraph()",
                        IsStatic = true,
                        Category = "Graph"
                    }
                }
            };

            // Inventory Class Documentation
            _documentation["Inventory"] = new ApiClassDocumentation
            {
                ClassName = "Inventory",
                Namespace = "MESharp.API",
                Summary = "Provides access to the player's inventory, allowing you to query items, perform actions, and manipulate inventory contents.",
                Description = @"The Inventory class is your primary interface for working with the player's backpack. It provides both low-level slot-based operations and high-level item queries. All methods are static and thread-safe.

**Key Features:**
• Query inventory state (open, full, empty)
• Find items by ID or name
• Perform item actions (use, drop, combine)
• Monitor selected items
• Access individual item properties",
                Category = "Core APIs",
                RelatedClasses = new List<string> { "Bank", "Equipment", "Loot" },
                Methods = new List<ApiMethodDoc>
                {
                    new ApiMethodDoc
                    {
                        Name = "FindById",
                        Summary = "Find all items in the inventory that match the specified item ID.",
                        ReturnType = "List<Item>",
                        ReturnDescription = "A list of matching items. Empty list if no matches found.",
                        Parameters = new List<ApiParameterDoc>
                        {
                            new ApiParameterDoc
                            {
                                Name = "id",
                                Type = "int",
                                Description = "The unique item ID to search for"
                            }
                        },
                        Signature = "public static List<Item> FindById(int id)",
                        IsStatic = true,
                        Category = "Queries",
                        Examples = new List<ApiExampleDoc>
                        {
                            new ApiExampleDoc
                            {
                                Title = "Find all Lobsters",
                                Description = "Search for all Lobster items in your inventory",
                                Code = @"// Find all Lobsters (ID: 377)
var lobsters = Inventory.FindById(377);
Console.WriteLine($""Found {lobsters.Count} lobsters"");

// Use the first one
if (lobsters.Any())
{
    lobsters[0].DoAction(1); // Eat
}",
                                Output = "Found 5 lobsters"
                            }
                        }
                    },
                    new ApiMethodDoc
                    {
                        Name = "FindByName",
                        Summary = "Find all items in the inventory that match the specified name (case-insensitive).",
                        ReturnType = "List<Item>",
                        ReturnDescription = "A list of matching items. Empty list if no matches found.",
                        Parameters = new List<ApiParameterDoc>
                        {
                            new ApiParameterDoc
                            {
                                Name = "name",
                                Type = "string",
                                Description = "The item name to search for (supports partial matching)"
                            }
                        },
                        Signature = "public static List<Item> FindByName(string name)",
                        IsStatic = true,
                        Category = "Queries",
                        Examples = new List<ApiExampleDoc>
                        {
                            new ApiExampleDoc
                            {
                                Title = "Find Potions",
                                Description = "Search for any items with 'potion' in their name",
                                Code = @"// Find all potions
var potions = Inventory.FindByName(""potion"");
foreach (var potion in potions)
{
    Console.WriteLine($""Slot {potion.Slot}: {potion.Name}"");
}",
                                Output = @"Slot 5: Super strength potion (4)
Slot 12: Prayer potion (3)
Slot 18: Saradomin brew (4)"
                            }
                        }
                    },
                    new ApiMethodDoc
                    {
                        Name = "GetAll",
                        Summary = "Get all 28 inventory slots, including empty slots.",
                        ReturnType = "List<Item>",
                        ReturnDescription = "A list of exactly 28 items (empty slots have Id = -1)",
                        Parameters = new List<ApiParameterDoc>(),
                        Signature = "public static List<Item> GetAll()",
                        IsStatic = true,
                        Category = "Queries",
                        Examples = new List<ApiExampleDoc>
                        {
                            new ApiExampleDoc
                            {
                                Title = "Count Non-Empty Slots",
                                Description = "Iterate all slots and count filled ones",
                                Code = @"var allSlots = Inventory.GetAll();
var filledSlots = allSlots.Count(item => item.Id > 0);
Console.WriteLine($""Inventory: {filledSlots}/28 slots used"");",
                                Output = "Inventory: 15/28 slots used"
                            }
                        }
                    },
                    new ApiMethodDoc
                    {
                        Name = "UseItemOnItem",
                        Summary = "Use one inventory item on another by their IDs.",
                        ReturnType = "bool",
                        ReturnDescription = "True if the action was successfully initiated",
                        Parameters = new List<ApiParameterDoc>
                        {
                            new ApiParameterDoc { Name = "id1", Type = "int", Description = "First item ID" },
                            new ApiParameterDoc { Name = "id2", Type = "int", Description = "Second item ID" }
                        },
                        Signature = "public static bool UseItemOnItem(int id1, int id2)",
                        IsStatic = true,
                        Category = "Actions",
                        Examples = new List<ApiExampleDoc>
                        {
                            new ApiExampleDoc
                            {
                                Title = "Combine Items",
                                Description = "Use a needle on thread to create thread-on-needle",
                                Code = @"// Use needle (ID: 1733) on thread (ID: 1734)
if (Inventory.UseItemOnItem(1733, 1734))
{
                                        Console.WriteLine(""Using needle on thread..."");
    Thread.Sleep(1000); // Wait for action
}",
                                Output = "Using needle on thread..."
                            }
                        }
                    }
                },
                Properties = new List<ApiPropertyDoc>
                {
                    new ApiPropertyDoc
                    {
                        Name = "IsOpen",
                        Summary = "Indicates whether the inventory interface is currently open and visible.",
                        Type = "bool",
                        IsReadOnly = true,
                        IsStatic = true,
                        Category = "State"
                    },
                    new ApiPropertyDoc
                    {
                        Name = "IsFull",
                        Summary = "Returns true if all 28 inventory slots are occupied.",
                        Type = "bool",
                        IsReadOnly = true,
                        IsStatic = true,
                        Category = "State"
                    },
                    new ApiPropertyDoc
                    {
                        Name = "FreeSlots",
                        Summary = "Returns the number of empty inventory slots (0-28).",
                        Type = "int",
                        IsReadOnly = true,
                        IsStatic = true,
                        Category = "State",
                        Examples = new List<ApiExampleDoc>
                        {
                            new ApiExampleDoc
                            {
                                Title = "Check Space Before Looting",
                                Description = "Ensure you have inventory space before picking up items",
                                Code = @"if (Inventory.FreeSlots < 5)
{
    Console.WriteLine(""Low on inventory space! Banking..."");
    // Navigate to bank and deposit items
}",
                                Output = "Low on inventory space! Banking..."
                            }
                        }
                    }
                }
            };

            // Bank Class Documentation
            _documentation["Bank"] = new ApiClassDocumentation
            {
                ClassName = "Bank",
                Namespace = "MESharp.API",
                Summary = "Provides access to the bank interface, allowing deposits, withdrawals, and bank inventory queries.",
                Description = @"The Bank class interfaces with RuneScape's banking system. Use it to manage your stored items, check bank space, and perform banking operations.

**Important Notes:**
• Bank must be open before most operations will work
• Bank tabs are 1-indexed (tab 1 is the first tab)
• All quantities use unsigned long (ulong) to support large stacks",
                Category = "Core APIs",
                RelatedClasses = new List<string> { "Inventory", "Equipment" },
                Methods = new List<ApiMethodDoc>
                {
                    new ApiMethodDoc
                    {
                        Name = "DepositAll",
                        Summary = "Deposit all items from your inventory into the bank.",
                        ReturnType = "bool",
                        ReturnDescription = "True if the deposit all action was triggered successfully",
                        Parameters = new List<ApiParameterDoc>(),
                        Signature = "public static bool DepositAll()",
                        IsStatic = true,
                        Category = "Actions",
                        Examples = new List<ApiExampleDoc>
                        {
                            new ApiExampleDoc
                            {
                                Title = "Quick Bank Deposit",
                                Description = "Deposit all inventory items at once",
                                Code = @"if (Bank.IsOpen)
{
    Bank.DepositAll();
    Console.WriteLine(""Deposited all items"");
    Thread.Sleep(600); // Wait for items to deposit
}",
                                Output = "Deposited all items"
                            }
                        }
                    },
                    new ApiMethodDoc
                    {
                        Name = "Withdraw",
                        Summary = "Withdraw a specific quantity of an item from the bank by its ID.",
                        ReturnType = "bool",
                        ReturnDescription = "True if the withdrawal was initiated",
                        Parameters = new List<ApiParameterDoc>
                        {
                            new ApiParameterDoc { Name = "id", Type = "int", Description = "Item ID to withdraw" },
                            new ApiParameterDoc { Name = "quantity", Type = "int", Description = "Amount to withdraw" }
                        },
                        Signature = "public static bool Withdraw(int id, int quantity)",
                        IsStatic = true,
                        Category = "Actions"
                    }
                },
                Properties = new List<ApiPropertyDoc>
                {
                    new ApiPropertyDoc
                    {
                        Name = "IsOpen",
                        Summary = "Returns true if the bank interface is currently open.",
                        Type = "bool",
                        IsReadOnly = true,
                        IsStatic = true,
                        Category = "State"
                    }
                }
            };

            // LocalPlayer Class Documentation
            _documentation["LocalPlayer"] = new ApiClassDocumentation
            {
                ClassName = "LocalPlayer",
                Namespace = "MESharp.API",
                Summary = "Provides information about the local player character, including position, stats, and state.",
                Description = @"LocalPlayer gives you access to your character's current state in the game world. Query your position, check combat status, and monitor various player properties.

**Coordinate System:**
• Positions use tile-based coordinates (X, Y, Z)
• Exact positions use floating-point coordinates
• Z represents the current floor/plane level",
                Category = "Core APIs",
                RelatedClasses = new List<string> { "Players", "Skills", "Equipment" },
                Properties = new List<ApiPropertyDoc>
                {
                    new ApiPropertyDoc
                    {
                        Name = "Name",
                        Summary = "The display name of the local player character.",
                        Type = "string",
                        IsReadOnly = true,
                        IsStatic = true,
                        Category = "Identity"
                    },
                    new ApiPropertyDoc
                    {
                        Name = "TilePosition",
                        Summary = "Current tile-based position as a (X, Y, Z) tuple.",
                        Type = "(int X, int Y, int Z)",
                        IsReadOnly = true,
                        IsStatic = true,
                        Category = "Position",
                        Examples = new List<ApiExampleDoc>
                        {
                            new ApiExampleDoc
                            {
                                Title = "Check Player Location",
                                Description = "Get and display your current coordinates",
                                Code = @"var pos = LocalPlayer.TilePosition;
Console.WriteLine($""You are at ({pos.X}, {pos.Y}, {pos.Z})"");

// Check if at Grand Exchange
if (pos.X >= 3160 && pos.X <= 3170 && pos.Y >= 3480 && pos.Y <= 3490)
{
    Console.WriteLine(""At Grand Exchange!"");
}",
                                Output = @"You are at (3165, 3485, 0)
At Grand Exchange!"
                            }
                        }
                    },
                    new ApiPropertyDoc
                    {
                        Name = "IsInCombat",
                        Summary = "Returns true if the player is currently engaged in combat.",
                        Type = "bool",
                        IsReadOnly = true,
                        IsStatic = true,
                        Category = "Combat"
                    },
                    new ApiPropertyDoc
                    {
                        Name = "IsMoving",
                        Summary = "Returns true if the player is currently moving.",
                        Type = "bool",
                        IsReadOnly = true,
                        IsStatic = true,
                        Category = "Movement"
                    },
                    new ApiPropertyDoc
                    {
                        Name = "Animation",
                        Summary = "The current animation ID being played by the player (-1 if idle).",
                        Type = "int",
                        IsReadOnly = true,
                        IsStatic = true,
                        Category = "State"
                    }
                }
            };
        }

        private static ApiClassDocumentation CreateBasicDocumentation(Type classType)
        {
            return new ApiClassDocumentation
            {
                ClassName = classType.Name,
                Namespace = classType.Namespace ?? "Unknown",
                Summary = $"Documentation for {classType.Name} class.",
                Description = "Detailed documentation is being prepared for this class.",
                Category = "APIs",
                Methods = new List<ApiMethodDoc>(),
                Properties = new List<ApiPropertyDoc>(),
                RelatedClasses = new List<string>()
            };
        }
    }

    /// <summary>
    /// Represents complete documentation for an API class.
    /// </summary>
    public class ApiClassDocumentation
    {
        public string ClassName { get; init; } = string.Empty;
        public string Namespace { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public List<ApiMethodDoc> Methods { get; init; } = new();
        public List<ApiPropertyDoc> Properties { get; init; } = new();
        public List<ApiExampleDoc> ClassExamples { get; init; } = new();
        public List<string> RelatedClasses { get; init; } = new();
        public string Remarks { get; init; } = string.Empty;

        public List<ApiMethodDoc> GetMethodsByCategory(string category)
        {
            return Methods.Where(m => m.Category == category).ToList();
        }

        public List<string> GetMethodCategories()
        {
            return Methods.Select(m => m.Category).Distinct().Where(c => !string.IsNullOrEmpty(c)).ToList();
        }
    }
}
