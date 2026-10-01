using System.Net;

namespace FnsNet.NpdStatus;

/// <summary>
/// The service answered, and the answer was not a status.
/// </summary>
/// <remarks>
/// Carries what the service said rather than a flattened message, because the three documented codes
/// want different handling: <see cref="NpdStatusErrorCodes.ValidationFailed"/> is the caller's input
/// and will not improve on retry, while
/// <see cref="NpdStatusErrorCodes.ServiceUnavailable"/> and
/// <see cref="NpdStatusErrorCodes.ServiceLimited"/> are about this moment and will.
/// <para>
/// None of them mean "not registered". A failed check leaves the status unknown, and treating
/// unknown as negative is how a registered person gets turned away.
/// </para>
/// </remarks>
public sealed class NpdStatusException : Exception
{
    /// <summary>Creates an exception describing what the service answered.</summary>
    /// <param name="statusCode">The HTTP status the service returned.</param>
    /// <param name="code">The service's error code, when it sent one.</param>
    /// <param name="serviceMessage">The service's own message, when it sent one.</param>
    /// <param name="responseBody">The response body as it arrived.</param>
    /// <param name="innerException">The underlying failure, when there was one.</param>
    public NpdStatusException(
        HttpStatusCode statusCode,
        string? code,
        string? serviceMessage,
        string? responseBody = null,
        Exception? innerException = null)
        : base(BuildMessage(statusCode, code, serviceMessage), innerException)
    {
        StatusCode = statusCode;
        Code = code;
        ServiceMessage = serviceMessage;
        ResponseBody = responseBody;
    }

    /// <summary>The HTTP status: 422 for a business error, 400 for a malformed body, 500 inside.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// The service's error code, or <c>null</c> when it sent none — a 400 carries no code.
    /// </summary>
    public string? Code { get; }

    /// <summary>The service's own message, in Russian. Shown to a person, not matched on.</summary>
    public string? ServiceMessage { get; }

    /// <summary>
    /// The response body as it arrived, kept whatever the status.
    /// </summary>
    /// <remarks>
    /// Always captured, unlike the body of a successful answer: an error is rare, small, and the one
    /// case where <see cref="Code" /> alone is not enough — an unfamiliar code or a 400 with no code
    /// at all leaves nothing else to put in a support ticket.
    /// </remarks>
    public string? ResponseBody { get; }

    /// <summary>Whether trying again later could plausibly give a different answer.</summary>
    public bool IsTransient =>
        Code is NpdStatusErrorCodes.ServiceUnavailable or NpdStatusErrorCodes.ServiceLimited
        || StatusCode == HttpStatusCode.InternalServerError;

    private static string BuildMessage(HttpStatusCode statusCode, string? code, string? serviceMessage)
    {
        var detail = string.IsNullOrWhiteSpace(serviceMessage) ? null : serviceMessage;

        return (code, detail) switch
        {
            (not null, not null) => $"FNS NPD status service returned {(int)statusCode} {code}: {detail}",
            (not null, null) => $"FNS NPD status service returned {(int)statusCode} {code}.",
            (null, not null) => $"FNS NPD status service returned {(int)statusCode}: {detail}",
            _ => $"FNS NPD status service returned {(int)statusCode}.",
        };
    }
}
