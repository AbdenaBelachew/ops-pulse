using System.Net;
using OpsPulse.Infrastructure.Monitoring;

namespace OpsPulse.Tests.Monitoring;

public class NetworkAddressPolicyTests
{
    [Theory]
    [InlineData("127.0.0.1")]          // loopback
    [InlineData("10.1.2.3")]           // private
    [InlineData("172.16.0.1")]         // private
    [InlineData("172.31.255.255")]     // private
    [InlineData("192.168.1.10")]       // private
    [InlineData("169.254.169.254")]    // cloud metadata (link-local)
    [InlineData("100.64.0.1")]         // CGNAT
    [InlineData("0.0.0.0")]            // this network
    [InlineData("224.0.0.1")]          // multicast
    [InlineData("255.255.255.255")]    // broadcast
    [InlineData("::1")]                // IPv6 loopback
    [InlineData("::")]                 // IPv6 unspecified
    [InlineData("fe80::1")]            // IPv6 link-local
    [InlineData("fd00::1")]            // IPv6 unique-local (fc00::/7)
    [InlineData("fc00::1")]            // IPv6 unique-local (fc00::/7)
    [InlineData("ff02::1")]            // IPv6 multicast
    [InlineData("::ffff:127.0.0.1")]   // IPv4-mapped loopback
    [InlineData("::ffff:10.0.0.5")]    // IPv4-mapped private
    [InlineData("::ffff:169.254.169.254")] // IPv4-mapped metadata
    public void Non_public_addresses_are_blocked(string address)
    {
        Assert.False(NetworkAddressPolicy.IsPublic(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("172.15.0.1")]         // just outside 172.16.0.0/12
    [InlineData("172.32.0.1")]         // just outside 172.16.0.0/12
    [InlineData("100.63.255.255")]     // just outside CGNAT
    [InlineData("2606:4700:4700::1111")]
    [InlineData("::ffff:8.8.8.8")]
    public void Public_addresses_are_allowed(string address)
    {
        Assert.True(NetworkAddressPolicy.IsPublic(IPAddress.Parse(address)));
    }
}
