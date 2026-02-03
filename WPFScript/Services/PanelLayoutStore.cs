using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MESharp.Services
{
    public static class PanelLayoutStore
    {
        private static readonly object SyncRoot = new object();
        private static readonly string LayoutDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MESharp",
            "WPFScript");
        private static readonly string LayoutFilePath = Path.Combine(LayoutDirectory, "panel-layouts.json");

        private static Dictionary<string, List<string>>? _layouts;

        public static IReadOnlyList<string> GetOrder(string pageKey)
        {
            if (string.IsNullOrWhiteSpace(pageKey))
            {
                return Array.Empty<string>();
            }

            lock (SyncRoot)
            {
                EnsureLoaded();
                if (_layouts != null && _layouts.TryGetValue(pageKey, out var order))
                {
                    return order.ToList();
                }
            }

            return Array.Empty<string>();
        }

        public static void SaveOrder(string pageKey, IEnumerable<string> orderedPanelKeys)
        {
            if (string.IsNullOrWhiteSpace(pageKey))
            {
                return;
            }

            var keys = orderedPanelKeys?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .ToList() ?? new List<string>();

            lock (SyncRoot)
            {
                EnsureLoaded();
                _layouts![pageKey] = keys;
                Persist();
            }
        }

        public static void RemoveOrder(string pageKey)
        {
            if (string.IsNullOrWhiteSpace(pageKey))
            {
                return;
            }

            lock (SyncRoot)
            {
                EnsureLoaded();
                if (_layouts != null && _layouts.Remove(pageKey))
                {
                    Persist();
                }
            }
        }

        private static void EnsureLoaded()
        {
            if (_layouts != null)
            {
                return;
            }

            try
            {
                if (!File.Exists(LayoutFilePath))
                {
                    _layouts = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                    return;
                }

                var json = File.ReadAllText(LayoutFilePath);
                _layouts = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)
                           ?? new Dictionary<string, List<string>>(StringComparer.Ordinal);
            }
            catch
            {
                _layouts = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            }
        }

        private static void Persist()
        {
            try
            {
                Directory.CreateDirectory(LayoutDirectory);
                var json = JsonSerializer.Serialize(_layouts, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(LayoutFilePath, json);
            }
            catch
            {
                // Keep UI responsive; layout persistence failures should never break the app.
            }
        }
    }
}
