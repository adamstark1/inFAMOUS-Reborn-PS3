using DNS.Protocol;
using System.Net;
using System.Net.Sockets;
using DNS.Client.RequestResolver;
using DNS.Server;

namespace inFAMOUSReborn.Services;

public class DnsProxyService : BackgroundService
{
    private readonly ILogger<DnsProxyService> _logger;
    private DnsServer? _server;

    public DnsProxyService(ILogger<DnsProxyService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Get local IP
        string localIp = GetLocalIPAddress();
        IPAddress localIpAddress = IPAddress.Parse(localIp);
        
        var masterFile = new MasterFile();
    
        // Reroute Socker Punch and Cloudfront to local IP
        masterFile.AddIPAddressResourceRecord(new Domain("infamous2-release.ps3.online.scea.com"), localIpAddress);
        masterFile.AddIPAddressResourceRecord(new Domain("dbhhpqias9rrc.cloudfront.net"), localIpAddress);
        masterFile.AddIPAddressResourceRecord(new Domain("r2.infamousreborn.com"), localIpAddress);
        
        _server = new DnsServer(masterFile, "8.8.8.8");

        _logger.LogInformation("==================================================");
        _logger.LogInformation($"inFAMOUS Reborn is running. IP: {localIp}");
        _logger.LogInformation("==================================================");

        try
        {
            // Open Port 53
            _server.Listen(53);
        }
        catch (SocketException ex)
        {
            _logger.LogError($"[Errör] Couldn't open Port 53. Try running with sudo/admin. ({ex.Message})");
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