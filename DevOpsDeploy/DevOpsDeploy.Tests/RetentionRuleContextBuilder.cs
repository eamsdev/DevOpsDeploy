using DevOpsDeploy.Models;
using DevOpsDeploy.Rules.Retention;
using Environment = DevOpsDeploy.Models.Environment;

namespace DevOpsDeploy.Tests;

public class RetentionRuleContextBuilder
{
    private readonly List<Deployment> _deployments = new();
    private readonly List<Environment> _environments = new();
    private readonly List<Project> _projects = new();
    private readonly List<Release> _releases = new();

    public static RetentionRuleContextBuilder Create() => new();
    
    public RetentionRuleContextBuilder WithDeployment(string id, string relId, string envId, DateTime deployedAt)
    {
        _deployments.Add(new Deployment
        {
            Id = id,
            DeployedAt = deployedAt,
            EnvironmentId = envId,
            ReleaseId = relId
        });
        
        return this;
    }
    
    public RetentionRuleContextBuilder WithEnvironment(string id, string name)
    {
        _environments.Add(new Environment
        {
            Id = id,
            Name = name
        });
        
        return this;
    }
    
    public RetentionRuleContextBuilder WithProject(string id, string name)
    {        
        _projects.Add(new Project
        {
            Id = id,
            Name = name
        });
        
        return this;
    }
    
    public RetentionRuleContextBuilder WithRelease(string id, string projectId, string? version, DateTime created)
    {        
        _releases.Add(new Release
        {
            Id = id,
            ProjectId = projectId,
            Version = version,
            Created = created
        });
        
        return this;
    }

    public RetentionRule.Context Build() => new(_projects, _releases, _deployments, _environments);
}