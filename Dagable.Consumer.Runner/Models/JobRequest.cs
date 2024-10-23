namespace Dagable.Consumer.Runner.Models
{
    public class JobRequest
    {
        public Guid RequestGuid { get; set; }
        public Guid UserGuid { get; set; }
        public int GraphCount { get; set; }
        public bool IncludeCP { get; set; }
        public GraphSettings GraphSettings { get; set; }
    }

    public class GraphSettings
    {
        public int MinLayer { get; set; }
        public int MaxLayer { get; set; }
        public int MinNodes { get; set; }
        public int MaxNodes { get; set; }
        public int MaxComm { get; set; }
        public int MinComm { get; set; }
        public int MaxComp { get; set; }
        public int MinComp { get; set; }
        public int MaxProcessors { get; set; }
        public int MinProcessors { get; set; }

    }
}
