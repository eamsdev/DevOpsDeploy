using DevOpsDeploy.Models;
using Microsoft.Extensions.Logging;

namespace DevOpsDeploy.Rules.Retention;

public class RetentionRule
{
    private readonly RetentionRuleContext _context;
    private readonly ILogger<RetentionRule> _logger;

    public RetentionRule(
        RetentionRuleContext context,
        ILogger<RetentionRule> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IEnumerable<Release> GetReleasesToKeep(int numReleaseToKeep)
    {
        var uniqueReleaseIdsToKeep = new List<string>();
        foreach (var (projectId, environmentId) in _context.ProjectVsEnvironmentCombinations)
        {
            var deploymentsReleasePair =  _context.GetDeploymentsReleasePairForProjAndEnv(projectId, environmentId);
            var releasesToKeep = GetReleasesToKeep(deploymentsReleasePair, numReleaseToKeep);
            
            uniqueReleaseIdsToKeep.AddRange(releasesToKeep.Select(r => r.Id));
            LogRetentionReason(releasesToKeep, numReleaseToKeep, projectId, environmentId);
        }

        return _context.Releases.Where(r => uniqueReleaseIdsToKeep.Distinct().Contains(r.Id));
    }

    private static IReadOnlyCollection<Release> GetReleasesToKeep(
        IEnumerable<DeploymentsReleasePair> depRelPairs,
        int numReleaseToKeep)
    {            
        var mostRecentDeploymentPerRelease = depRelPairs
            .Where(HasBeenDeployed)
            .Select(MostRecentDeploymentReleasePair)
            .OrderByDescending(drp => drp.MostRecentDeployment.DeployedAt)
            .Take(numReleaseToKeep);

        return mostRecentDeploymentPerRelease.Select(drp => drp.Release).ToList();
    }

    private static (Deployment MostRecentDeployment, Release Release) MostRecentDeploymentReleasePair(
        DeploymentsReleasePair drp)
    {
        return new()
        {
            MostRecentDeployment = drp.Deployments.OrderByDescending(d => d.DeployedAt).First(), 
            Release = drp.Release
        };
    }

    private static bool HasBeenDeployed(DeploymentsReleasePair deploymentsReleasePair)
    {
        return deploymentsReleasePair.Deployments.Any();
    }

    private void LogRetentionReason(
        IReadOnlyCollection<Release> releasesToKeep, 
        int numReleaseToKeep, 
        string projectId, 
        string environmentId)
    {
        _logger.LogInformation("Release Ids: '{ReleaseIds}' should be retained, " +
                               "Reason: Most recent {ReleasesToRetainCount} release(s) out of the " +
                               "maximum {MaximumReleasesToRetainCount} release(s) to keep for " +
                               "Project: '{ProjectId}', Environment: '{EnvironmentId}'",
            string.Join(", ", releasesToKeep.Select(x => x.Id)),
            releasesToKeep.Count,
            numReleaseToKeep,
            projectId,
            environmentId);
    }
}