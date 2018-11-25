using EchoDotNetLite;
using EchoDotNetLite.Common;
using EchoDotNetLite.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RJCP.IO.Ports;
using SkstackIpDotNet;
using System;
using System.Threading.Tasks;

namespace EchoDotNetLiteSkstackIpBridge.Example
{
    class Program
    {
        static void ConfigureServices(IServiceCollection serviceCollection)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile("appsettings.local.json", false)
                .Build();

            // Add logging
            serviceCollection.AddSingleton<ILoggerFactory, LoggerFactory>();
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            serviceCollection.AddLogging(loggingBuilder =>
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"))
                .AddConsole()
            );

            // Add access to generic IConfigurationRoot
            serviceCollection.AddSingleton(configuration);

            serviceCollection.AddSingleton<SKDevice>();
            serviceCollection.AddSingleton<SkstackIpPANAClient>();
            serviceCollection.AddSingleton<Example>();
        }
        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<Example>();

            var skStackClient = serviceProvider.GetService<SkstackIpPANAClient>();
            serviceCollection.AddSingleton<IPANAClient, SkstackIpPANAClient>(f=>skStackClient);
            serviceCollection.AddSingleton<EchoClient>();
            serviceProvider = serviceCollection.BuildServiceProvider();
            var configuration = serviceProvider.GetService<IConfigurationRoot>();
            var BRouteId = configuration.GetValue<string>("BRoute:Id");
            var BRoutePw = configuration.GetValue<string>("BRoute:Pw");
            try
            {
                //Windows
                string devicePort = "COM3";
                if (System.Environment.OSVersion.Platform.ToString() != "Win32NT")
                {
                    //Linux等(Raspbian Stretchで動作確認)
                    devicePort = "/dev/ttyUSB0";
                }
                //シリアルポートOpen
                skStackClient.OpenAsync(devicePort, 115200, 8, Parity.None, StopBits.One).Wait();
                //スキャン＆Join
                if(skStackClient.ScanAndJoinAsync(BRouteId, BRoutePw).Result)
                {
                    serviceProvider.GetService<EchoClient>().Initialize(skStackClient.SelfIpaddr);
                    Task.Run(() => serviceProvider.GetService<Example>().ExecuteAsync());
                }
                Task.WaitAll(Task.Delay(-1));
            }
            catch (AggregateException ex)
            {
                logger.LogError(ex, "AggregateException");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception");
            }
            finally
            {
                skStackClient?.Close();
            }
            Task.WaitAll(Task.Delay(-1));
        }

    }
}
