using System;
using System.Collections.Generic;

namespace MESharp.Models
{
    public class RouteDefinition
    {
        public string Name { get; set; } = string.Empty;
        public List<RouteWaypoint> Waypoints { get; set; } = new();
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

    public class RouteWaypoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public override string ToString() => $"{X},{Y},{Z}";
    }
}
