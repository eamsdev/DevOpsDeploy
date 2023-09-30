using DevOpsDeploy.Models;
using Microsoft.Extensions.Logging;
using Environment = DevOpsDeploy.Models.Environment;

namespace DevOpsDeploy.Rules.Retention;

public class RetentionRule
{
    private readonly Context _context;
    private readonly ILogger<RetentionRule> _logger;

    public RetentionRule(
        Context context,
        ILogger<RetentionRule> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IEnumerable<Release> GetReleasesToKeep(int numReleaseToKeep)
    {
        var projectVsEnvironmentCombination =
            _context.AllProjectIds.SelectMany(p => _context.AllEnvironmentIds.Select(e => (p, e)));
        
        var deploymentsReleasePairs = _context.Releases.GroupJoin(
            _context.Deployments,
            release => release.Id,
            deployment => deployment.ReleaseId,
            (release, deployments) => new DeploymentsReleasePair(deployments, release)
        ).ToList();

        var allReleasesToKeep = new List<Release>();
        foreach (var (projectId, environmentId) in projectVsEnvironmentCombination)
        {
            var releases = GetReleasesToKeep(deploymentsReleasePairs, projectId, environmentId).ToList();
            allReleasesToKeep.AddRange(releases);
            LogRetentionReason(releases, numReleaseToKeep, projectId, environmentId);
        }

        return allReleasesToKeep;
    }

    private static IEnumerable<Release> GetReleasesToKeep(
        IEnumerable<DeploymentsReleasePair> depRelPairs, 
        string projectId,
        string environmentId)
    {            
        var matchingDepAndRel = depRelPairs
            .Where(dar => dar.Release.ProjectId == projectId)
            .Select(dar => dar with { Deployments = dar.Deployments.Where(d => d.EnvironmentId == environmentId) })
            .Where(HasBeenDeployed);

        // Assume we are keeping most recent *DISTINCT* n-releases 
        var mostRecentDepPerRel = matchingDepAndRel
            .Select(dar => new
            {
                Deployment = dar.Deployments.OrderByDescending(d => d.DeployedAt).First(),
                dar.Release
            });

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

    public class Context
    {
        public Context(
            List<Project> projects,
            List<Release> releases,
            List<Deployment> deployments,
            List<Environment> environments)
        {
            Projects = projects;
            Releases = releases;
            Deployments = deployments;
            Environments = environments;
        }
        
        public List<Project> Projects { get; }
        public List<Release> Releases { get; }
        public List<Deployment> Deployments { get; }
        public List<Environment> Environments { get; }
        public IEnumerable<string> AllProjectIds => Projects.Select(p => p.Id).Distinct();
        public IEnumerable<string> AllEnvironmentIds => Environments.Select(p => p.Id).Distinct();
    }

    private record DeploymentsReleasePair(IEnumerable<Deployment> Deployments, Release Release);
}