using BitConverter;
using EchoDotNetLite;
using EchoDotNetLite.Common;
using EchoDotNetLite.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace EchoDotNetLiteSkstackIpBridge.Example
{
    public class Example
    {
        private EchoClient echoClient;
        private ILogger<Example> _logger;

        public Example(ILogger<Example> logger, EchoClient echoClient)
        {
            _logger = logger;
            this.echoClient = echoClient;
            this.echoClient.OnNodeJoined += LogNodeJoined;

            //コントローラとしてふるまう
            this.echoClient.SelfNode.Devices.Add(
                new EchoObjectInstance(
                    EchoDotNetLite.Specifications.機器.管理操作関連機器.コントローラ, 0x01));
        }

        private void LogNodeJoined(object sender, EchoDotNetLite.Models.EchoNode e)
        {
            _logger.LogTrace($"新しいEchoNode {e.Address}");
            e.OnCollectionChanged += LogEchoObjectChange;
        }

        private void LogEchoObjectChange(object sender, (CollectionChangeType type, EchoObjectInstance instance) e)
        {
            switch (e.type)
            {
                case CollectionChangeType.Add:
                    _logger.LogTrace($"EchoObject Add {e.instance.GetDebugString()}");
                    e.instance.OnCollectionChanged += LogEchoPropertyChange;
                    break;
                case CollectionChangeType.Remove:
                    _logger.LogTrace($"EchoObject Remove {e.instance.GetDebugString()}");
                    break;
                default:
                    break;
            }
        }

        private void LogEchoPropertyChange(object sender, (CollectionChangeType type, EchoPropertyInstance instance) e)
        {
            switch (e.type)
            {
                case CollectionChangeType.Add:
                    _logger.LogTrace($"EchoProperty Add {e.instance.GetDebugString()}");
                    e.instance.ValueChanged += LogEchoPropertyValueChanged;
                    break;
                case CollectionChangeType.Remove:
                    _logger.LogTrace($"EchoProperty Remove {e.instance.GetDebugString()}");
                    break;
                default:
                    break;
            }
        }

        private void LogEchoPropertyValueChanged(object sender, byte[] e)
        {
            if (sender is EchoPropertyInstance echoPropertyInstance)
            {
                _logger.LogTrace($"EchoProperty Change {echoPropertyInstance.GetDebugString()} {BytesConvert.ToHexString(e)}");
            }
        }

        private Timer timer = null;
        public async Task ExecuteAsync()
        {
            try
            {
                await echoClient.インスタンスリスト通知Async();
                await echoClient.インスタンスリスト通知要求Async();

                await Task.Delay(2 * 1000);
                _logger.LogDebug("プロパティマップ読み込み完了まで待機");
                while (echoClient.NodeList?.FirstOrDefault()?.Devices?.FirstOrDefault() == null
                        || !echoClient.NodeList.First().Devices.First().IsPropertyMapGet)
                {
                    await Task.Delay(2 * 1000);
                }

                //Bルートなので、低圧スマート電力量メータ以外のデバイスは存在しない前提
                var node = echoClient.NodeList.First();
                var device = node.Devices.First();

                _logger.LogDebug("デバイスのGET対応プロパティの値をすべて取得");
                //まとめてもできるけど、大量に指定するとこけるのでプロパティ毎に
                foreach (var prop in echoClient.NodeList.First().Devices.First().GETProperties)
                {
                    await echoClient.プロパティ値読み出し(
                        echoClient.SelfNode.Devices.First(),//コントローラー
                        node,device,new EchoPropertyInstance[] { prop }
                        , 5 * 1000);
                }

                var target = new string[] { "瞬時電力計測値", "瞬時電流計測値", "現在時刻設定", "現在年月日設定" };
                var properties = device.GETProperties.Where(p => target.Contains(p.Spec.Name));
                timer = new Timer(20 * 1000);
                timer.Elapsed += (sender, e) =>
                {
                    try
                    {
                        timer.Stop();
                        Task.Run(() =>
                            echoClient.プロパティ値読み出し(
                                echoClient.SelfNode.Devices.First(),//コントローラー
                                node,device, properties
                                , 20 * 1000)
                        ).ContinueWith((t) =>
                        {
                            if (t.Exception != null)
                            {
                                _logger.LogTrace(t.Exception, "Exception");
                            }
                            var 瞬時電力計測値 = properties.Where(p => p.Spec.Name == "瞬時電力計測値").First();
                            var 瞬時電流計測値 = properties.Where(p => p.Spec.Name == "瞬時電流計測値").First();
                            var 現在時刻設定 = properties.Where(p => p.Spec.Name == "現在時刻設定").First();
                            var 現在年月日設定 = properties.Where(p => p.Spec.Name == "現在年月日設定").First();

                            _logger.LogDebug($"瞬時電力計測値:{EndianBitConverter.BigEndian.ToInt32(瞬時電力計測値.Value,0)}W");
                            _logger.LogDebug($"瞬時電流計測値: R相{EndianBitConverter.BigEndian.ToInt16(瞬時電流計測値.Value, 0) * 0.1}A,T相{EndianBitConverter.BigEndian.ToInt16(瞬時電流計測値.Value, 2) * 0.1}A");
                        });
                    }
                    finally
                    {
                        timer.Start();
                    }
                };
                timer.Start();
            }
            catch (AggregateException ex)
            {
                _logger.LogError(ex, "AggregateException");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception");
            }
        }
    }
}
