namespace OPC.DSClient.WinForm.UserControl
{
   
    public class JsonDsSystem
    {
        public string Name { get; set; } = string.Empty;
        public List<Flow> Flows { get; set; } = new();
    }

    public class Flow
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<Vertex> Vertices { get; set; } = new();
        public List<Edge> Edges { get; set; } = new();
        public List<Alias> Aliases { get; set; } = new();
    }

    public class Vertex
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<Vertex> Vertices { get; set; } = new(); // 하위 Vertex
        public List<Edge> Edges { get; set; } = new(); // 하위 Edge
    }

    public class Edge
    {
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
    }

    public class Alias
    {
        public string AliasKey { get; set; } = string.Empty;
        public List<string> Texts { get; set; } = new();
    }
}
