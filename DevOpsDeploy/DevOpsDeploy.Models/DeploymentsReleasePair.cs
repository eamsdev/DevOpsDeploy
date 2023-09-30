namespace DevOpsDeploy.Models;

public record DeploymentsReleasePair(IEnumerable<Deployment> Deployments, Release Release);