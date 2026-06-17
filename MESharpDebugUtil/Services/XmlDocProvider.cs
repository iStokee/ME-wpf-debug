using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace MESharp.Services
{
    public static class XmlDocProvider
    {
        private static readonly Dictionary<string, XDocument?> _docCache = new(StringComparer.OrdinalIgnoreCase);

        public static string GetSummary(MemberInfo member)
        {
            var element = GetMemberElement(member);
            return GetTagText(element, "summary");
        }

        public static string GetRemarks(MemberInfo member)
        {
            var element = GetMemberElement(member);
            return GetTagText(element, "remarks");
        }

        public static string GetExample(MemberInfo member)
        {
            var element = GetMemberElement(member);
            return GetTagText(element, "example");
        }

        public static string GetParamDoc(MethodInfo method, string paramName)
        {
            var element = GetMemberElement(method);
            if (element == null) return null;

            var param = element.Elements("param")
                .FirstOrDefault(p => string.Equals(p.Attribute("name")?.Value, paramName, StringComparison.Ordinal));
            return GetElementText(param);
        }

        public static string GetReturns(MethodInfo method)
        {
            var element = GetMemberElement(method);
            return GetTagText(element, "returns");
        }

        private static string? GetTagText(XElement? element, string tagName)
        {
            if (element == null) return null;
            return GetElementText(element.Element(tagName));
        }

        private static string? GetElementText(XElement? element)
        {
            if (element == null) return null;

            // Keep it simple: flatten common XML doc tags to readable text.
            var parts = new List<string>();
            foreach (var node in element.Nodes())
            {
                if (node is XText text)
                {
                    parts.Add(text.Value);
                    continue;
                }

                if (node is XElement el)
                {
                    if (el.Name.LocalName == "see")
                    {
                        var cref = el.Attribute("cref")?.Value ?? string.Empty;
                        parts.Add(NormalizeCref(cref));
                        continue;
                    }

                    if (el.Name.LocalName == "paramref")
                    {
                        var name = el.Attribute("name")?.Value ?? string.Empty;
                        parts.Add(name);
                        continue;
                    }

                    parts.Add(el.Value);
                }
            }

            var joined = string.Join(string.Empty, parts);
            return NormalizeWhitespace(joined);
        }

        private static string NormalizeCref(string cref)
        {
            if (string.IsNullOrWhiteSpace(cref)) return string.Empty;
            // cref often looks like "T:Namespace.Type" or "M:..."; strip prefix for readability.
            var idx = cref.IndexOf(':');
            return idx >= 0 && idx < cref.Length - 1 ? cref[(idx + 1)..] : cref;
        }

        private static string NormalizeWhitespace(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var sb = new System.Text.StringBuilder(input.Length);
            var lastWasSpace = false;
            foreach (var ch in input)
            {
                var isSpace = char.IsWhiteSpace(ch);
                if (isSpace)
                {
                    if (!lastWasSpace) sb.Append(' ');
                    lastWasSpace = true;
                    continue;
                }
                sb.Append(ch);
                lastWasSpace = false;
            }
            return sb.ToString().Trim();
        }

        private static XElement? GetMemberElement(MemberInfo member)
        {
            var memberName = GetMemberName(member);
            if (memberName == null) return null;

            var assembly = member is Type t ? t.Assembly : member.DeclaringType?.Assembly;
            if (assembly == null) return null;
            var doc = GetDoc(assembly);
            if (doc == null) return null;

            return doc.Descendants("member")
                      .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);
        }

        private static string? GetMemberName(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.TypeInfo:
                case MemberTypes.NestedType:
                    return $"T:{GetTypeId((Type)member)}";
                case MemberTypes.Property:
                    return GetPropertyId((PropertyInfo)member);
                case MemberTypes.Field:
                    return $"F:{GetTypeId(member.DeclaringType!)}.{member.Name}";
                case MemberTypes.Event:
                    return $"E:{GetTypeId(member.DeclaringType!)}.{member.Name}";
                case MemberTypes.Method:
                    return GetMethodId((MethodInfo)member);
                default:
                    return null;
            }
        }

        private static string GetPropertyId(PropertyInfo prop)
        {
            var typeId = GetTypeId(prop.DeclaringType!);
            var name = prop.Name;

            var indexParams = prop.GetIndexParameters();
            if (indexParams.Length == 0)
            {
                return $"P:{typeId}.{name}";
            }

            var paramIds = string.Join(",", indexParams.Select(p => GetParameterTypeId(p.ParameterType, prop.DeclaringType, null)));
            return $"P:{typeId}.{name}({paramIds})";
        }

        private static string GetMethodId(MethodInfo method)
        {
            var typeId = GetTypeId(method.DeclaringType!);
            var name = method.IsConstructor
                ? (method.IsStatic ? "#cctor" : "#ctor")
                : method.Name;

            if (method.IsGenericMethodDefinition)
            {
                name += $"``{method.GetGenericArguments().Length}";
            }

            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                return $"M:{typeId}.{name}";
            }

            var paramIds = string.Join(",", parameters.Select(p => GetParameterTypeId(p.ParameterType, method.DeclaringType, method)));
            return $"M:{typeId}.{name}({paramIds})";
        }

        // Type IDs follow the compiler XML doc ID format.
        private static string GetTypeId(Type type)
        {
            if (type.IsGenericParameter)
            {
                // Generic type parameter on a type definition.
                return $"`{type.GenericParameterPosition}";
            }

            if (type.IsArray)
            {
                return GetArrayTypeId(type);
            }

            if (type.IsByRef)
            {
                return $"{GetTypeId(type.GetElementType()!)}@";
            }

            if (type.IsPointer)
            {
                return $"{GetTypeId(type.GetElementType()!)}*";
            }

            if (type.IsGenericType)
            {
                if (type.IsGenericTypeDefinition)
                {
                    // Type definitions in XML doc IDs do not include their type argument list.
                    return type.FullName ?? type.Name;
                }

                var def = type.GetGenericTypeDefinition();
                var defName = RemoveGenericArity(def.FullName ?? def.Name);
                var args = type.GetGenericArguments();
                return $"{defName}{{{string.Join(",", args.Select(GetTypeId))}}}";
            }

            // FullName for nested types uses '+', which matches XML doc IDs.
            return type.FullName ?? type.Name;
        }

        private static string RemoveGenericArity(string typeFullName)
        {
            if (string.IsNullOrEmpty(typeFullName)) return typeFullName;

            var sb = new System.Text.StringBuilder(typeFullName.Length);
            for (var i = 0; i < typeFullName.Length; i++)
            {
                var ch = typeFullName[i];
                if (ch != '`')
                {
                    sb.Append(ch);
                    continue;
                }

                // Skip `N where N is one or more digits.
                var j = i + 1;
                while (j < typeFullName.Length && char.IsDigit(typeFullName[j]))
                {
                    j++;
                }
                i = j - 1;
            }
            return sb.ToString();
        }

        private static string GetArrayTypeId(Type arrayType)
        {
            var elementId = GetTypeId(arrayType.GetElementType()!);
            var rank = arrayType.GetArrayRank();
            if (rank == 1)
            {
                return $"{elementId}[]";
            }

            // Multidimensional: [0:,0:,...]
            var dims = string.Join(",", Enumerable.Repeat("0:", rank));
            return $"{elementId}[{dims}]";
        }

        private static string GetParameterTypeId(Type parameterType, Type? declaringType, MethodBase? declaringMethod)
        {
            if (parameterType.IsGenericParameter)
            {
                // Generic method parameter => ``n ; generic type parameter => `n
                var isMethodGeneric = parameterType.DeclaringMethod != null || declaringMethod != null;
                var prefix = isMethodGeneric ? "``" : "`";
                return $"{prefix}{parameterType.GenericParameterPosition}";
            }

            return GetTypeId(parameterType);
        }

        private static XDocument? GetDoc(Assembly assembly)
        {
            var assemblyName = assembly.GetName().Name;
            if (assemblyName != null && _docCache.TryGetValue(assemblyName, out var cached))
            {
                return cached;
            }

            var xmlPath = TryResolveXmlPath(assemblyName ?? string.Empty, assembly);
            if (xmlPath == null)
            {
                Console.WriteLine($"[XmlDocProvider] Documentation not found for '{assemblyName}'.");
                if (assemblyName != null) _docCache[assemblyName] = null;
                return null;
            }

            try
            {
                var doc = XDocument.Load(xmlPath);
                if (assemblyName != null) _docCache[assemblyName] = doc;
                return doc;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[XmlDocProvider] Error loading XML documentation: {ex.Message}");
                if (assemblyName != null) _docCache[assemblyName] = null;
                return null;
            }
        }

        private static string? TryResolveXmlPath(string assemblyName, Assembly assembly)
        {
            // 1) BaseDirectory: matches current behavior (best when XML is copied next to the exe).
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var baseCandidate = Path.Combine(baseDir, $"{assemblyName}.xml");
            if (File.Exists(baseCandidate)) return baseCandidate;

            // 2) Next to referenced assembly (when copied local).
            var assemblyLocation = assembly.Location;
            if (!string.IsNullOrWhiteSpace(assemblyLocation))
            {
                var nextToAsm = Path.ChangeExtension(assemblyLocation, ".xml");
                if (File.Exists(nextToAsm)) return nextToAsm;
            }

            // 3) Explicit override via env vars.
            var docsDir = Environment.GetEnvironmentVariable("MESHARP_DOCS_DIR");
            if (!string.IsNullOrWhiteSpace(docsDir))
            {
                var envCandidate = Path.Combine(docsDir, $"{assemblyName}.xml");
                if (File.Exists(envCandidate)) return envCandidate;
            }

            var buildDllDir = Environment.GetEnvironmentVariable("MESHARP_BUILD_DLL_DIR");
            if (!string.IsNullOrWhiteSpace(buildDllDir))
            {
                var envCandidate = Path.Combine(buildDllDir, $"{assemblyName}.xml");
                if (File.Exists(envCandidate)) return envCandidate;
            }

            // 4) Repo-root probing: find MemoryError.sln and look under ME/x64/Build_DLL.
            var repoRoot = TryFindRepoRoot();
            if (!string.IsNullOrWhiteSpace(repoRoot))
            {
                var repoCandidate = Path.Combine(repoRoot, "ME", "x64", "Build_DLL", $"{assemblyName}.xml");
                if (File.Exists(repoCandidate)) return repoCandidate;
            }

            // 5) Common default path used in this repo's post-build steps (Windows).
            // This is only a fallback and should not be treated as canonical.
            var defaultWin = Path.Combine(@"C:\Development\MemoryError\ME\x64\Build_DLL", $"{assemblyName}.xml");
            if (File.Exists(defaultWin)) return defaultWin;

            return null;
        }

        private static string? TryFindRepoRoot()
        {
            // Prefer working directory when launched from Visual Studio / dev.
            var seeds = new[]
            {
                Directory.GetCurrentDirectory(),
                AppDomain.CurrentDomain.BaseDirectory,
            };

            foreach (var seed in seeds)
            {
                var root = TryFindUpwards(seed, "MemoryError.sln");
                if (root != null) return root;
                root = TryFindUpwards(seed, "AGENTS.md");
                if (root != null) return root;
            }

            return null;
        }

        private static string? TryFindUpwards(string startDir, string markerFile)
        {
            if (string.IsNullOrWhiteSpace(startDir)) return null;

            var dir = new DirectoryInfo(startDir);
            for (var i = 0; i < 12 && dir != null; i++)
            {
                var candidate = Path.Combine(dir.FullName, markerFile);
                if (File.Exists(candidate))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            return null;
        }
    }
}
