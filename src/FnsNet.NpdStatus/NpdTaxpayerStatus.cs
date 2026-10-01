using System.Text.Json.Serialization;

namespace FnsNet.NpdStatus;

/// <summary>
/// What the service answers about one INN on one date.
/// </summary>
/// <param name="Inn">The INN that was asked about.</param>
/// <param name="Date">The date the answer is about.</param>
/// <param name="IsRegistered">
/// Whether that INN was paying налог на профессиональный доход on that date.
/// </param>
/// <param name="Message">
/// The service's own sentence, in Russian, suitable for showing to a person.
/// </param>
/// <remarks>
/// The answer is about a <em>date</em>, not a standing fact: registration can end at any time, so a
/// result is only ever true of the day it was asked about. Store the date beside the answer.
/// </remarks>
public sealed record NpdTaxpayerStatus(
    string Inn,
    DateOnly Date,
    bool IsRegistered,
    string? Message);

/// <summary>The request body, exactly as the service expects it.</summary>
internal sealed record NpdStatusRequestBody
{
    [JsonPropertyName("inn")]
    public required string Inn { get; init; }

    /// <summary>The date, as <c>yyyy-MM-dd</c>.</summary>
    [JsonPropertyName("requestDate")]
    public required string RequestDate { get; init; }
}

/// <summary>
/// The response body.
/// </summary>
/// <remarks>
/// Unknown members are ignored by design: the published contract says the service may add fields and
/// headers without raising the API version, and requires clients to tolerate that.
/// </remarks>
internal sealed record NpdStatusResponseBody
{
    [JsonPropertyName("status")]
    public bool Status { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

/// <summary>The body the service returns with a business error.</summary>
internal sealed record NpdStatusErrorBody
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
