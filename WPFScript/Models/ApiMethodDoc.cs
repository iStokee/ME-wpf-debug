using System.Collections.Generic;

namespace MESharp.Models
{
    public class ApiMethodDoc
    {
        public string Name { get; set; }
        public string ParametersDisplay { get; set; }
        public string ReturnType { get; set; }
        public bool IsStatic { get; set; }
        public string Summary { get; set; }
        public List<ApiParameterDoc> Parameters { get; set; } = new List<ApiParameterDoc>();
        public List<ApiExampleDoc> Examples { get; set; } = new List<ApiExampleDoc>();
		public string Category { get; internal set; }
		public string Signature { get; internal set; }
		public string ReturnDescription { get; internal set; }
	}
}