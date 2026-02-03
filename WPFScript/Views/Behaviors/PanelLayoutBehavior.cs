using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MESharp.Services;

namespace MESharp.Views.Behaviors
{
    public static class PanelLayoutBehavior
    {
        private const string DragFormat = "MESharp.PanelLayout.PanelKey";

        public static readonly DependencyProperty PageKeyProperty =
            DependencyProperty.RegisterAttached(
                "PageKey",
                typeof(string),
                typeof(PanelLayoutBehavior),
                new PropertyMetadata(null, OnPageKeyChanged));

        public static readonly DependencyProperty PanelKeyProperty =
            DependencyProperty.RegisterAttached(
                "PanelKey",
                typeof(string),
                typeof(PanelLayoutBehavior),
                new PropertyMetadata(null));

        private static readonly DependencyProperty TrackerProperty =
            DependencyProperty.RegisterAttached(
                "Tracker",
                typeof(LayoutTracker),
                typeof(PanelLayoutBehavior),
                new PropertyMetadata(null));

        public static string GetPageKey(DependencyObject obj) => (string)obj.GetValue(PageKeyProperty);
        public static void SetPageKey(DependencyObject obj, string value) => obj.SetValue(PageKeyProperty, value);

        public static string GetPanelKey(DependencyObject obj) => (string)obj.GetValue(PanelKeyProperty);
        public static void SetPanelKey(DependencyObject obj, string value) => obj.SetValue(PanelKeyProperty, value);

        private static void OnPageKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Panel panel)
            {
                return;
            }

            panel.Loaded -= OnPanelLoaded;
            if (!string.IsNullOrWhiteSpace(e.NewValue as string))
            {
                panel.Loaded += OnPanelLoaded;
            }
        }

        private static void OnPanelLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Panel panel)
            {
                return;
            }

            var pageKey = GetPageKey(panel);
            if (string.IsNullOrWhiteSpace(pageKey))
            {
                return;
            }

            var tracker = (LayoutTracker)panel.GetValue(TrackerProperty);
            if (tracker == null)
            {
                tracker = new LayoutTracker
                {
                    PageKey = pageKey,
                    DefaultOrder = GetKeyedChildren(panel).Select(GetPanelKey).ToList()
                };
                panel.SetValue(TrackerProperty, tracker);
            }
            else
            {
                tracker.PageKey = pageKey;
            }

            ApplySavedOrder(panel, pageKey);
            AttachPanelMenus(panel);
            AttachDragDrop(panel);
        }

        private static void ApplySavedOrder(Panel panel, string pageKey)
        {
            var saved = PanelLayoutStore.GetOrder(pageKey);
            if (saved.Count == 0)
            {
                return;
            }

            var currentKeyed = GetKeyedChildren(panel).ToList();
            if (currentKeyed.Count <= 1)
            {
                return;
            }

            var keyedById = currentKeyed.ToDictionary(GetPanelKey, x => x, StringComparer.Ordinal);
            var ordered = new List<UIElement>();

            foreach (var key in saved)
            {
                if (keyedById.TryGetValue(key, out var child))
                {
                    ordered.Add(child);
                    keyedById.Remove(key);
                }
            }

            ordered.AddRange(currentKeyed.Where(x => keyedById.ContainsKey(GetPanelKey(x))));
            ApplyKeyedOrder(panel, ordered);
        }

        private static void AttachPanelMenus(Panel panel)
        {
            foreach (var child in GetKeyedChildren(panel).OfType<FrameworkElement>())
            {
                if (child.ContextMenu != null)
                {
                    continue;
                }

                var menu = new ContextMenu();

                var moveUp = new MenuItem { Header = "Move Panel Up" };
                moveUp.Click += (_, __) => MovePanel(panel, child, -1);

                var moveDown = new MenuItem { Header = "Move Panel Down" };
                moveDown.Click += (_, __) => MovePanel(panel, child, +1);

                var reset = new MenuItem { Header = "Reset Panel Order" };
                reset.Click += (_, __) => ResetPanelOrder(panel);

                menu.Items.Add(moveUp);
                menu.Items.Add(moveDown);
                menu.Items.Add(new Separator());
                menu.Items.Add(reset);

                menu.Opened += (_, __) =>
                {
                    var keyed = GetKeyedChildren(panel).ToList();
                    var idx = keyed.IndexOf(child);
                    moveUp.IsEnabled = idx > 0;
                    moveDown.IsEnabled = idx >= 0 && idx < keyed.Count - 1;
                };

                child.ContextMenu = menu;
                child.ToolTip = "Drag to reorder or right-click for move actions";
            }
        }

        private static void AttachDragDrop(Panel panel)
        {
            var tracker = (LayoutTracker)panel.GetValue(TrackerProperty);
            if (tracker == null)
            {
                return;
            }

            if (!tracker.MouseEventsAttached)
            {
                panel.PreviewMouseLeftButtonDown += OnPanelPreviewMouseLeftButtonDown;
                panel.PreviewMouseMove += OnPanelPreviewMouseMove;
                tracker.MouseEventsAttached = true;
            }

            foreach (var child in GetKeyedChildren(panel).OfType<FrameworkElement>())
            {
                child.AllowDrop = true;
                child.DragOver -= OnPanelChildDragOver;
                child.Drop -= OnPanelChildDrop;
                child.DragOver += OnPanelChildDragOver;
                child.Drop += OnPanelChildDrop;
            }
        }

        private static void OnPanelPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Panel panel)
            {
                return;
            }

            var tracker = (LayoutTracker)panel.GetValue(TrackerProperty);
            if (tracker == null)
            {
                return;
            }

            tracker.DragStartPoint = e.GetPosition(panel);
            tracker.PendingDragPanelKey = null;

            var clicked = FindAncestorWithPanelKey(e.OriginalSource as DependencyObject);
            if (clicked != null)
            {
                tracker.PendingDragPanelKey = GetPanelKey(clicked);
            }
        }

        private static void OnPanelPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not Panel panel || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var tracker = (LayoutTracker)panel.GetValue(TrackerProperty);
            if (tracker == null || string.IsNullOrWhiteSpace(tracker.PendingDragPanelKey))
            {
                return;
            }

            var pos = e.GetPosition(panel);
            if (Math.Abs(pos.X - tracker.DragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(pos.Y - tracker.DragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            var sourceKey = tracker.PendingDragPanelKey;
            tracker.PendingDragPanelKey = null;

            var data = new DataObject(DragFormat, sourceKey);
            DragDrop.DoDragDrop(panel, data, DragDropEffects.Move);
        }

        private static void OnPanelChildDragOver(object sender, DragEventArgs e)
        {
            if (sender is not FrameworkElement target || target.Parent is not Panel panel)
            {
                return;
            }

            if (!e.Data.GetDataPresent(DragFormat))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            var sourceKey = e.Data.GetData(DragFormat) as string;
            var targetKey = GetPanelKey(target);
            e.Effects = !string.IsNullOrWhiteSpace(sourceKey) && !string.Equals(sourceKey, targetKey, StringComparison.Ordinal)
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        private static void OnPanelChildDrop(object sender, DragEventArgs e)
        {
            if (sender is not FrameworkElement target || target.Parent is not Panel panel)
            {
                return;
            }

            if (!e.Data.GetDataPresent(DragFormat))
            {
                return;
            }

            var sourceKey = e.Data.GetData(DragFormat) as string;
            var targetKey = GetPanelKey(target);
            if (string.IsNullOrWhiteSpace(sourceKey) || string.IsNullOrWhiteSpace(targetKey) ||
                string.Equals(sourceKey, targetKey, StringComparison.Ordinal))
            {
                return;
            }

            var keyed = GetKeyedChildren(panel).ToList();
            var source = keyed.FirstOrDefault(x => string.Equals(GetPanelKey(x), sourceKey, StringComparison.Ordinal));
            var dropTarget = keyed.FirstOrDefault(x => string.Equals(GetPanelKey(x), targetKey, StringComparison.Ordinal));
            if (source == null || dropTarget == null)
            {
                return;
            }

            keyed.Remove(source);
            var targetIndex = keyed.IndexOf(dropTarget);
            if (targetIndex < 0)
            {
                return;
            }

            // Drop below the midpoint to place after, above midpoint to place before.
            var dropPos = e.GetPosition(target);
            if (dropPos.Y > target.ActualHeight / 2d)
            {
                targetIndex++;
            }

            if (targetIndex < 0)
            {
                targetIndex = 0;
            }
            if (targetIndex > keyed.Count)
            {
                targetIndex = keyed.Count;
            }

            keyed.Insert(targetIndex, source);
            ApplyKeyedOrder(panel, keyed);
            SaveCurrentOrder(panel);
            e.Handled = true;
        }

        private static void MovePanel(Panel panel, UIElement child, int delta)
        {
            var keyed = GetKeyedChildren(panel).ToList();
            var currentIndex = keyed.IndexOf(child);
            if (currentIndex < 0)
            {
                return;
            }

            var targetIndex = currentIndex + delta;
            if (targetIndex < 0 || targetIndex >= keyed.Count)
            {
                return;
            }

            (keyed[currentIndex], keyed[targetIndex]) = (keyed[targetIndex], keyed[currentIndex]);
            ApplyKeyedOrder(panel, keyed);
            SaveCurrentOrder(panel);
        }

        private static void ResetPanelOrder(Panel panel)
        {
            var tracker = (LayoutTracker)panel.GetValue(TrackerProperty);
            if (tracker == null || tracker.DefaultOrder.Count == 0)
            {
                return;
            }

            var currentByKey = GetKeyedChildren(panel).ToDictionary(GetPanelKey, x => x, StringComparer.Ordinal);
            var ordered = new List<UIElement>();

            foreach (var key in tracker.DefaultOrder)
            {
                if (currentByKey.TryGetValue(key, out var child))
                {
                    ordered.Add(child);
                    currentByKey.Remove(key);
                }
            }

            ordered.AddRange(currentByKey.Values);
            ApplyKeyedOrder(panel, ordered);
            PanelLayoutStore.RemoveOrder(tracker.PageKey);
        }

        private static void SaveCurrentOrder(Panel panel)
        {
            var pageKey = GetPageKey(panel);
            if (string.IsNullOrWhiteSpace(pageKey))
            {
                return;
            }

            var keys = GetKeyedChildren(panel).Select(GetPanelKey);
            PanelLayoutStore.SaveOrder(pageKey, keys);
        }

        private static IEnumerable<UIElement> GetKeyedChildren(Panel panel)
        {
            foreach (UIElement child in panel.Children)
            {
                if (!string.IsNullOrWhiteSpace(GetPanelKey(child)))
                {
                    yield return child;
                }
            }
        }

        private static FrameworkElement? FindAncestorWithPanelKey(DependencyObject? start)
        {
            var current = start;
            while (current != null)
            {
                if (current is FrameworkElement fe && !string.IsNullOrWhiteSpace(GetPanelKey(fe)))
                {
                    return fe;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static void ApplyKeyedOrder(Panel panel, IReadOnlyList<UIElement> orderedKeyed)
        {
            if (orderedKeyed.Count == 0)
            {
                return;
            }

            var currentChildren = panel.Children.Cast<UIElement>().ToList();
            int keyedIdx = 0;
            panel.Children.Clear();

            foreach (var child in currentChildren)
            {
                if (!string.IsNullOrWhiteSpace(GetPanelKey(child)))
                {
                    panel.Children.Add(orderedKeyed[keyedIdx++]);
                }
                else
                {
                    panel.Children.Add(child);
                }
            }
        }

        private sealed class LayoutTracker
        {
            public string PageKey { get; set; } = string.Empty;
            public List<string> DefaultOrder { get; set; } = new();
            public bool MouseEventsAttached { get; set; }
            public Point DragStartPoint { get; set; }
            public string? PendingDragPanelKey { get; set; }
        }
    }
}
