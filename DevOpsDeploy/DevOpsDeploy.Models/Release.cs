namespace DevOpsDeploy.Models
{
    public class Release
    {
        public required string Id { get; init; }
        public required string ProjectId { get; init; }
        public string? Version { get; init; }
        public required DateTime Created { get; init; }
    }
}