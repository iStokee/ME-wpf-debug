using System.Collections.Generic;

namespace MESharp.Models
{
    public class ApiDocSection
    {
        public string Title { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public string Details { get; init; } = string.Empty;

        public static IEnumerable<ApiDocSection> DefaultSections()
        {
            yield return new ApiDocSection
            {
                Title = "Getting Started",
                Summary = "Overview of prerequisites and how to reference the interop layer from scripts.",
                Details = "TODO: Provide step-by-step setup instructions, screenshots, and troubleshooting tips."
            };

            yield return new ApiDocSection
            {
                Title = "Core Concepts",
                Summary = "Explain the architecture, threading model, and safety guidelines when calling into the client.",
                Details = "TODO: Document execution flow, threading expectations, and reference examples."
            };

            yield return new ApiDocSection
            {
                Title = "API Reference",
                Summary = "Detailed reference for namespaces, classes, and methods exposed to scripts.",
                Details = "TODO: Generate per-class documentation, parameter tables, and usage snippets."
            };

            yield return new ApiDocSection
            {
                Title = "Examples & Recipes",
                Summary = "Sample scripts that demonstrate common automation workflows.",
                Details = "TODO: Link to runnable samples, describe expected outcomes, and highlight best practices."
            };
        }
    }
}
