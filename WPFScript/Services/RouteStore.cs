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
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string? LastError { get; private set; }

        public static IReadOnlyList<RouteDefinition> Load()
        {
            var loadedRoutes = new List<RouteDefinition>();
            LastError = null;

            try
            {
                if (!File.Exists(RoutesFile))
                {
                    return MergeWithCoreRoutes(loadedRoutes);
                }

                var json = File.ReadAllText(RoutesFile);
                var stored = JsonSerializer.Deserialize<List<WebwalkingStoredRoute>>(json);
                if (stored == null)
                {
                    return MergeWithCoreRoutes(loadedRoutes);
                }

                foreach (var route in stored)
                {
                    route?.Normalize();
                }

                loadedRoutes = stored
                    .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Name))
                    .Select(ConvertFromStored)
                    .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Name))
                    .ToList();
            }
            catch (Exception ex)
            {
                loadedRoutes = new List<RouteDefinition>();
                LastError = $"Route load failed: {ex.Message}";
            }

            return MergeWithCoreRoutes(loadedRoutes);
        }

        public static void Save(IEnumerable<RouteDefinition> routes)
        {
            _ = TrySave(routes, out _);
        }

        public static bool TrySave(IEnumerable<RouteDefinition> routes, out string? error)
        {
            LastError = null;
            error = null;
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
                    .ToList();

                var stored = normalized.Select(ConvertToStored).ToList();
                var json = JsonSerializer.Serialize(stored, JsonOptions);

                var tmpFile = RoutesFile + ".tmp";
                File.WriteAllText(tmpFile, json);

                if (File.Exists(RoutesFile))
                {
                    File.Replace(tmpFile, RoutesFile, null);
                }
                else
                {
                    File.Move(tmpFile, RoutesFile);
                }

                return true;
            }
            catch (Exception ex)
            {
                error = $"Route save failed: {ex.Message}";
                LastError = error;
                try
                {
                    var tmpFile = RoutesFile + ".tmp";
                    if (File.Exists(tmpFile))
                    {
                        File.Delete(tmpFile);
                    }
                }
                catch
                {
                    // ignore cleanup errors
                }

                return false;
            }
        }

        public static string GetStorePath() => RoutesFile;

        private static IReadOnlyList<RouteDefinition> MergeWithCoreRoutes(IEnumerable<RouteDefinition> loadedRoutes)
        {
            var merged = new Dictionary<string, RouteDefinition>(StringComparer.OrdinalIgnoreCase);

            static string BuildKey(RouteDefinition route)
            {
                if (!string.IsNullOrWhiteSpace(route.Id))
                {
                    return $"id:{route.Id.Trim()}";
                }

                return $"name:{route.Name.Trim()}";
            }

            foreach (var core in NavigationRouteSeeds.GetCoreRoutes())
            {
                core.Normalize();
                merged[BuildKey(core)] = core;
            }

            foreach (var route in loadedRoutes ?? Array.Empty<RouteDefinition>())
            {
                route.Normalize();
                merged[BuildKey(route)] = route;
            }

            return merged.Values.ToList();
        }

        private static RouteDefinition ConvertFromStored(WebwalkingStoredRoute route)
        {
            var converted = new RouteDefinition
            {
                SchemaVersion = route.SchemaVersion,
                Id = route.Id,
                Name = route.Name,
                Description = route.Description,
                Category = route.Category,
                IsEnabled = route.IsEnabled,
                Tags = route.Tags?.ToList() ?? new List<string>(),
                CreatedAt = route.CreatedAt,
                SavedAt = route.SavedAt,
                Waypoints = (route.Waypoints ?? new List<WebwalkingStoredWaypoint>())
                    .Select(wp => new RouteWaypoint
                    {
                        Id = wp.Id,
                        Label = wp.Label,
                        X = wp.X,
                        Y = wp.Y,
                        Z = wp.Z,
                        AreaRadius = wp.AreaRadius,
                        ArrivalDistance = wp.ArrivalDistance,
                        TimeoutMs = wp.TimeoutMs,
                        JitterTiles = wp.JitterTiles,
                        ChainWhileMoving = wp.ChainWhileMoving,
                        IsTransition = wp.IsTransition,
                        TransitionObjectIds = wp.TransitionObjectIds?.ToList() ?? new List<int>()
                    })
                    .ToList()
            };

            converted.Normalize();
            return converted;
        }

        private static WebwalkingStoredRoute ConvertToStored(RouteDefinition route)
        {
            var stored = new WebwalkingStoredRoute
            {
                SchemaVersion = route.SchemaVersion,
                Id = route.Id,
                Name = route.Name,
                Description = route.Description,
                Category = route.Category,
                IsEnabled = route.IsEnabled,
                Tags = route.Tags?.ToList() ?? new List<string>(),
                CreatedAt = route.CreatedAt,
                SavedAt = route.SavedAt,
                Waypoints = (route.Waypoints ?? new List<RouteWaypoint>())
                    .Select(wp => new WebwalkingStoredWaypoint
                    {
                        Id = wp.Id,
                        Label = wp.Label,
                        X = wp.X,
                        Y = wp.Y,
                        Z = wp.Z,
                        AreaRadius = wp.AreaRadius,
                        ArrivalDistance = wp.ArrivalDistance,
                        TimeoutMs = wp.TimeoutMs,
                        JitterTiles = wp.JitterTiles,
                        ChainWhileMoving = wp.ChainWhileMoving,
                        IsTransition = wp.IsTransition,
                        TransitionObjectIds = wp.TransitionObjectIds?.ToList() ?? new List<int>()
                    })
                    .ToList()
            };

            stored.Normalize();
            return stored;
        }

        private sealed class WebwalkingStoredRoute
        {
            public const int CurrentSchemaVersion = 3;

            public int SchemaVersion { get; set; } = CurrentSchemaVersion;
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public bool IsEnabled { get; set; } = true;
            public List<string> Tags { get; set; } = new();
            public List<WebwalkingStoredWaypoint> Waypoints { get; set; } = new();
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime SavedAt { get; set; } = DateTime.UtcNow;

            public void Normalize()
            {
                if (SchemaVersion <= 0)
                {
                    SchemaVersion = CurrentSchemaVersion;
                }

                Id ??= string.Empty;
                Name ??= string.Empty;
                Description ??= string.Empty;
                Category ??= string.Empty;
                Tags ??= new List<string>();
                Waypoints ??= new List<WebwalkingStoredWaypoint>();

                if (string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(Name))
                {
                    Id = Name.Trim().ToLowerInvariant().Replace(' ', '_');
                }

                if (CreatedAt == default)
                {
                    CreatedAt = SavedAt == default ? DateTime.UtcNow : SavedAt;
                }

                if (SavedAt == default)
                {
                    SavedAt = DateTime.UtcNow;
                }

                foreach (var wp in Waypoints)
                {
                    wp?.Normalize();
                }
            }
        }

        private sealed class WebwalkingStoredWaypoint
        {
            public string Id { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
            public int AreaRadius { get; set; } = 0;
            public int ArrivalDistance { get; set; } = 2;
            public int TimeoutMs { get; set; } = 8000;
            public int JitterTiles { get; set; } = 1;
            public bool ChainWhileMoving { get; set; } = true;
            public bool IsTransition { get; set; }
            public List<int> TransitionObjectIds { get; set; } = new();

            public void Normalize()
            {
                Id ??= string.Empty;
                Label ??= string.Empty;
                AreaRadius = Math.Clamp(AreaRadius, 0, 25);
                ArrivalDistance = Math.Clamp(ArrivalDistance, 0, 25);
                TimeoutMs = Math.Clamp(TimeoutMs <= 0 ? 8000 : TimeoutMs, 1000, 180000);
                JitterTiles = Math.Clamp(JitterTiles, 0, 8);
                TransitionObjectIds ??= new List<int>();
                TransitionObjectIds = TransitionObjectIds.Where(i => i > 0).Distinct().ToList();

                if (string.IsNullOrWhiteSpace(Id))
                {
                    Id = Guid.NewGuid().ToString("N");
                }
            }
        }
    }
}
