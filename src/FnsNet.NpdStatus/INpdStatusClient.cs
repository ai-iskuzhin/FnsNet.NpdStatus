namespace FnsNet.NpdStatus;

/// <summary>
/// Asks the FNS whether an INN belongs to a self-employed taxpayer on a given date.
/// </summary>
/// <remarks>
/// A thin client over one public endpoint. It does not retry, rate-limit, cache or validate input:
/// the service allows two requests per minute per IP address, and how to spend them — queue, cache,
/// which failures block what — is the integrating application's decision, not this library's.
/// </remarks>
public interface INpdStatusClient
{
    /// <summary>
    /// Asks about <paramref name="inn"/> on <paramref name="date"/>.
    /// </summary>
    /// <param name="inn">The taxpayer's INN.</param>
    /// <param name="date">
    /// The date to ask about. The service accepts 2019-01-01 at the earliest, when the regime began,
    /// and refuses anything later than today.
    /// </param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="NpdStatusException">The service answered with an error.</exception>
    /// <exception cref="HttpRequestException">The service could not be reached.</exception>
    /// <exception cref="TaskCanceledException">The request timed out or was cancelled.</exception>
    Task<NpdTaxpayerStatus> GetStatusAsync(
        string inn,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
