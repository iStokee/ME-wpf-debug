using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MESharp.Models;

namespace MESharp.Services
{
    internal static class RouteStore
    {
        private static readonly string RoutesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MESharp");
        private static readonly string RoutesFile = Path.Combine(RoutesDirectory, "routes.json");

        public static IReadOnlyList<RouteDefinition> Load()
        {
            try
            {
                if (!File.Exists(RoutesFile))
                {
                    return Array.Empty<RouteDefinition>();
                }

                var json = File.ReadAllText(RoutesFile);
                var routes = JsonSerializer.Deserialize<List<RouteDefinition>>(json);
                if (routes == null)
                {
                    return Array.Empty<RouteDefinition>();
                }
                return routes;
            }
            catch
            {
                return Array.Empty<RouteDefinition>();
            }
        }

        public static void Save(IEnumerable<RouteDefinition> routes)
        {
            try
            {
                Directory.CreateDirectory(RoutesDirectory);
                var json = JsonSerializer.Serialize(routes, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(RoutesFile, json);
            }
            catch
            {
                // Best-effort; UI will report status separately.
            }
        }

        public static string GetStorePath() => RoutesFile;
    }
}
