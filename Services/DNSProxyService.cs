using System.Net;
using System.Net.Sockets;
using DNS.Protocol;
using DNS.Server;

namespace inFAMOUSReborn.Services;

public class DNSProxyService : BackgroundService
{
    private readonly ILogger<DNSProxyService> _logger;
    private DnsServer? _server;

    public DNSProxyService(ILogger<DNSProxyService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string localIp = GetLocalIPAddress();
        IPAddress localIpAddress = IPAddress.Parse(localIp);
        
        var masterFile = new MasterFile();
        
        string[] domains = {
            "infamous2-release.ps3.online.scea.com",
            "infamous2-release.ps3.online.scea.com.",
            "dbhhpqias9rrc.cloudfront.net",
            "dbhhpqias9rrc.cloudfront.net.",
            "r2.infamousreborn.com",
            "r2.infamousreborn.com."
        };

        foreach (var domain in domains)
        {
            masterFile.AddIPAddressResourceRecord(new Domain(domain), localIpAddress);
        }
        
        _server = new DnsServer(masterFile, "8.8.8.8");

        _logger.LogInformation("==================================================");
        _logger.LogInformation($"inFAMOUS Reborn DNS Proxy is running.");
        _logger.LogInformation($"IP: {localIp}");
        _logger.LogInformation("==================================================");

        try
        {
            await _server.Listen(53, IPAddress.Any);
        }
        catch (SocketException ex)
        {
            _logger.LogError($"[Error] Couldn't open Port 53. Run with sudo. ({ex.Message})");
        }
        
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _server?.Dispose();
        base.Dispose();
    }

    private string GetLocalIPAddress()
    {
        using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
        socket.Connect("8.8.8.8", 65530);
        return socket.LocalEndPoint is IPEndPoint endPoint ? endPoint.Address.ToString() : "127.0.0.1";
    }
}