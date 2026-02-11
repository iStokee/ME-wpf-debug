using MESharp.Models;
using MESharp.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MESharp.ViewModels
{
    public class ApiClassDocumentationViewModel
    {
        public string ClassName { get; }
        public string Namespace { get; }
        public string Category { get; }
        public string Summary { get; }
        public string Description { get; }
        public List<string> RelatedClasses { get; } = new List<string>();
        public List<ApiPropertyDoc> Properties { get; } = new List<ApiPropertyDoc>();
        public List<ApiMethodDoc> Methods { get; } = new List<ApiMethodDoc>();

        public ApiClassDocumentationViewModel(Type classType)
        {
            ClassName = classType.Name;
            Namespace = classType.Namespace ?? string.Empty;

            Summary = XmlDocProvider.GetSummary(classType) ?? $"Provides access to {classType.Name}-related information and actions.";
            Description = XmlDocProvider.GetRemarks(classType) ?? $"The {classType.Name} class is a key component of the MESharp API, offering a suite of tools to interact with the game's {classType.Name.ToLowerInvariant()} system.";
            Category = "Core API";

            LoadProperties(classType);
            LoadMethods(classType);
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
                    Summary = XmlDocProvider.GetSummary(prop) ?? $"Gets the {prop.Name}.",
                    Examples = examples
                };
                Properties.Add(propDoc);
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
                if (!string.IsNullOrWhiteSpace(exampleCode))
                {
                    examples.Add(new ApiExampleDoc { Title = "Example Usage", Code = exampleCode });
                }

                var methodDoc = new ApiMethodDoc
                {
                    Name = method.Name,
                    ReturnType = GetFriendlyTypeName(method.ReturnType),
                    IsStatic = method.IsStatic,
                    Summary = XmlDocProvider.GetSummary(method) ?? $"Performs an action related to {method.Name}.",
                    ParametersDisplay = GetParametersDisplay(method),
                    Parameters = GetParameters(method),
                    Examples = examples,
                    Signature = BuildSignature(method),
                    ReturnDescription = XmlDocProvider.GetReturns(method) ?? string.Empty
                };

                Methods.Add(methodDoc);
            }
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
                Description = XmlDocProvider.GetParamDoc(method, p.Name ?? string.Empty) ?? string.Empty
            }).ToList();
        }
    }
}
