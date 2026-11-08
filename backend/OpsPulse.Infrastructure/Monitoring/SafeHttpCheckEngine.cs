using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpsPulse.Domain.Services;

namespace OpsPulse.Infrastructure.Monitoring;

public sealed record HttpCheckResult(
    bool IsSuccess,
    int? StatusCode,
    int ResponseTimeMs,
    string? ErrorMessage,
    ServiceStatus StatusAfter);

/// <summary>
/// Executes HTTP checks with SSRF protection: DNS is resolved inside the
/// connect callback and every resolved address is validated before a socket
/// is opened, which also defeats DNS rebinding between validation and connect.
/// </summary>
public sealed class SafeHttpCheckEngine(IOptions<MonitoringOptions> options, ILogger<SafeHttpCheckEngine> logger)
{
    public async Task<HttpCheckResult> CheckAsync(MonitoredService service, CancellationToken cancellationToken = default)
    {
        var started = Stopwatch.GetTimestamp();

        if (!Uri.TryCreate(service.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Failure(started, "Invalid URL — must be absolute http(s).");
        }

        HashSet<int> expectedCodes;
        try
        {
            expectedCodes = service.ExpectedStatusCodes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToHashSet();
        }
        catch (FormatException)
        {
            return Failure(started, "Invalid expected status code list.");
        }

        try
        {
            using var handler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.All,
                UseCookies = false,
                ConnectCallback = async (context, connectCancellationToken) =>
                {
                    var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, connectCancellationToken);
                    if (addresses.Length == 0)
                    {
                        throw new SocketException((int)SocketError.HostNotFound);
                    }

                    foreach (var address in addresses)
                    {
                        if (!IsAllowedRemoteAddress(address))
                        {
                            logger.LogWarning(
                                "SSRF protection blocked {Host} (resolved {Address}) for service {Service}",
                                context.DnsEndPoint.Host, address, service.Name);
                            throw new HttpRequestException(
                                $"Blocked non-public address {address} (SSRF protection).");
                        }
                    }

                    var target = addresses[0];
                    var socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(new DnsEndPoint(target.ToString(), context.DnsEndPoint.Port), connectCancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
            };

            using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(service.TimeoutSeconds));

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("OpsPulse-Monitor/1.0");

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
            var elapsedMs = ElapsedMs(started);
            var statusCode = (int)response.StatusCode;
            var isSuccess = expectedCodes.Contains(statusCode);

            var statusAfter = !isSuccess
                ? ServiceStatus.Down
                : elapsedMs > options.Value.DegradedResponseTimeMs
                    ? ServiceStatus.Degraded
                    : ServiceStatus.Up;

            return new HttpCheckResult(
                isSuccess,
                statusCode,
                elapsedMs,
                isSuccess ? null : $"Unexpected status code {statusCode} (expected: {service.ExpectedStatusCodes})",
                statusAfter);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure(started, $"Timed out after {service.TimeoutSeconds}s.");
        }
        catch (HttpRequestException exception)
        {
            return Failure(started, $"Connection failed: {exception.Message}");
        }
        catch (Exception exception)
        {
            return Failure(started, $"Check error: {exception.Message}");
        }
    }

    private HttpCheckResult Failure(long started, string message) =>
        new(false, null, ElapsedMs(started), message, ServiceStatus.Down);

    private static int ElapsedMs(long started) =>
        (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;

    private bool IsAllowedRemoteAddress(IPAddress address)
    {
        if (options.Value.AllowPrivateNetworks)
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var octets = address.GetAddressBytes();
            return !(octets[0] == 127 || octets[0] == 10 || octets[0] == 0 ||
                     (octets[0] == 172 && octets[1] >= 16 && octets[1] <= 31) ||
                     (octets[0] == 192 && octets[1] == 168) ||
                     (octets[0] == 169 && octets[1] == 254));
        }

        // IPv6: loopback (::1), link-local (fe80::/10) and unique-local (fc00::/7) are private.
        return !(IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal);
    }
}
