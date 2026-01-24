using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace MESharp.ViewModels
{
    public class ApiDocsViewModel : INotifyPropertyChanged, IActivatableViewModel
    {
        public ObservableCollection<ApiClassInfo> ApiClasses { get; private set; }

        private ApiClassInfo _selectedClass;
        public ApiClassInfo SelectedClass
        {
            get => _selectedClass;
            set
            {
                if (SetProperty(ref _selectedClass, value))
                {
                    if (value != null)
                    {
                        SelectedClassDocumentation = new ApiClassDocumentationViewModel(value.ClassType);
                    }
                }
            }
        }

        private ApiClassDocumentationViewModel _selectedClassDocumentation;
        public ApiClassDocumentationViewModel SelectedClassDocumentation
        {
            get => _selectedClassDocumentation;
            set => SetProperty(ref _selectedClassDocumentation, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterClasses();
                }
            }
        }

        private List<ApiClassInfo> _allClasses;

        public ApiDocsViewModel()
        {
            ApiClasses = new ObservableCollection<ApiClassInfo>();
            LoadApiClasses();
        }

        private void LoadApiClasses()
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "csharp_interop", StringComparison.OrdinalIgnoreCase));

                if (assembly == null)
                {
                    var assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "csharp_interop.dll");
                    if (!File.Exists(assemblyPath))
                    {
                        Console.WriteLine($"[ApiDocs] csharp_interop.dll not found at {assemblyPath}");
                        _allClasses = new List<ApiClassInfo>();
                        return;
                    }

                    assembly = Assembly.LoadFrom(assemblyPath);
                }

                _allClasses = assembly.GetTypes()
                    .Where(t => t.IsPublic && t.IsClass && t.Namespace == "MESharp.API")
                    .OrderBy(t => t.Name)
                    .Select(t => new ApiClassInfo
                    {
                        ClassType = t,
                        Name = t.Name,
                        Namespace = t.Namespace ?? "Unknown"
                    })
                    .ToList();

                FilterClasses();

                // Auto-select Inventory as default
                var inventoryClass = ApiClasses.FirstOrDefault(c => c.Name == "Inventory");
                if (inventoryClass != null)
                {
                    SelectedClass = inventoryClass;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiDocs] Error loading API classes: {ex.Message}");
                _allClasses = new List<ApiClassInfo>();
            }
        }

        private void FilterClasses()
        {
            ApiClasses.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allClasses
                : _allClasses.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var classInfo in filtered)
            {
                ApiClasses.Add(classInfo);
            }
        }

        public void OnActivated()
        {
        }

        public void OnDeactivated()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, newValue)) return false;
            field = newValue;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }

    public class ApiClassInfo
    {
        public Type ClassType { get; init; }
        public string Name { get; init; }
        public string Namespace { get; init; }
    }
}
