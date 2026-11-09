using System.Net;
using System.Net.Sockets;

namespace OpsPulse.Infrastructure.Monitoring;

/// <summary>
/// SSRF address policy shared by every outbound request OpsPulse makes on a
/// tenant's behalf (HTTP checks and webhook deliveries). An address is
/// "public" when it is routable on the internet — loopback, private,
/// link-local (incl. cloud metadata 169.254.169.254), CGNAT, multicast and
/// reserved ranges are not.
/// </summary>
public static class NetworkAddressPolicy
{
    public static bool IsPublic(IPAddress address)
    {
        // ::ffff:10.0.0.1 must be judged as 10.0.0.1, otherwise IPv4-mapped
        // IPv6 literals would bypass the IPv4 rules.
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return !(b[0] == 0 ||                                  // 0.0.0.0/8 "this network"
                     b[0] == 10 ||                                 // 10.0.0.0/8 private
                     b[0] == 127 ||                                // 127.0.0.0/8 loopback
                     (b[0] == 100 && b[1] >= 64 && b[1] <= 127) || // 100.64.0.0/10 CGNAT
                     (b[0] == 169 && b[1] == 254) ||               // 169.254.0.0/16 link-local / metadata
                     (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||  // 172.16.0.0/12 private
                     (b[0] == 192 && b[1] == 0 && b[2] == 0) ||    // 192.0.0.0/24 IETF protocol
                     (b[0] == 192 && b[1] == 168) ||               // 192.168.0.0/16 private
                     (b[0] == 198 && (b[1] == 18 || b[1] == 19)) ||// 198.18.0.0/15 benchmarking
                     b[0] >= 224);                                 // multicast + reserved + broadcast
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.Equals(IPAddress.IPv6Any) || IPAddress.IsLoopback(address) ||
                address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
            {
                return false;
            }

            // fc00::/7 unique-local addresses (IsIPv6SiteLocal only covers the deprecated fec0::/10).
            var first = address.GetAddressBytes()[0];
            return (first & 0xFE) != 0xFC;
        }

        return false;
    }

    /// <summary>
    /// Socket connect callback that resolves DNS itself and refuses non-public
    /// targets before any socket is opened — this also defeats DNS rebinding.
    /// </summary>
    public static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        bool allowPrivateNetworks,
        CancellationToken cancellationToken)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        if (addresses.Length == 0)
        {
            throw new SocketException((int)SocketError.HostNotFound);
        }

        if (!allowPrivateNetworks && addresses.FirstOrDefault(a => !IsPublic(a)) is { } blocked)
        {
            throw new HttpRequestException($"Blocked non-public address {blocked} (SSRF protection).");
        }

        var socket = new Socket(addresses[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(new IPEndPoint(addresses[0], context.DnsEndPoint.Port), cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
