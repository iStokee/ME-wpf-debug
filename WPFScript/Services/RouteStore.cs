using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var loadedRoutes = new List<RouteDefinition>();

            try
            {
                if (!File.Exists(RoutesFile))
                {
                    return MergeWithCoreRoutes(loadedRoutes);
                }

                var json = File.ReadAllText(RoutesFile);
                var routes = JsonSerializer.Deserialize<List<RouteDefinition>>(json);
                if (routes == null)
                {
                    return MergeWithCoreRoutes(loadedRoutes);
                }

                foreach (var route in routes)
                {
                    route?.Normalize();
                }

                loadedRoutes = routes
                    .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Name))
                    .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                loadedRoutes = new List<RouteDefinition>();
            }

            return MergeWithCoreRoutes(loadedRoutes);
        }

        public static void Save(IEnumerable<RouteDefinition> routes)
        {
            try
            {
                Directory.CreateDirectory(RoutesDirectory);
                var normalized = (routes ?? Array.Empty<RouteDefinition>())
                    .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Name))
                    .Select(r =>
                    {
                        r.Normalize();
                        return r;
                    })
                    .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var json = JsonSerializer.Serialize(normalized, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(RoutesFile, json);
            }
            catch
            {
                // Best-effort; UI will report status separately.
            }
        }

        public static string GetStorePath() => RoutesFile;

        private static IReadOnlyList<RouteDefinition> MergeWithCoreRoutes(IEnumerable<RouteDefinition> loadedRoutes)
        {
            var merged = new Dictionary<string, RouteDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (var core in NavigationRouteSeeds.GetCoreRoutes())
            {
                core.Normalize();
                merged[core.Name] = core;
            }

            foreach (var route in loadedRoutes ?? Array.Empty<RouteDefinition>())
            {
                route.Normalize();
                merged[route.Name] = route;
            }

            return merged.Values
                .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
