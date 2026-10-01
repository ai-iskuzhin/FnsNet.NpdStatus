using System.Net;
using System.Text.Json;
using Xunit;

namespace FnsNet.NpdStatus.Tests;

/// <summary>
/// What the client sends, and what it makes of what comes back.
/// </summary>
/// <remarks>
/// The sample bodies are the ones the published description gives, and the ones the live service
/// actually returned when this client was written — including that the description's own example INN
/// now fails the service's checksum validation.
/// </remarks>
public class NpdStatusClientTests
{
    private static readonly DateOnly Date = new(2026, 10, 1);

    [Fact]
    public async Task ARegisteredTaxpayerIsReported()
    {
        var (client, _) = StubHandler.Create(
            HttpStatusCode.OK,
            """{"status":true,"message":"525741209968 является плательщиком налога на профессиональный доход"}""");

        var status = await client.GetStatusAsync("525741209968", Date);

        Assert.True(status.IsRegistered);
        Assert.Equal("525741209968", status.Inn);
        Assert.Equal(Date, status.Date);
        Assert.Contains("является плательщиком", status.Message!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnUnregisteredTaxpayerIsReported()
    {
        var (client, _) = StubHandler.Create(
            HttpStatusCode.OK,
            """{"status":false,"message":"123456789047 не является плательщиком налога на профессиональный доход"}""");

        var status = await client.GetStatusAsync("123456789047", Date);

        Assert.False(status.IsRegistered);
    }

    [Fact]
    public async Task TheRequestMatchesThePublishedShape()
    {
        var (client, handler) = StubHandler.Create(HttpStatusCode.OK, """{"status":true}""");

        await client.GetStatusAsync("  525741209968  ", new DateOnly(2019, 1, 11));

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal("https://statusnpd.nalog.ru/api/v1/tracker/taxpayer_status", handler.LastRequestUri!.ToString());

        using var sent = JsonDocument.Parse(handler.LastRequestBody!);
        // Trimmed, because a copied INN arrives with whitespace more often than not.
        Assert.Equal("525741209968", sent.RootElement.GetProperty("inn").GetString());
        // The field is camelCase and the date is plain yyyy-MM-dd, not a timestamp.
        Assert.Equal("2019-01-11", sent.RootElement.GetProperty("requestDate").GetString());
    }

    /// <summary>
    /// The contract says the service may add fields without raising the version, and that clients
    /// must ignore them. A client that fails on an unknown field breaks on a day nobody deployed.
    /// </summary>
    [Fact]
    public async Task UnknownResponseFieldsAreIgnored()
    {
        var (client, _) = StubHandler.Create(
            HttpStatusCode.OK,
            """{"status":true,"message":"ok","someFutureField":{"nested":[1,2,3]},"anotherOne":42}""");

        var status = await client.GetStatusAsync("525741209968", Date);

        Assert.True(status.IsRegistered);
    }

    [Theory]
    [InlineData(NpdStatusErrorCodes.ValidationFailed, "Указан некорректный ИНН", false)]
    [InlineData(NpdStatusErrorCodes.ServiceUnavailable, "Сервис временно недоступен", true)]
    [InlineData(NpdStatusErrorCodes.ServiceLimited, "Превышено количество запросов", true)]
    public async Task ABusinessErrorCarriesItsCode(string code, string message, bool transient)
    {
        var (client, _) = StubHandler.Create(
            HttpStatusCode.UnprocessableEntity,
            $$"""{"code":"{{code}}","message":"{{message}}"}""");

        var exception = await Assert.ThrowsAsync<NpdStatusException>(
            () => client.GetStatusAsync("525741209968", Date));

        Assert.Equal(code, exception.Code);
        Assert.Equal(message, exception.ServiceMessage);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, exception.StatusCode);
        // Whether asking again could help is the whole point of telling the codes apart.
        Assert.Equal(transient, exception.IsTransient);
    }

    /// <summary>
    /// A 400 means the body never parsed, so it carries no code — and the client must still say
    /// something useful rather than fall over trying to read one.
    /// </summary>
    [Fact]
    public async Task AMalformedRequestHasNoCode()
    {
        var (client, _) = StubHandler.Create(HttpStatusCode.BadRequest, "Bad Request", "text/plain");

        var exception = await Assert.ThrowsAsync<NpdStatusException>(
            () => client.GetStatusAsync("525741209968", Date));

        Assert.Null(exception.Code);
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("400", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnInternalErrorIsTransient()
    {
        var (client, _) = StubHandler.Create(HttpStatusCode.InternalServerError, string.Empty, "text/plain");

        var exception = await Assert.ThrowsAsync<NpdStatusException>(
            () => client.GetStatusAsync("525741209968", Date));

        Assert.True(exception.IsTransient);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnEmptyInnIsRejectedBeforeTheCall(string? inn)
    {
        var (client, handler) = StubHandler.Create(HttpStatusCode.OK, """{"status":true}""");

        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.GetStatusAsync(inn!, Date));

        // Not sent: the service allows two requests a minute, and none of them should be spent
        // asking about nothing.
        Assert.Null(handler.LastRequestUri);
    }
}
