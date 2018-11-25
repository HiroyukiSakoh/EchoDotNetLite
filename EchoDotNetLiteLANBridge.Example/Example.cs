using BitConverter;
using EchoDotNetLite;
using EchoDotNetLite.Common;
using EchoDotNetLite.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace EchoDotNetLiteLANBridge.Example
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

        public async Task ExecuteAsync()
        {
            try
            {
                //エミュレーターはプロパティ値通知要求に応答しないので、
                //ノードを手動で追加し、自ノードインスタンスリストSをプロパティ値読み出し する
                var エミュレーターノード = new EchoNode()
                {
                    Address = "192.168.11.11",
                    NodeProfile = new EchoObjectInstance(EchoDotNetLite.Specifications.プロファイル.ノードプロファイル, 0x01)
                };
                echoClient.NodeList.Add(エミュレーターノード);

                await echoClient.プロパティ値読み出し(
                    echoClient.SelfNode.NodeProfile//ノードプロファイルから
                    , エミュレーターノード
                    , エミュレーターノード.NodeProfile
                    , エミュレーターノード.NodeProfile.GETProperties.Where(p=>p.Spec.Code==0xD6)//自ノードインスタンスリストS
                    );

                //インスタンスリスト通知受信を自ノードインスタンスリストSの値で、Reflectionを使って呼び出す
                var method = echoClient.GetType().GetMethod("インスタンスリスト通知受信", BindingFlags.Instance | BindingFlags.NonPublic);
                method.Invoke(echoClient, new object[]{エミュレーターノード,
                    エミュレーターノード.NodeProfile.GETProperties.Where(p => p.Spec.Code == 0xD6).First().Value });

                _logger.LogDebug("プロパティマップ読み込み完了まで待機");
                while (!エミュレーターノード.Devices.All(d=>d.IsPropertyMapGet))
                {
                    await Task.Delay(100);
                }

                foreach(var device in エミュレーターノード.Devices)
                {
                    await echoClient.プロパティ値読み出し(
                        echoClient.SelfNode.Devices.First()//コントローラーから
                        , エミュレーターノード
                        , device
                        , device.GETProperties//全プロパティ読み込み
                        );

                    var sb = new StringBuilder();
                    sb.AppendLine($"{device.Spec.Class.ClassNameOfficial}");
                    foreach(var prop in device.GETProperties)
                    {
                        sb.AppendLine($"\t0x{prop.Spec.Code:X2} {prop.Spec.Name}\t{BytesConvert.ToHexString(prop.Value)}");
                    }
                    _logger.LogInformation(sb.ToString());
                }

                var HomeAirConditioner = エミュレーターノード.Devices.Where(d => d.Spec.ClassGroup.ClassGroupCode == 0x01 && d.Spec.Class.ClassCode == 0x30).First();
                HomeAirConditioner.SETProperties.Where(p => p.Spec.Code == 0x80).First().Value = new byte[] { 0x30 };//0x80電源,0x30 ON
                await echoClient.プロパティ値書き込み応答要(
                    echoClient.SelfNode.NodeProfile//ノードプロファイルから
                    , エミュレーターノード
                    , HomeAirConditioner
                    , HomeAirConditioner.SETProperties.Where(p=>p.Spec.Code== 0x80) 
                    );
                while (true)
                {
                    await Task.Delay(60 * 1000);
                }
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
