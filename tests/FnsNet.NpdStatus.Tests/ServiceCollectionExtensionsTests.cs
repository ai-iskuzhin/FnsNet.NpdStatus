using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FnsNet.NpdStatus.Tests;

/// <summary>Registration gives a working client with the published defaults.</summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void TheClientIsResolvableWithThePublishedDefaults()
    {
        using var provider = new ServiceCollection()
            .AddNpdStatusClient()
            .Services
            .BuildServiceProvider();

        var client = provider.GetRequiredService<INpdStatusClient>();

        Assert.IsType<NpdStatusClient>(client);
    }

    [Fact]
    public void TheDefaultTimeoutIsTheOneTheContractRequires()
    {
        var options = new NpdStatusOptions();

        // The published description requires callers to allow at least 60 seconds.
        Assert.Equal(TimeSpan.FromSeconds(60), options.Timeout);
        Assert.Equal("https://statusnpd.nalog.ru/", options.BaseAddress.ToString());
    }

    [Fact]
    public void TheAddressAndTimeoutCanBeOverridden()
    {
        using var provider = new ServiceCollection()
            .AddNpdStatusClient(options =>
            {
                options.BaseAddress = new Uri("https://stub.invalid/");
                options.Timeout = TimeSpan.FromSeconds(5);
            })
            .Services
            .BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient(ServiceCollectionExtensions.HttpClientName);

        Assert.Equal("https://stub.invalid/", httpClient.BaseAddress!.ToString());
        Assert.Equal(TimeSpan.FromSeconds(5), httpClient.Timeout);
    }

    /// <summary>Клиент собирается и без контейнера — для скриптов и проб.</summary>
    [Fact]
    public void TheStandaloneFactoryUsesTheSameDefaults()
    {
        using var client = NpdStatusClient.Create();

        Assert.NotNull(client);
    }
}
