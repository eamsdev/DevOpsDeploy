namespace DevOpsDeploy.Models
{
    public class Deployment
    {
        public required string Id { get; init; }
        public required string ReleaseId { get; init; }
        public required string EnvironmentId { get; init; }
        public required DateTime DeployedAt { get; init; }
    }
}