﻿using DevOpsDeploy.Models;
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
    
    public IEnumerable<(string Project, string Environment)> ProjectVsEnvironmentCombinations 
        => AllProjectIds.SelectMany(projId => AllEnvironmentIds.Select(envId 
            => (Project: projId, Environment: envId)));
    
    public IEnumerable<DeploymentsReleasePair> GetDeploymentsReleasePairForProjAndEnv(
        string projectId, 
        string environmentId)
    {
        return Releases
            .GroupJoin(
                Deployments,
                release => release.Id,
                deployment => deployment.ReleaseId,
                (release, deployments) => new DeploymentsReleasePair(deployments, release))
            .Where(drp => drp.Release.ProjectId == projectId)
            .Select(drp => drp with { Deployments = drp.Deployments.Where(d => d.EnvironmentId == environmentId) });
    }

    private IEnumerable<string> AllProjectIds => Projects.Select(p => p.Id).Distinct();
    
    private IEnumerable<string> AllEnvironmentIds => Environments.Select(p => p.Id).Distinct();

    private IEnumerable<string> AllEnvironmentsIdsFromDeployments => Deployments.Select(d => d.EnvironmentId);
    
    private IEnumerable<string> AllProjectIdsFromReleases => Releases.Select(d => d.ProjectId);
}