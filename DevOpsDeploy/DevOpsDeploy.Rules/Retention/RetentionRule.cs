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
        var deploymentsReleasePairs = _context.Releases.GroupJoin(
            _context.Deployments,
            release => release.Id,
            deployment => deployment.ReleaseId,
            (release, deployments) => new DeploymentsReleasePair(deployments, release)
        ).ToList();

        var allReleasesToKeep = new List<Release>();
        foreach (var (projectId, environmentId) in _context.GetProjectVsEnvironmentCombinations())
        {
            var releases = GetReleasesToKeep(deploymentsReleasePairs, projectId, environmentId, numReleaseToKeep).ToList();
            allReleasesToKeep.AddRange(releases);
            LogRetentionReason(releases, numReleaseToKeep, projectId, environmentId);
        }

        return allReleasesToKeep;
    }

    private static IEnumerable<Release> GetReleasesToKeep(
        IEnumerable<DeploymentsReleasePair> depRelPairs, 
        string projectId,
        string environmentId,
        int numReleaseToKeep)
    {            
        var matchingDepAndRel = depRelPairs
            .Where(dar => dar.Release.ProjectId == projectId)
            .Select(dar => dar with { Deployments = dar.Deployments.Where(d => d.EnvironmentId == environmentId) })
            .Where(HasBeenDeployed);

        var mostRecentDepPerRel = matchingDepAndRel
            .Select(dar => new
            {
                Deployment = dar.Deployments.OrderByDescending(d => d.DeployedAt).First(),
                dar.Release
            })
            .OrderByDescending(dar => dar.Deployment.DeployedAt)
            .Take(numReleaseToKeep);

        return mostRecentDepPerRel.Select(dar => dar.Release);
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
            string.Join(',', releasesToKeep.Select(x => x.Id)),
            releasesToKeep.Count,
            numReleaseToKeep,
            projectId,
            environmentId);
    }

    private record DeploymentsReleasePair(IEnumerable<Deployment> Deployments, Release Release);
}