using EchoDotNetLite;
using EchoDotNetLite.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EchoDotNetLiteLANBridge
{
    public class LANClient : IPANAClient, IDisposable
    {
        private readonly UdpClient receiveUdpClient;
        private readonly ILogger _logger;
        private static int DefaultUdpPort = 3610;
        public LANClient(ILogger<LANClient> logger)
        {
            var selfAddresses = NetworkInterface.GetAllNetworkInterfaces().SelectMany(ni => ni.GetIPProperties().UnicastAddresses.Select(ua => ua.Address.ToString()));
            _logger = logger;
            try
            {
                receiveUdpClient = new UdpClient(DefaultUdpPort)
                {
                    EnableBroadcast = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Exception");
                throw;
            }
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        var receivedResults = await receiveUdpClient.ReceiveAsync();
                        if (selfAddresses.Contains(receivedResults.RemoteEndPoint.Address.ToString()))
                        {
                            //ブロードキャストを自分で受信(無視)
                            continue;
                        }
                        _logger.LogDebug($"UDP受信:{receivedResults.RemoteEndPoint.Address.ToString()} {BytesConvert.ToHexString(receivedResults.Buffer)}");
                        OnEventReceived?.Invoke(this, (receivedResults.RemoteEndPoint.Address.ToString(), receivedResults.Buffer));
                    }
                }
                catch (System.ObjectDisposedException)
                {
                    //握りつぶす
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Exception");
                }
            });
        }

        public event EventHandler<(string, byte[])> OnEventReceived;

        public void Dispose()
        {
            _logger.LogDebug("Dispose");
            try
            {
                receiveUdpClient?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Exception");
            }
        }

        public async Task RequestAsync(string address, byte[] request)
        {
            _logger.LogDebug($"UDP送信:{address ?? "Broadcast"} {BytesConvert.ToHexString(request)}");
            IPEndPoint remote;
            if (address == null)
            {
                remote = new IPEndPoint(IPAddress.Broadcast, DefaultUdpPort);
            }
            else
            {
                remote = new IPEndPoint(IPAddress.Parse(address), DefaultUdpPort);
            }
            var sendUdpClient = new UdpClient()
            {
                EnableBroadcast = true
            };
            sendUdpClient.Connect(remote);
            await sendUdpClient.SendAsync(request, request.Length);
            sendUdpClient.Close();
        }
    }
}
