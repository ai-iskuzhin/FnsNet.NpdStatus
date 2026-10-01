namespace FnsNet.NpdStatus;

/// <summary>
/// Settings for the FNS self-employed status service.
/// </summary>
public sealed class NpdStatusOptions
{
    /// <summary>Configuration section this binds from by convention.</summary>
    public const string SectionName = "FnsNpdStatus";

    /// <summary>
    /// The service address. Defaults to the published production endpoint.
    /// </summary>
    public Uri BaseAddress { get; set; } = new("https://statusnpd.nalog.ru/");

    /// <summary>
    /// How long to wait for a reply.
    /// </summary>
    /// <remarks>
    /// The published contract requires callers to allow at least 60 seconds, so that is the default.
    /// Observed replies are far quicker — around 100 ms — but a client that gives up early turns the
    /// service's own slow path into an error of its own making.
    /// </remarks>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
}
