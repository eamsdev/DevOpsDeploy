using DevOpsDeploy.Models;
using Environment = DevOpsDeploy.Models.Environment;

namespace DevOpsDeploy.Rules.Retention;

public class RetentionRuleContext
{
    public RetentionRuleContext(
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
    
    public IEnumerable<(string Project, string Environment)> GetProjectVsEnvironmentCombinations()
    {
        return AllProjectIds.SelectMany(projId => AllEnvironmentIds.Select(envId => (Project: projId, Environment: envId)));
    }

    private IEnumerable<string> AllProjectIds => Projects.Select(p => p.Id).Distinct();
    
    private IEnumerable<string> AllEnvironmentIds => Environments.Select(p => p.Id).Distinct();
}