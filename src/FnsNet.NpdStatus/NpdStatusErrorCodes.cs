namespace FnsNet.NpdStatus;

/// <summary>
/// The business error codes the service documents.
/// </summary>
/// <remarks>
/// Branch on these rather than on the message: the message is Russian prose meant for a person and
/// carries the detail, while the code is the contract. The list is not closed — an unrecognised code
/// still arrives as <see cref="NpdStatusException.Code"/>.
/// </remarks>
public static class NpdStatusErrorCodes
{
    /// <summary>
    /// The input was rejected. The message says why: a malformed INN, a date before 2019-01-01 when
    /// the regime began, or a date in the future.
    /// </summary>
    public const string ValidationFailed = "validation.failed";

    /// <summary>
    /// The service is temporarily unavailable. Documented as an ordinary outcome, not an incident —
    /// treat an answer as unknown rather than as "not registered".
    /// </summary>
    public const string ServiceUnavailable = "taxpayer.status.service.unavailable.error";

    /// <summary>
    /// Too many requests from this IP address. The published limit is two per minute, counted per
    /// address rather than per caller, so every process behind one address shares it.
    /// </summary>
    public const string ServiceLimited = "taxpayer.status.service.limited.error";
}
