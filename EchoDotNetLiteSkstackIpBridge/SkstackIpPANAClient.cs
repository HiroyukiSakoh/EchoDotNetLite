using EchoDotNetLite;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RJCP.IO.Ports;
using SkstackIpDotNet;
using SkstackIpDotNet.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoDotNetLiteSkstackIpBridge
{
    public class SkstackIpPANAClient : IPANAClient, IDisposable
    {
        private readonly ILogger _logger;
        private readonly SKDevice SKDevice;
        public SkstackIpPANAClient(ILogger<SkstackIpPANAClient> logger, SKDevice skDevice)
        {
            _logger = logger;
            SKDevice = skDevice;
            SKDevice.OnERXUDPReceived += ReceivedERXUDP;
        }

        public async Task OpenAsync(string port, int baud, int data, Parity parity, StopBits stopbits)
        {
            _logger.LogDebug($"Bridge Open port:{port},baud:{baud},data:{data},parity:{parity},stopbits:{stopbits}");
            SKDevice.Open(port, baud, data, parity, stopbits);
            var info = await SKDevice.SKInfoAsync();
            SelfIpaddr = info.IPAddress;
        }

        public string BroadcastIpaddr { get; set; }

        public string SmartMaterIpaddr { get; set; }
        public string SelfIpaddr { get; set; }

        public async Task<bool> ScanAndJoinAsync(string bRouteId, string bRoutePassword)
        {
            _logger.LogDebug($"パスワードの設定");
            await SKDevice.SKSetPwdAsync("C", bRoutePassword);
            _logger.LogDebug($"IDの設定");
            await SKDevice.SKSetRBIDAsync(bRouteId);

            _logger.LogDebug($"スキャン開始");
            SkstackIpDotNet.Responses.EPANDESC pan = null;
            for (byte duration = 4; duration < 8; duration++)
            {
                _logger.LogDebug($"スキャン時間:{duration}");
                var scanResult = await SKDevice.SKScanActiveExAsync(0xFFFFFFFF, duration);

                if (scanResult.Any())
                {
                    pan = scanResult.First();
                    _logger.LogDebug($"PAN発見: 論理チャンネル番号:{pan.Channel},チャンネルページ:{pan.ChannelPage},PAN ID:{pan.PanID},アドレス:{pan.Addr},RSSI:{pan.LQI},PairingID:{pan.PairID}");
                    break;
                }
            }
            if (pan == null)
            {
                _logger.LogDebug($"PANが見つからない");
                return false;
            }
            await SKDevice.SKSRegAsync("S2", pan.Channel);
            await SKDevice.SKSRegAsync("S3", pan.PanID);
            var skll64 = await SKDevice.SKLl64Async(pan.Addr);
            //TODO Bルートの一斉同報の宛先ってスマートメーターだけ…?
            BroadcastIpaddr = skll64.Ipaddr;
            SmartMaterIpaddr = skll64.Ipaddr;
            var joinTCS = new TaskCompletionSource<bool>();
            var joinEvent = default(EventHandler<EVENT>);
            joinEvent += (sender, e) =>
            {
                if (e.Num == "24")
                {
                    _logger.LogDebug($"0x24:PANA による接続過程でエラーが発生した（接続が完了しなかった）");
                    joinTCS.SetResult(false);
                    SKDevice.OnEVENTReceived -= joinEvent;
                }
                if (e.Num == "25")
                {
                    _logger.LogDebug($"0x25:PANA による接続が完了した");
                    joinTCS.SetResult(true);
                    SKDevice.OnEVENTReceived -= joinEvent;
                }
            };
            SKDevice.OnEVENTReceived += joinEvent;
            _logger.LogDebug($"PANA接続シーケンス開始");
            await SKDevice.SKJoinAsync(SmartMaterIpaddr);
            if (await Task.WhenAny(joinTCS.Task, Task.Delay(30*1000)) == joinTCS.Task)
            {
                return await joinTCS.Task;
            }
            else
            {
                _logger.LogDebug($"PANA接続シーケンス タイムアウト");
                SKDevice.OnEVENTReceived -= joinEvent;
                throw new Exception("Time has expired");
            }
        }

        public event EventHandler<(string,byte[])> OnEventReceived;

        public void ReceivedERXUDP(object sendor, ERXUDP erxudp)
        {
            OnEventReceived?.Invoke(this, (erxudp.Sender, BytesConvert.FromHexString(erxudp.Data)));
        }


        public async Task RequestAsync(string address,byte[] request)
        {
            if (address == null)
            {
                address = BroadcastIpaddr;
            }
            await SKDevice.SKSendToAsync(
                "1",
                address,
                "0E1A",
                SKSendToSec.SecOrNotTransfer,
                request);
        }


        public void Close()
        {
            SKDevice?.Close();
        }
        public void Dispose()
        {
            SKDevice?.Close();
            SKDevice?.Dispose();
        }
    }
}
