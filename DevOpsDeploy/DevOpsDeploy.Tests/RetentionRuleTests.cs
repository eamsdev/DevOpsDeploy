using DevOpsDeploy.Rules.Retention;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace DevOpsDeploy.Tests;

public class RetentionRuleTests
{
    private readonly Func<RetentionRule.Context, RetentionRule> _sutFactory;
    
    public RetentionRuleTests(ITestOutputHelper testOutputHelper)
    {
        _sutFactory = ctx => new RetentionRule(ctx, GetLogger(testOutputHelper));
    }

    [Fact]
    public void RetainOneMostRecentFromOneRelease()
    {
        // Given
        const int releaseToKeep = 1;
        var ctx = new RetentionRuleContextBuilder()
            .WithEnvironment("Environment-1", "Staging")
            .WithProject("Project-1", "Random Quotes")
            .WithRelease("Release-1", "Project-1", "1.0.0", DateTime.UtcNow)
            .WithDeployment("Deployment-1", "Release-1", "Environment-1", DateTime.UtcNow)
            .Build();
        
        var sut = _sutFactory.Invoke(ctx);

        // When
        var releases = sut.GetReleasesToKeep(releaseToKeep).ToList();

        // Then 
        releases.Count.Should().Be(1);
        releases.First().Id.Should().Be("Release-1");
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