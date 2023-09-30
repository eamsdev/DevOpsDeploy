using DevOpsDeploy.Rules.Retention;
using DevOpsDeploy.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace DevOpsDeploy.Tests;

public class RetentionRuleTests
{
    private readonly Func<RetentionRuleContext, RetentionRule> _retentionRuleFactory;
    
    public RetentionRuleTests(ITestOutputHelper testOutputHelper)
    {
        _retentionRuleFactory = ctx => new RetentionRule(ctx, GetLogger(testOutputHelper));
    }

    [Fact]
    public void RetainOneMostRecentFromNoDeployment()
    {
        // Given
        const int releaseToKeep = 1;
        var context = new RetentionRuleContextBuilder()
            .Build();
        
        var retentionRule = _retentionRuleFactory(context);

        // When
        var releases = retentionRule.GetReleasesToKeep(releaseToKeep).ToList();

        // Then 
        releases.Count.Should().Be(0);
    }
    
    [Fact]
    public void RetainOneMostRecentFromOneDeployedRelease()
    {
        // Given
        const int releaseToKeep = 1;
        var context = new RetentionRuleContextBuilder()
            .WithEnvironment("Environment-1", "Staging")
            .WithProject("Project-1", "Random Quotes")
            .WithRelease("Release-1", "Project-1", "1.0.0")
            .WithDeployment("Deployment-1", "Release-1", "Environment-1")
            .Build();
        
        var retentionRule = _retentionRuleFactory(context);

        // When
        var releases = retentionRule.GetReleasesToKeep(releaseToKeep).ToList();

        // Then 
        releases.Count.Should().Be(1);
        releases.First().Id.Should().Be("Release-1");
    }
    
    [Fact]
    public void RetainOneMostRecentFromTwoDeployedReleasesInASingleEnvironment()
    {
        // Given
        const int releaseToKeep = 1;
        var ctx = new RetentionRuleContextBuilder()
            .WithEnvironment("Environment-1", "Staging")
            .WithProject("Project-1", "Random Quotes")
            .WithRelease("Release-1", "Project-1", "1.0.0")
            .WithRelease("Release-2", "Project-1", "1.0.1")
            .WithDeployment("Deployment-1", "Release-1", "Environment-1", DateTime.UtcNow.AddHours(-1))
            .WithDeployment("Deployment-2", "Release-2", "Environment-1")
            .Build();
        
        var sut = _retentionRuleFactory(ctx);

        // When
        var releases = sut.GetReleasesToKeep(releaseToKeep).ToList();

        // Then 
        releases.Count.Should().Be(1);
        releases.First().Id.Should().Be("Release-2");
    }
    
    [Fact]
    public void RetainOneMostRecentFromTwoDeployedReleasesInDifferentEnvironments()
    {
        // Given
        const int releaseToKeep = 1;
        var ctx = new RetentionRuleContextBuilder()
            .WithEnvironment("Environment-1", "Staging")
            .WithEnvironment("Environment-2", "Production")
            .WithProject("Project-1", "Random Quotes")
            .WithRelease("Release-1", "Project-1", "1.0.0")
            .WithRelease("Release-2", "Project-1", "1.0.1")
            .WithDeployment("Deployment-1", "Release-1", "Environment-1")
            .WithDeployment("Deployment-2", "Release-2", "Environment-2")
            .Build();
        
        var sut = _retentionRuleFactory(ctx);

        // When
        var releases = sut.GetReleasesToKeep(releaseToKeep).ToList();

        // Then 
        releases.Count.Should().Be(2);
        releases.Should().ContainSingle(r => r.Id == "Release-1");
        releases.Should().ContainSingle(r => r.Id == "Release-2");
    }
    
    [Fact]
    public void RetainTwoMostRecentFromTwoDeployedReleasesInASingleEnvironment()
    {
        // Given
        const int releaseToKeep = 2;
        var ctx = new RetentionRuleContextBuilder()
            .WithEnvironment("Environment-1", "Staging")
            .WithProject("Project-1", "Random Quotes")
            .WithRelease("Release-1", "Project-1", "1.0.0")
            .WithRelease("Release-2", "Project-1", "1.0.1")
            .WithDeployment("Deployment-1", "Release-1", "Environment-1", DateTime.UtcNow.AddHours(-1))
            .WithDeployment("Deployment-2", "Release-2", "Environment-1")
            .Build();
        
        var sut = _retentionRuleFactory(ctx);

        // When
        var releases = sut.GetReleasesToKeep(releaseToKeep).ToList();

        // Then 
        releases.Count.Should().Be(2);
        releases.Should().ContainSingle(r => r.Id == "Release-1");
        releases.Should().ContainSingle(r => r.Id == "Release-2");
    }
    
    [Fact]
    public void RetainTwoDistinctMostRecentFromFourDeployedReleasesInASingleEnvironment()
    {
        // Given - the same release was deployed thrice in the same environment
        const int releaseToKeep = 2;
        var ctx = new RetentionRuleContextBuilder()
            .WithEnvironment("Environment-1", "Staging")
            .WithProject("Project-1", "Random Quotes")
            .WithRelease("Release-1", "Project-1", "1.0.0")
            .WithRelease("Release-2", "Project-1", "1.0.1")
            .WithDeployment("Deployment-1", "Release-1", "Environment-1", DateTime.UtcNow.AddHours(-3))
            .WithDeployment("Deployment-2", "Release-2", "Environment-1", DateTime.UtcNow.AddHours(-2))
            .WithDeployment("Deployment-3", "Release-2", "Environment-1", DateTime.UtcNow.AddHours(-1))
            .WithDeployment("Deployment-4", "Release-2", "Environment-1")
            .Build();
        
        var sut = _retentionRuleFactory(ctx);

        // When
        var releases = sut.GetReleasesToKeep(releaseToKeep).ToList();

        // Then 
        releases.Count.Should().Be(2);
        releases.Should().ContainSingle(r => r.Id == "Release-1");
        releases.Should().ContainSingle(r => r.Id == "Release-2");
    }

    private static ILogger<RetentionRule> GetLogger(ITestOutputHelper testOutputHelper)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(new XunitLoggerProvider(testOutputHelper)))
            .BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
        return factory.CreateLogger<RetentionRule>();
    }
}