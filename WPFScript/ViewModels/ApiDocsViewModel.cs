using MESharp.Commands;
using MESharp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace MESharp.ViewModels
{
    public class ApiDocsViewModel : INotifyPropertyChanged, IActivatableViewModel
    {
        private readonly Action<string>? _openDebugToolByClassName;
        private readonly string? _initialClassName;

        private static readonly HashSet<string> DedicatedPanelApiClasses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Game",
            "Chat",
            "Skills",
            "Players",
            "LocalPlayer",
            "Traversal",
            "Movement",
            "LodestoneData",
            "Inventory",
            "Bank",
            "Equipment",
            "Loot",
            "MaterialCache",
            "TradeWindow",
            "Familiar",
            "GrandExchange",
            "Objects",
            "Npcs",
            "Interfaces",
            "Varbits"
        };

        private static readonly HashSet<string> HiddenTopLevelApiClasses = new(StringComparer.OrdinalIgnoreCase)
        {
            "DBRow",
            "DungeoneeringSignals",
            "DungeoneeringProbes",
            "DungeoneeringRoomGraph",
            "WebwalkingRoutes"
        };

        public ObservableCollection<ApiClassInfo> ApiClasses { get; private set; }
        public ObservableCollection<ApiSearchResult> SearchResults { get; } = new();

        public ICommand OpenDebugToolCommand { get; }

        private ApiClassInfo _selectedClass;
        public ApiClassInfo SelectedClass
        {
            get => _selectedClass;
            set
            {
                if (SetProperty(ref _selectedClass, value))
                {
                    if (value != null)
                    {
                        SelectedClassDocumentation = new ApiClassDocumentationViewModel(value.ClassType);
                    }

                    OnPropertyChanged(nameof(CanOpenDebugTool));
                    OnPropertyChanged(nameof(OpenDebugToolLabel));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private ApiSearchResult _selectedSearchResult;
        public ApiSearchResult SelectedSearchResult
        {
            get => _selectedSearchResult;
            set
            {
                if (SetProperty(ref _selectedSearchResult, value) && value != null)
                {
                    NavigateToResult(value);
                }
            }
        }

        private ApiClassDocumentationViewModel _selectedClassDocumentation;
        public ApiClassDocumentationViewModel SelectedClassDocumentation
        {
            get => _selectedClassDocumentation;
            set => SetProperty(ref _selectedClassDocumentation, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterClasses();
                    UpdateSearchResults();
                    OnPropertyChanged(nameof(IsSearchActive));
                    OnPropertyChanged(nameof(SearchResultSummary));
                    OnPropertyChanged(nameof(HasSearchResults));
                }
            }
        }

        private List<ApiClassInfo> _allClasses = new();
        private List<ApiSearchIndexEntry> _searchIndex = new();

        public bool CanOpenDebugTool => SelectedClass != null && !string.IsNullOrWhiteSpace(SelectedClass.DebugToolLabel);
        public string OpenDebugToolLabel => SelectedClass?.DebugToolLabel ?? "Open Debug Tool";

        public bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchText);
        public bool HasSearchResults => SearchResults.Count > 0;

        public string SearchResultSummary
        {
            get
            {
                if (!IsSearchActive)
                {
                    return "";
                }

                return SearchResults.Count == 0
                    ? "No API matches found"
                    : $"{SearchResults.Count} match{(SearchResults.Count == 1 ? string.Empty : "es")}";
            }
        }

        public ApiDocsViewModel(Action<string>? openDebugToolByClassName = null, string? initialClassName = null)
        {
            _openDebugToolByClassName = openDebugToolByClassName;
            _initialClassName = initialClassName;
            ApiClasses = new ObservableCollection<ApiClassInfo>();
            OpenDebugToolCommand = new RelayCommand(_ => OpenDebugTool(), _ => CanOpenDebugTool);
            LoadApiClasses();
        }

        private void LoadApiClasses()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "csharp_interop", StringComparison.OrdinalIgnoreCase));

                if (assembly == null)
                {
                    var assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "csharp_interop.dll");
                    if (!File.Exists(assemblyPath))
                    {
                        Console.WriteLine($"[ApiDocs] csharp_interop.dll not found at {assemblyPath}");
                        _allClasses = new List<ApiClassInfo>();
                        _searchIndex = new List<ApiSearchIndexEntry>();
                        return;
                    }

                    assembly = Assembly.LoadFrom(assemblyPath);
                }

                _allClasses = assembly.GetTypes()
                    .Where(IsBrowsableApiType)
                    .OrderBy(t => t.Name)
                    .Select(t =>
                    {
                        var toolLabel = GetDebugToolLabel(t.Name);
                        return new ApiClassInfo
                        {
                            ClassType = t,
                            Name = t.Name,
                            Namespace = t.Namespace ?? "Unknown",
                            HasDedicatedPanel = !string.IsNullOrWhiteSpace(toolLabel) || DedicatedPanelApiClasses.Contains(t.Name),
                            DebugToolLabel = toolLabel
                        };
                    })
                    .ToList();

                BuildSearchIndex();
                FilterClasses();
                UpdateSearchResults();

                if (!string.IsNullOrWhiteSpace(_initialClassName))
                {
                    var requestedClass = ApiClasses.FirstOrDefault(c => string.Equals(c.Name, _initialClassName, StringComparison.OrdinalIgnoreCase));
                    if (requestedClass != null)
                    {
                        SelectedClass = requestedClass;
                        return;
                    }
                }

                var inventoryClass = ApiClasses.FirstOrDefault(c => c.Name == "Inventory");
                if (inventoryClass != null)
                {
                    SelectedClass = inventoryClass;
                }
                else if (ApiClasses.Count > 0)
                {
                    SelectedClass = ApiClasses[0];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiDocs] Error loading API classes: {ex.Message}");
                _allClasses = new List<ApiClassInfo>();
                _searchIndex = new List<ApiSearchIndexEntry>();
            }
        }

        private void BuildSearchIndex()
        {
            _searchIndex = new List<ApiSearchIndexEntry>(_allClasses.Count * 8);

            foreach (var classInfo in _allClasses)
            {
                var type = classInfo.ClassType;
                var classSummary = XmlDocProvider.GetSummary(type) ?? string.Empty;
                _searchIndex.Add(ApiSearchIndexEntry.ForClass(classInfo, classSummary));

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(m => m.DeclaringType == type && !m.IsSpecialName);

                foreach (var method in methods)
                {
                    var summary = XmlDocProvider.GetSummary(method) ?? string.Empty;
                    _searchIndex.Add(ApiSearchIndexEntry.ForMethod(classInfo, method.Name, BuildMethodSignature(method), summary));
                }

                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(p => p.DeclaringType == type);

                foreach (var property in properties)
                {
                    var summary = XmlDocProvider.GetSummary(property) ?? string.Empty;
                    _searchIndex.Add(ApiSearchIndexEntry.ForProperty(classInfo, property.Name, BuildPropertySignature(property), summary));
                }
            }
        }

        private void FilterClasses()
        {
            ApiClasses.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allClasses
                : _allClasses.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var classInfo in filtered)
            {
                ApiClasses.Add(classInfo);
            }
        }

        private void UpdateSearchResults()
        {
            SearchResults.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                OnPropertyChanged(nameof(SearchResultSummary));
                OnPropertyChanged(nameof(HasSearchResults));
                return;
            }

            var trimmedQuery = SearchText.Trim();
            var useWildcard = trimmedQuery.Contains('*') || trimmedQuery.Contains('?');
            Regex? wildcardRegex = null;
            if (useWildcard)
            {
                wildcardRegex = BuildWildcardRegex(trimmedQuery);
            }

            var results = _searchIndex
                .Where(entry => MatchesQuery(entry, trimmedQuery, wildcardRegex))
                .OrderBy(entry => entry.ClassName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.ResultTypeSortOrder)
                .ThenBy(entry => entry.MemberName, StringComparer.OrdinalIgnoreCase)
                .Take(250)
                .Select(entry => entry.ToSearchResult())
                .ToList();

            foreach (var result in results)
            {
                SearchResults.Add(result);
            }

            OnPropertyChanged(nameof(SearchResultSummary));
            OnPropertyChanged(nameof(HasSearchResults));
        }

        private static Regex BuildWildcardRegex(string wildcard)
        {
            var escaped = Regex.Escape(wildcard)
                .Replace("\\*", ".*")
                .Replace("\\?", ".");
            return new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        private static bool MatchesQuery(ApiSearchIndexEntry entry, string query, Regex? wildcardRegex)
        {
            if (wildcardRegex != null)
            {
                return wildcardRegex.IsMatch(entry.ClassName)
                    || wildcardRegex.IsMatch(entry.MemberName)
                    || wildcardRegex.IsMatch(entry.Signature)
                    || wildcardRegex.IsMatch(entry.Summary);
            }

            return entry.SearchableText.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildMethodSignature(MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                return method.Name + "()";
            }

            var parameterText = string.Join(", ", parameters.Select(p => $"{GetFriendlyTypeName(p.ParameterType)} {p.Name}"));
            return $"{method.Name}({parameterText})";
        }

        private static string BuildPropertySignature(PropertyInfo property)
        {
            return $"{property.Name}: {GetFriendlyTypeName(property.PropertyType)}";
        }

        private static string GetFriendlyTypeName(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
            {
                return $"{GetFriendlyTypeName(underlying)}?";
            }

            if (type.IsArray)
            {
                return $"{GetFriendlyTypeName(type.GetElementType()!)}[]";
            }

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                var args = type.GetGenericArguments();
                if (args.Length == 1)
                {
                    var inner = GetFriendlyTypeName(args[0]);
                    if (genericDef == typeof(IEnumerable<>)) return $"IEnumerable<{inner}>";
                    if (genericDef == typeof(IReadOnlyList<>)) return $"IReadOnlyList<{inner}>";
                    if (genericDef == typeof(IList<>)) return $"IList<{inner}>";
                    if (genericDef == typeof(List<>)) return $"List<{inner}>";
                }
            }

            return type.Name;
        }

        private void NavigateToResult(ApiSearchResult result)
        {
            if (result.ClassInfo == null)
            {
                return;
            }

            if (!ReferenceEquals(SelectedClass, result.ClassInfo))
            {
                SelectedClass = result.ClassInfo;
            }

            SelectedClassDocumentation?.NavigateToMember(result.ResultType, result.MemberName, result.Signature);
        }

        private void OpenDebugTool()
        {
            var className = SelectedClass?.Name;
            if (string.IsNullOrWhiteSpace(className))
            {
                return;
            }

            _openDebugToolByClassName?.Invoke(className);
        }

        private static string? GetDebugToolLabel(string className)
        {
            return className switch
            {
                "Game" => "Open Game",
                "Chat" => "Open Chat",
                "Skills" => "Open Skills",
                "Players" or "LocalPlayer" => "Open Players",
                "Traversal" or "Movement" or "LodestoneData" or "Teleports" or "Minimap" => "Open Navigation",
                "Webwalking" => "Open Navigation",
                "Inventory" => "Open Items: Inventory",
                "Bank" => "Open Items: Bank",
                "Equipment" => "Open Items: Equipment",
                "Loot" => "Open Items: Loot",
                "MaterialCache" => "Open Items: Material Cache",
                "TradeWindow" => "Open Items: Trade Window",
                "Familiar" => "Open Items: Familiar",
                "ItemContainers" or "ItemContainer" or "InventoryInterfaces" => "Open Items: Inventory",
                "GrandExchange" => "Open GE",
                "Objects" => "Open Objects: Objects",
                "Npcs" => "Open Objects: NPCs",
                "GroundItems" => "Open Objects: Ground Items",
                "Interfaces" or "InterfaceIds" or "InterfaceOverrides" or "Dialogs" => "Open Interfaces",
                "Focus" => "Open Game",
                "ActionOffsets" => "Open Objects",
                "Abilities" or "ActionButtons" or "DebugDraw" or "Session" or "ScriptHost" => "Open Misc API",
                "Dungeoneering" => "Open Dungeoneering",
                "Varbits" => "Open Varbits",
                _ => null
            };
        }

        private static bool IsBrowsableApiType(Type type)
        {
            if (!type.IsPublic || !type.IsClass || type.Namespace != "MESharp.API")
            {
                return false;
            }

            if (HiddenTopLevelApiClasses.Contains(type.Name))
            {
                return false;
            }

            if (type.Name.StartsWith("Dg", StringComparison.Ordinal))
            {
                return false;
            }

            if (type.Name.StartsWith("WebwalkingStored", StringComparison.Ordinal) ||
                type.Name.StartsWith("WebwalkingRoute", StringComparison.Ordinal) ||
                string.Equals(type.Name, "WebwalkingWaypoint", StringComparison.Ordinal) ||
                string.Equals(type.Name, "WebwalkingRunResult", StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        public void OnActivated()
        {
        }

        public void OnDeactivated()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, newValue)) return false;
            field = newValue;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        private sealed class ApiSearchIndexEntry
        {
            public string ResultType { get; init; }
            public int ResultTypeSortOrder { get; init; }
            public ApiClassInfo ClassInfo { get; init; }
            public string ClassName { get; init; }
            public string MemberName { get; init; }
            public string Signature { get; init; }
            public string Summary { get; init; }
            public string SearchableText { get; init; }

            public ApiSearchResult ToSearchResult() => new()
            {
                ResultType = ResultType,
                ClassInfo = ClassInfo,
                ClassName = ClassName,
                MemberName = MemberName,
                Signature = Signature,
                Summary = Summary
            };

            public static ApiSearchIndexEntry ForClass(ApiClassInfo classInfo, string summary)
            {
                var className = classInfo.Name;
                return new ApiSearchIndexEntry
                {
                    ResultType = "Class",
                    ResultTypeSortOrder = 0,
                    ClassInfo = classInfo,
                    ClassName = className,
                    MemberName = className,
                    Signature = className,
                    Summary = summary,
                    SearchableText = BuildSearchableText("Class", className, className, summary)
                };
            }

            public static ApiSearchIndexEntry ForMethod(ApiClassInfo classInfo, string methodName, string signature, string summary)
            {
                return new ApiSearchIndexEntry
                {
                    ResultType = "Method",
                    ResultTypeSortOrder = 1,
                    ClassInfo = classInfo,
                    ClassName = classInfo.Name,
                    MemberName = methodName,
                    Signature = signature,
                    Summary = summary,
                    SearchableText = BuildSearchableText("Method", classInfo.Name, methodName, signature, summary)
                };
            }

            public static ApiSearchIndexEntry ForProperty(ApiClassInfo classInfo, string propertyName, string signature, string summary)
            {
                return new ApiSearchIndexEntry
                {
                    ResultType = "Property",
                    ResultTypeSortOrder = 2,
                    ClassInfo = classInfo,
                    ClassName = classInfo.Name,
                    MemberName = propertyName,
                    Signature = signature,
                    Summary = summary,
                    SearchableText = BuildSearchableText("Property", classInfo.Name, propertyName, signature, summary)
                };
            }

            private static string BuildSearchableText(params string[] parts)
            {
                return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
            }
        }
    }

    public class ApiClassInfo
    {
        public Type ClassType { get; init; }
        public string Name { get; init; }
        public string Namespace { get; init; }
        public bool HasDedicatedPanel { get; init; }
        public string? DebugToolLabel { get; init; }
    }

    public class ApiSearchResult
    {
        public string ResultType { get; init; } = string.Empty;
        public ApiClassInfo ClassInfo { get; init; }
        public string ClassName { get; init; } = string.Empty;
        public string MemberName { get; init; } = string.Empty;
        public string Signature { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public string DisplayName => ResultType == "Class" ? ClassName : $"{ClassName}.{MemberName}";
    }
}
