using System;
using System.Collections.Generic;
using MESharp.Models;

namespace MESharp.Services
{
    internal static class NavigationRouteSeeds
    {
        public static IReadOnlyList<RouteDefinition> GetCoreRoutes()
        {
            return new[]
            {
                BuildEdgevilleBankToWildyWall(),
                BuildWildyWallToGreenDragons(),
                BuildEdgevilleBankToWildernessMine()
            };
        }

        private static RouteDefinition BuildEdgevilleBankToWildyWall()
        {
            return CreateRoute(
                "core.edgeville.bank_to_wildy_wall",
                "Edgeville",
                "Core banking/travel chain used by dragon, mining, and wilderness scripts.",
                new RouteWaypoint { X = 3092, Y = 3495, Z = 0, AreaRadius = 2, ArrivalDistance = 2, TimeoutMs = 15000, JitterTiles = 1, ChainWhileMoving = true, Label = "edgeville bank" },
                new RouteWaypoint { X = 3109, Y = 3520, Z = 0, AreaRadius = 2, ArrivalDistance = 2, TimeoutMs = 20000, JitterTiles = 1, ChainWhileMoving = true, Label = "wilderness wall inside" });
        }

        private static RouteDefinition BuildWildyWallToGreenDragons()
        {
            return CreateRoute(
                "core.wildy_wall.to_green_dragons",
                "Wilderness",
                "Core wilderness chain to green dragons area.",
                new RouteWaypoint { X = 3103, Y = 3585, Z = 0, AreaRadius = 3, ArrivalDistance = 4, TimeoutMs = 20000, JitterTiles = 1, ChainWhileMoving = true, Label = "north pass" },
                new RouteWaypoint { X = 3048, Y = 3610, Z = 0, AreaRadius = 4, ArrivalDistance = 4, TimeoutMs = 25000, JitterTiles = 1, ChainWhileMoving = true, Label = "mid waypoint" },
                new RouteWaypoint { X = 2979, Y = 3616, Z = 0, AreaRadius = 4, ArrivalDistance = 4, TimeoutMs = 30000, JitterTiles = 1, ChainWhileMoving = true, Label = "green dragons" });
        }

        private static RouteDefinition BuildEdgevilleBankToWildernessMine()
        {
            return CreateRoute(
                "core.edgeville.bank_to_wildy_mine",
                "Mining",
                "Core example chain to wilderness mine from Edgeville bank.",
                new RouteWaypoint { X = 3092, Y = 3495, Z = 0, AreaRadius = 2, ArrivalDistance = 2, TimeoutMs = 15000, JitterTiles = 1, ChainWhileMoving = true, Label = "edgeville bank" },
                new RouteWaypoint { X = 3110, Y = 3520, Z = 0, AreaRadius = 2, ArrivalDistance = 2, TimeoutMs = 18000, JitterTiles = 1, ChainWhileMoving = true, Label = "wildy wall" },
                new RouteWaypoint { X = 3031, Y = 3572, Z = 0, AreaRadius = 4, ArrivalDistance = 4, TimeoutMs = 28000, JitterTiles = 1, ChainWhileMoving = true, Label = "wilderness mine" });
        }

        private static RouteDefinition CreateRoute(string name, string category, string description, params RouteWaypoint[] waypoints)
        {
            var route = new RouteDefinition
            {
                SchemaVersion = RouteDefinition.CurrentSchemaVersion,
                Name = name,
                Category = category,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                SavedAt = DateTime.UtcNow,
                Waypoints = new List<RouteWaypoint>(waypoints ?? Array.Empty<RouteWaypoint>())
            };

            route.Normalize();
            return route;
        }
    }
}
