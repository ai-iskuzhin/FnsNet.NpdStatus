using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FnsNet.NpdStatus;

/// <inheritdoc cref="INpdStatusClient" />
public sealed class NpdStatusClient : INpdStatusClient, IDisposable
{
    internal const string RequestPath = "api/v1/tracker/taxpayer_status";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    /// <summary>Creates the client over a configured <see cref="HttpClient" />.</summary>
    /// <param name="httpClient">
    /// Supplies the base address and the timeout; register it through
    /// <see cref="ServiceCollectionExtensions.AddNpdStatusClient" />.
    /// </param>
    public NpdStatusClient(HttpClient httpClient)
        : this(httpClient, ownsHttpClient: false)
    {
    }

    private NpdStatusClient(HttpClient httpClient, bool ownsHttpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
        _ownsHttpClient = ownsHttpClient;
    }

    /// <summary>
    /// Освобождает <see cref="HttpClient" />, если клиент создал его сам.
    /// </summary>
    /// <remarks>
    /// Переданный извне не трогаем: при регистрации через контейнер им владеет фабрика, и закрыть
    /// его здесь значило бы рвать соединения, которые она переиспользует для других вызовов.
    /// </remarks>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    /// <summary>
    /// Создаёт клиент без контейнера зависимостей.
    /// </summary>
    /// <param name="options">Настройки; если не переданы, берутся значения по умолчанию.</param>
    /// <remarks>
    /// Созданный так клиент владеет собственным <see cref="HttpClient" /> и живёт, пока его не
    /// освободят. В приложении с контейнером предпочтительнее
    /// <see cref="ServiceCollectionExtensions.AddNpdStatusClient" />: он отдаёт соединения фабрике,
    /// а не держит их на себе.
    /// </remarks>
    public static NpdStatusClient Create(NpdStatusOptions? options = null)
    {
        var settings = options ?? new NpdStatusOptions();

        return new NpdStatusClient(
            new HttpClient
            {
                BaseAddress = settings.BaseAddress,
                Timeout = settings.Timeout,
            },
            ownsHttpClient: true);
    }

    /// <inheritdoc />
    public async Task<NpdTaxpayerStatus> GetStatusAsync(
        string inn,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inn);

        var body = new NpdStatusRequestBody
        {
            Inn = inn.Trim(),
            RequestDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        };

        using var response = await _httpClient
            .PostAsJsonAsync(RequestPath, body, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw await BuildErrorAsync(response, cancellationToken).ConfigureAwait(false);
        }

        var payload = await response.Content
            .ReadFromJsonAsync<NpdStatusResponseBody>(SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        if (payload is null)
        {
            throw new NpdStatusException(response.StatusCode, null, "The service returned an empty body.");
        }

        return new NpdTaxpayerStatus(body.Inn, date, payload.Status, payload.Message);
    }

    /// <summary>
    /// Turns a failure response into an exception carrying whatever the service said.
    /// </summary>
    /// <remarks>
    /// A 422 carries <c>{ code, message }</c>; a 400 carries neither, because the body never parsed.
    /// Reading the error body is best-effort for that reason — failing to parse the explanation of a
    /// failure must not replace it with a parser error, which would hide what actually went wrong.
    /// </remarks>
    private static async Task<NpdStatusException> BuildErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string? code = null;
        string? message = null;

        try
        {
            var error = await response.Content
                .ReadFromJsonAsync<NpdStatusErrorBody>(SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            code = error?.Code;
            message = error?.Message;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            message = await ReadRawAsync(response, cancellationToken).ConfigureAwait(false);
        }

        return new NpdStatusException(response.StatusCode, code, message);
    }

    private static async Task<string?> ReadRawAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
        }
        catch (Exception exception) when (exception is HttpRequestException or ObjectDisposedException)
        {
            return null;
        }
    }
}
