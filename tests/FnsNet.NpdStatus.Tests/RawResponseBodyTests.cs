using System.Net;
using Xunit;

namespace FnsNet.NpdStatus.Tests;

/// <summary>
/// Что остаётся от ответа, когда нужно разобраться, что именно ответил сервис.
/// </summary>
/// <remarks>
/// Тело успешного ответа хранится только по флагу: в нём ИНН, то есть персональные данные.
/// Тело ответа с ошибкой — всегда: ошибка приходит редко, и без неё в обращении в поддержку нечего
/// показать, особенно когда код незнакомый или его нет вовсе.
/// </remarks>
public class RawResponseBodyTests
{
    private const string Body =
        """{"status":true,"message":"525741209968 является плательщиком налога на профессиональный доход"}""";

    private static readonly DateOnly Date = new(2026, 10, 1);

    [Fact]
    public async Task TheSuccessfulBodyIsNotKeptByDefault()
    {
        var (client, _) = StubHandler.Create(HttpStatusCode.OK, Body);

        var status = await client.GetStatusAsync("525741209968", Date);

        Assert.Null(status.RawResponseBody);
    }

    [Fact]
    public async Task TheSuccessfulBodyIsKeptWhenAskedFor()
    {
        var (client, _) = StubHandler.Create(
            HttpStatusCode.OK, Body, options: new NpdStatusOptions { CaptureRawResponseBody = true });

        var status = await client.GetStatusAsync("525741209968", Date);

        Assert.Equal(Body, status.RawResponseBody);
    }

    [Fact]
    public async Task AnErrorBodyIsAlwaysKept()
    {
        const string error = """{"code":"validation.failed","message":"Указан некорректный ИНН"}""";
        var (client, _) = StubHandler.Create(HttpStatusCode.UnprocessableEntity, error);

        var exception = await Assert.ThrowsAsync<NpdStatusException>(
            () => client.GetStatusAsync("525741209968", Date));

        // Без флага: разбираться с ошибкой приходится по тому, что есть.
        Assert.Equal(error, exception.ResponseBody);
        Assert.Equal(NpdStatusErrorCodes.ValidationFailed, exception.Code);
    }

    /// <summary>
    /// Незнакомый код не схлопывается в «неизвестная ошибка»: он доходит как есть, и тело рядом.
    /// </summary>
    [Fact]
    public async Task AnUnfamiliarCodeReachesTheCallerWithItsBody()
    {
        const string error = """{"code":"some.future.error","message":"Что-то новое"}""";
        var (client, _) = StubHandler.Create(HttpStatusCode.UnprocessableEntity, error);

        var exception = await Assert.ThrowsAsync<NpdStatusException>(
            () => client.GetStatusAsync("525741209968", Date));

        Assert.Equal("some.future.error", exception.Code);
        Assert.Equal(error, exception.ResponseBody);
        Assert.False(exception.IsTransient);
    }

    /// <summary>У ответа 400 кода нет вовсе — тело остаётся единственным, что можно показать.</summary>
    [Fact]
    public async Task AMalformedRequestKeepsItsPlainTextBody()
    {
        var (client, _) = StubHandler.Create(HttpStatusCode.BadRequest, "Bad Request", "text/plain");

        var exception = await Assert.ThrowsAsync<NpdStatusException>(
            () => client.GetStatusAsync("525741209968", Date));

        Assert.Null(exception.Code);
        Assert.Equal("Bad Request", exception.ResponseBody);
    }

    /// <summary>Успешный ответ, который не разобрался, тоже отдаёт тело.</summary>
    [Fact]
    public async Task AnUnparseableSuccessKeepsItsBody()
    {
        var (client, _) = StubHandler.Create(HttpStatusCode.OK, "<html>not json</html>", "text/html");

        var exception = await Assert.ThrowsAsync<NpdStatusException>(
            () => client.GetStatusAsync("525741209968", Date));

        Assert.Equal("<html>not json</html>", exception.ResponseBody);
    }
}
