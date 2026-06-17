using System.Collections.Generic;

namespace MESharp.Models
{
    public class ApiPropertyDoc
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsStatic { get; set; }
        public bool IsReadOnly { get; set; }
        public string Summary { get; set; }
        public List<ApiExampleDoc> Examples { get; set; } = new List<ApiExampleDoc>();
		public string Category { get; internal set; }
	}
}
