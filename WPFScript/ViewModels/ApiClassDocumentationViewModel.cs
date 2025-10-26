using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MESharp.Models;

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
            Namespace = classType.Namespace;

            // Placeholder data - in a real app, this would come from attributes or an XML doc file.
            Category = "Core API";
            Summary = $"Provides access to {classType.Name}-related information and actions.";
            Description = $"The {classType.Name} class is a key component of the MESharp API, offering a suite of tools to interact with the game's {classType.Name.ToLower()} system.";

            LoadProperties(classType);
            LoadMethods(classType);
        }

        private void LoadProperties(Type classType)
        {
            var properties = classType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

            foreach (var prop in properties)
            {
                var propDoc = new ApiPropertyDoc
                {
                    Name = prop.Name,
                    Type = prop.PropertyType.Name,
                    IsStatic = prop.GetGetMethod()?.IsStatic ?? false,
                    IsReadOnly = !prop.CanWrite,
                    Summary = $"Gets the {prop.Name}.",
                    Examples = new List<ApiExampleDoc>
                    {
                        new ApiExampleDoc
                        {
                            Title = "Example",
                            Code = $"var {prop.Name.ToLower()} = {classType.Name}.{prop.Name};"
                        }
                    }
                };
                Properties.Add(propDoc);
            }
        }

        private void LoadMethods(Type classType)
        {
            var methods = classType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                                   .Where(m => !m.IsSpecialName); // Exclude property getters/setters

            foreach (var method in methods)
            {
                var methodDoc = new ApiMethodDoc
                {
                    Name = method.Name,
                    ReturnType = method.ReturnType.Name,
                    IsStatic = method.IsStatic,
                    Summary = $"Performs an action related to {method.Name}.",
                    ParametersDisplay = GetParametersDisplay(method),
                    Parameters = GetParameters(method),
                    Examples = new List<ApiExampleDoc>
                    {
                        new ApiExampleDoc
                        {
                            Title = "Example Usage",
                            Code = $"// Example for {method.Name}\n{classType.Name}.{method.Name}();"
                        }
                    }
                };
                Methods.Add(methodDoc);
            }
        }

        private string GetParametersDisplay(MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (!parameters.Any()) return "()";

            var sb = new StringBuilder();
            sb.Append("(");
            for (int i = 0; i < parameters.Length; i++)
            {
                sb.Append($"{parameters[i].ParameterType.Name} {parameters[i].Name}");
                if (i < parameters.Length - 1) sb.Append(", ");
            }
            sb.Append(")");
            return sb.ToString();
        }

        private List<ApiParameterDoc> GetParameters(MethodInfo method)
        {
            return method.GetParameters().Select(p => new ApiParameterDoc
            {
                Name = p.Name,
                Type = p.ParameterType.Name,
                Description = "Parameter description placeholder."
            }).ToList();
        }
    }
}