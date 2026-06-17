using MESharp.Models;
using MESharp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace MESharp.ViewModels
{
    public class ApiClassDocumentationViewModel : INotifyPropertyChanged
    {
        private static readonly Dictionary<string, string> ClassSummaryHints = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Bank"] = "Banking API for opening bank interfaces, querying bank contents, and performing deposit/withdraw operations.",
            ["Inventory"] = "Inventory API for querying backpack items and running inventory actions.",
            ["Equipment"] = "Equipment API for reading worn gear and performing equipment actions.",
            ["Loot"] = "Ground loot API for querying and picking up nearby items.",
            ["MaterialCache"] = "Material cache API for interacting with your ore/material storage.",
            ["TradeWindow"] = "Trade window API for reading and interacting with active trade offers.",
            ["Familiar"] = "Familiar API for interacting with summoned familiars and familiar inventory/status.",
            ["Objects"] = "World object API for querying and acting on nearby scene objects.",
            ["Npcs"] = "NPC API for finding NPCs and interacting with them.",
            ["Players"] = "Player API for discovering nearby players and interacting with them.",
            ["GrandExchange"] = "Grand Exchange API for reading offer state and placing/canceling offers.",
            ["Interfaces"] = "UI interfaces API for finding and interacting with game interface components.",
            ["Varbits"] = "Varbit API for reading low-level game state flags.",
            ["Traversal"] = "Traversal API for pathing, movement orchestration, and route execution.",
            ["Movement"] = "Movement API for low-level movement commands and positioning.",
            ["Game"] = "Core game API exposing global game/client state and utility actions."
        };

        private readonly List<ApiPropertyDoc> _allProperties = new();
        private readonly List<ApiMethodDoc> _allMethods = new();

        private static readonly IReadOnlyList<string> PropertySortOptionsInternal = new[]
        {
            "Name (A-Z)",
            "Name (Z-A)",
            "Type (A-Z)",
            "Type (Z-A)"
        };

        private static readonly IReadOnlyList<string> MethodSortOptionsInternal = new[]
        {
            "Name (A-Z)",
            "Name (Z-A)",
            "Return Type (A-Z)",
            "Return Type (Z-A)"
        };

        public string ClassName { get; }
        public string Namespace { get; }
        public string Category { get; }
        public string Summary { get; }
        public string Description { get; }
        public List<string> RelatedClasses { get; } = new List<string>();
        public ObservableCollection<ApiPropertyDoc> Properties { get; } = new();
        public ObservableCollection<ApiMethodDoc> Methods { get; } = new();

        private ApiPropertyDoc? _selectedProperty;
        public ApiPropertyDoc? SelectedProperty
        {
            get => _selectedProperty;
            set => SetProperty(ref _selectedProperty, value);
        }

        private ApiMethodDoc? _selectedMethod;
        public ApiMethodDoc? SelectedMethod
        {
            get => _selectedMethod;
            set => SetProperty(ref _selectedMethod, value);
        }

        public IReadOnlyList<string> PropertySortOptions => PropertySortOptionsInternal;
        public IReadOnlyList<string> MethodSortOptions => MethodSortOptionsInternal;

        private string _selectedPropertySort = PropertySortOptionsInternal[0];
        public string SelectedPropertySort
        {
            get => _selectedPropertySort;
            set
            {
                if (SetProperty(ref _selectedPropertySort, value))
                {
                    ApplyPropertySort();
                }
            }
        }

        private string _selectedMethodSort = MethodSortOptionsInternal[0];
        public string SelectedMethodSort
        {
            get => _selectedMethodSort;
            set
            {
                if (SetProperty(ref _selectedMethodSort, value))
                {
                    ApplyMethodSort();
                }
            }
        }

        public ApiClassDocumentationViewModel(Type classType)
        {
            ClassName = classType.Name;
            Namespace = classType.Namespace ?? string.Empty;

            Summary = XmlDocProvider.GetSummary(classType) ?? BuildFallbackSummary(classType);
            Description = XmlDocProvider.GetRemarks(classType) ?? BuildFallbackDescription(classType);
            Category = "Core API";

            LoadProperties(classType);
            LoadMethods(classType);
            ApplyPropertySort();
            ApplyMethodSort();
        }

        private void LoadProperties(Type classType)
        {
            var properties = classType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                                      .Where(p => p.DeclaringType == classType);

            foreach (var prop in properties)
            {
                var exampleCode = XmlDocProvider.GetExample(prop);
                var examples = new List<ApiExampleDoc>();
                if (!string.IsNullOrWhiteSpace(exampleCode))
                {
                    examples.Add(new ApiExampleDoc { Title = "Example", Code = exampleCode });
                }

                var propDoc = new ApiPropertyDoc
                {
                    Name = prop.Name,
                    Type = GetFriendlyTypeName(prop.PropertyType),
                    IsStatic = prop.GetGetMethod()?.IsStatic ?? false,
                    IsReadOnly = !prop.CanWrite,
                    Summary = XmlDocProvider.GetSummary(prop) ?? BuildPropertyFallbackSummary(prop),
                    Examples = examples
                };
                _allProperties.Add(propDoc);
            }
        }

        private void LoadMethods(Type classType)
        {
            var methods = classType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                                   .Where(m => m.DeclaringType == classType && !m.IsSpecialName);

            foreach (var method in methods)
            {
                var exampleCode = XmlDocProvider.GetExample(method);
                var examples = new List<ApiExampleDoc>();
                examples.Add(new ApiExampleDoc
                {
                    Title = "Example Usage",
                    Code = string.IsNullOrWhiteSpace(exampleCode)
                        ? BuildMethodFallbackExample(classType, method)
                        : exampleCode
                });

                var methodDoc = new ApiMethodDoc
                {
                    Name = method.Name,
                    ReturnType = GetFriendlyTypeName(method.ReturnType),
                    IsStatic = method.IsStatic,
                    Summary = XmlDocProvider.GetSummary(method) ?? BuildMethodFallbackSummary(method),
                    ParametersDisplay = GetParametersDisplay(method),
                    Parameters = GetParameters(method),
                    Examples = examples,
                    Signature = BuildSignature(method),
                    ReturnDescription = XmlDocProvider.GetReturns(method) ?? string.Empty
                };

                _allMethods.Add(methodDoc);
            }
        }

        private void ApplyPropertySort()
        {
            var selectedName = SelectedProperty?.Name;
            IEnumerable<ApiPropertyDoc> sorted = SelectedPropertySort switch
            {
                "Name (Z-A)" => _allProperties.OrderByDescending(p => p.Name, StringComparer.OrdinalIgnoreCase),
                "Type (A-Z)" => _allProperties.OrderBy(p => p.Type, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase),
                "Type (Z-A)" => _allProperties.OrderByDescending(p => p.Type, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase),
                _ => _allProperties.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            };

            Properties.Clear();
            foreach (var property in sorted)
            {
                Properties.Add(property);
            }

            if (!string.IsNullOrWhiteSpace(selectedName))
            {
                SelectedProperty = Properties.FirstOrDefault(p => string.Equals(p.Name, selectedName, StringComparison.OrdinalIgnoreCase));
            }
        }

        private void ApplyMethodSort()
        {
            var selectedName = SelectedMethod?.Name;
            var selectedSignature = SelectedMethod?.Signature;
            IEnumerable<ApiMethodDoc> sorted = SelectedMethodSort switch
            {
                "Name (Z-A)" => _allMethods.OrderByDescending(m => m.Name, StringComparer.OrdinalIgnoreCase),
                "Return Type (A-Z)" => _allMethods.OrderBy(m => m.ReturnType, StringComparer.OrdinalIgnoreCase).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase),
                "Return Type (Z-A)" => _allMethods.OrderByDescending(m => m.ReturnType, StringComparer.OrdinalIgnoreCase).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase),
                _ => _allMethods.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            };

            Methods.Clear();
            foreach (var method in sorted)
            {
                Methods.Add(method);
            }

            if (!string.IsNullOrWhiteSpace(selectedName))
            {
                SelectedMethod = Methods.FirstOrDefault(m =>
                    string.Equals(m.Name, selectedName, StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrWhiteSpace(selectedSignature) || string.Equals(m.Signature, selectedSignature, StringComparison.Ordinal)));
            }
        }

        public void NavigateToMember(string resultType, string memberName, string signature)
        {
            if (string.IsNullOrWhiteSpace(resultType))
            {
                return;
            }

            if (string.Equals(resultType, "Method", StringComparison.OrdinalIgnoreCase))
            {
                SelectedProperty = null;
                SelectedMethod = Methods.FirstOrDefault(m =>
                    string.Equals(m.Name, memberName, StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrWhiteSpace(signature) || string.Equals(m.Signature, signature, StringComparison.Ordinal)))
                    ?? Methods.FirstOrDefault(m => string.Equals(m.Name, memberName, StringComparison.OrdinalIgnoreCase));
                return;
            }

            if (string.Equals(resultType, "Property", StringComparison.OrdinalIgnoreCase))
            {
                SelectedMethod = null;
                SelectedProperty = Properties.FirstOrDefault(p => string.Equals(p.Name, memberName, StringComparison.OrdinalIgnoreCase));
                return;
            }

            SelectedMethod = null;
            SelectedProperty = null;
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
                if (genericDef == typeof(IEnumerable<>))
                {
                    return $"IEnumerable<{GetFriendlyTypeName(type.GetGenericArguments()[0])}>";
                }

                if (genericDef == typeof(IReadOnlyList<>))
                {
                    return $"IReadOnlyList<{GetFriendlyTypeName(type.GetGenericArguments()[0])}>";
                }

                if (genericDef == typeof(IList<>))
                {
                    return $"IList<{GetFriendlyTypeName(type.GetGenericArguments()[0])}>";
                }

                if (genericDef == typeof(List<>))
                {
                    return $"List<{GetFriendlyTypeName(type.GetGenericArguments()[0])}>";
                }
            }

            return type.Name;
        }

        private static string BuildFallbackSummary(Type classType)
        {
            if (ClassSummaryHints.TryGetValue(classType.Name, out var hint))
            {
                return hint;
            }

            var methods = classType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(m => m.DeclaringType == classType && !m.IsSpecialName)
                .ToList();
            var properties = classType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(p => p.DeclaringType == classType)
                .ToList();

            var allStatic = methods.All(m => m.IsStatic) && properties.All(p => (p.GetGetMethod() ?? p.GetSetMethod())?.IsStatic == true);
            var domain = SplitPascalCase(classType.Name);
            var staticPart = allStatic ? " All members are static." : string.Empty;

            return $"{domain} API surface with {methods.Count} methods and {properties.Count} properties.{staticPart}";
        }

        private static string BuildFallbackDescription(Type classType)
        {
            var methods = classType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(m => m.DeclaringType == classType && !m.IsSpecialName)
                .ToList();

            if (methods.Count == 0)
            {
                return $"Use `{classType.Name}` to inspect and interact with related game state.";
            }

            var representativeActions = methods
                .Select(m => SplitPascalCase(m.Name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList();

            return $"Use `{classType.Name}` to inspect and interact with related game state. Common operations include: {string.Join(", ", representativeActions)}.";
        }

        private static string BuildMethodFallbackSummary(MethodInfo method)
        {
            var name = method.Name;
            var nameWords = SplitPascalCase(name);
            var returns = GetFriendlyTypeName(method.ReturnType);

            if (name.StartsWith("Get", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Find", StringComparison.OrdinalIgnoreCase))
            {
                return $"Retrieves data via `{nameWords}` and returns `{returns}`.";
            }

            if (name.StartsWith("Is", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Has", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Can", StringComparison.OrdinalIgnoreCase))
            {
                return $"Checks whether `{nameWords}` is true.";
            }

            if (name.StartsWith("Set", StringComparison.OrdinalIgnoreCase))
            {
                return $"Sets `{nameWords}`.";
            }

            return $"Executes `{nameWords}` and returns `{returns}`.";
        }

        private static string BuildPropertyFallbackSummary(PropertyInfo property)
        {
            var access = property.CanWrite ? "Gets or sets" : "Gets";
            return $"{access} `{SplitPascalCase(property.Name)}`.";
        }

        private static string BuildMethodFallbackExample(Type classType, MethodInfo method)
        {
            var args = method.GetParameters()
                .Select(p => BuildExampleArgument(p.ParameterType, p.Name))
                .ToList();

            var callTarget = method.IsStatic ? classType.Name : "api";
            var call = $"{callTarget}.{method.Name}({string.Join(", ", args)})";

            if (method.ReturnType == typeof(void))
            {
                if (method.IsStatic)
                {
                    return $"{call};";
                }

                return $"var api = default({classType.Name}); // acquire instance from your script context\n{call};";
            }

            if (method.IsStatic)
            {
                return $"var result = {call};";
            }

            return $"var api = default({classType.Name}); // acquire instance from your script context\nvar result = {call};";
        }

        private static string BuildExampleArgument(Type parameterType, string? parameterName)
        {
            var underlying = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
            var nameHint = parameterName ?? "value";

            if (underlying == typeof(string))
            {
                return $"\"{nameHint}\"";
            }

            if (underlying == typeof(bool))
            {
                return "false";
            }

            if (underlying == typeof(int) || underlying == typeof(short) || underlying == typeof(byte))
            {
                return "0";
            }

            if (underlying == typeof(long))
            {
                return "0L";
            }

            if (underlying == typeof(uint))
            {
                return "0u";
            }

            if (underlying == typeof(ulong))
            {
                return "0ul";
            }

            if (underlying == typeof(float))
            {
                return "0f";
            }

            if (underlying == typeof(double))
            {
                return "0d";
            }

            if (underlying == typeof(decimal))
            {
                return "0m";
            }

            if (underlying.IsEnum)
            {
                var values = Enum.GetValues(underlying);
                if (values.Length > 0)
                {
                    return $"{underlying.Name}.{values.GetValue(0)}";
                }
            }

            if (underlying.IsArray)
            {
                var elementType = underlying.GetElementType();
                var elementLiteral = elementType == null
                    ? "default"
                    : BuildExampleArgument(elementType, "item");
                return $"new[] {{ {elementLiteral} }}";
            }

            return "default";
        }

        private static string SplitPascalCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            var sb = new StringBuilder(input.Length + 8);
            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (i > 0 && char.IsUpper(c) && (char.IsLower(input[i - 1]) || char.IsDigit(input[i - 1])))
                {
                    sb.Append(' ');
                }
                sb.Append(c);
            }

            return sb.ToString();
        }

        private static string BuildSignature(MethodInfo method)
        {
            var sb = new StringBuilder();
            sb.Append("public ");
            if (method.IsStatic) sb.Append("static ");

            sb.Append(GetFriendlyTypeName(method.ReturnType));
            sb.Append(' ');
            sb.Append(method.Name);
            sb.Append('(');

            var parameters = method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                sb.Append(GetFriendlyTypeName(p.ParameterType));
                sb.Append(' ');
                sb.Append(p.Name ?? $"arg{i}");

                if (i < parameters.Length - 1) sb.Append(", ");
            }

            sb.Append(')');
            return sb.ToString();
        }

        private string GetParametersDisplay(MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (!parameters.Any()) return "()";

            var sb = new StringBuilder();
            sb.Append("(");
            for (var i = 0; i < parameters.Length; i++)
            {
                sb.Append($"{GetFriendlyTypeName(parameters[i].ParameterType)} {parameters[i].Name}");
                if (i < parameters.Length - 1) sb.Append(", ");
            }
            sb.Append(")");
            return sb.ToString();
        }

        private List<ApiParameterDoc> GetParameters(MethodInfo method)
        {
            return method.GetParameters().Select(p => new ApiParameterDoc
            {
                Name = p.Name ?? "arg",
                Type = GetFriendlyTypeName(p.ParameterType),
                Description = XmlDocProvider.GetParamDoc(method, p.Name ?? string.Empty) ?? $"Input `{p.Name ?? "arg"}`."
            }).ToList();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
