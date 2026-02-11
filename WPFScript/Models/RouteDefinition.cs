using System;
using System.Collections.Generic;

namespace MESharp.Models
{
    public class RouteDefinition
    {
        public const int CurrentSchemaVersion = 2;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<RouteWaypoint> Waypoints { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        public void Normalize()
        {
            if (SchemaVersion <= 0)
            {
                SchemaVersion = CurrentSchemaVersion;
            }

            Name ??= string.Empty;
            Description ??= string.Empty;
            Category ??= string.Empty;
            Waypoints ??= new List<RouteWaypoint>();

            if (CreatedAt == default)
            {
                CreatedAt = SavedAt == default ? DateTime.UtcNow : SavedAt;
            }

            if (SavedAt == default)
            {
                SavedAt = DateTime.UtcNow;
            }

            foreach (var waypoint in Waypoints)
            {
                waypoint?.Normalize();
            }
        }
    }

    public class RouteWaypoint
    {
        public string Label { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int AreaRadius { get; set; } = 0;
        public int ArrivalDistance { get; set; } = 2;
        public int TimeoutMs { get; set; } = 8000;
        public int JitterTiles { get; set; } = 1;
        public bool ChainWhileMoving { get; set; } = true;

        public void Normalize()
        {
            Label ??= string.Empty;
            AreaRadius = Math.Clamp(AreaRadius, 0, 25);
            ArrivalDistance = Math.Clamp(ArrivalDistance, 0, 25);
            TimeoutMs = Math.Clamp(TimeoutMs, 1000, 120000);
            JitterTiles = Math.Clamp(JitterTiles, 0, 8);
        }

        public bool IsWithinArea(int x, int y, int z)
        {
            if (z != Z)
            {
                return false;
            }

            var radius = Math.Max(0, AreaRadius);
            return Math.Abs(X - x) <= radius && Math.Abs(Y - y) <= radius;
        }

        public override string ToString()
        {
            var areaTag = AreaRadius > 0 ? $" r{AreaRadius}" : string.Empty;
            var labelTag = string.IsNullOrWhiteSpace(Label) ? string.Empty : $" [{Label}]";
            return $"{X},{Y},{Z}{areaTag}{labelTag}";
        }
    }
}
