
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
        private static readonly Dictionary<string, XDocument> _docCache = new Dictionary<string, XDocument>();

        public static string GetSummary(MemberInfo member)
        {
            var element = GetMemberElement(member);
            return element?.Element("summary")?.Value.Trim();
        }

        public static string GetRemarks(MemberInfo member)
        {
            var element = GetMemberElement(member);
            return element?.Element("remarks")?.Value.Trim();
        }

        public static string GetExample(MemberInfo member)
        {
            var element = GetMemberElement(member);
            return element?.Element("example")?.Value.Trim();
        }

        public static string GetParamDoc(MethodInfo method, string paramName)
        {
            var element = GetMemberElement(method);
            return element?.Elements("param")
                          .FirstOrDefault(p => p.Attribute("name")?.Value == paramName)?.Value.Trim();
        }

        private static XElement GetMemberElement(MemberInfo member)
        {
            var memberName = GetMemberName(member);
            if (memberName == null) return null;

            var assembly = member.DeclaringType.Assembly;
            var doc = GetDoc(assembly);
            if (doc == null) return null;

            return doc.Descendants("member")
                      .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);
        }

        private static string GetMemberName(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.TypeInfo:
                case MemberTypes.NestedType:
                    return $"T:{((Type)member).FullName}";
                case MemberTypes.Property:
                    return $"P:{member.DeclaringType.FullName}.{member.Name}";
                case MemberTypes.Method:
                    var method = (MethodInfo)member;
                    var parameters = method.GetParameters().Select(p => p.ParameterType.FullName.Replace("&", "@"));
                    return $"M:{member.DeclaringType.FullName}.{member.Name}({string.Join(",", parameters)})";
                default:
                    return null;
            }
        }

        private static XDocument GetDoc(Assembly assembly)
        {
            var assemblyName = assembly.GetName().Name;
            if (_docCache.ContainsKey(assemblyName))
            {
                return _docCache[assemblyName];
            }

            var xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{assemblyName}.xml");
            if (!File.Exists(xmlPath))
            {
                Console.WriteLine($"[XmlDocProvider] Documentation not found at {xmlPath}");
                return null;
            }

            try
            {
                var doc = XDocument.Load(xmlPath);
                _docCache[assemblyName] = doc;
                return doc;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[XmlDocProvider] Error loading XML documentation: {ex.Message}");
                return null;
            }
        }
    }
}
