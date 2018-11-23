using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RJCP.IO.Ports;
using System;
using System.Threading.Tasks;

namespace SkstackIpDotNet.Example
{
    class Program
    {
        static void ConfigureServices(IServiceCollection serviceCollection)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false)
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
            serviceCollection.AddTransient<SKDevice>();
        }
        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<Program>();
            try
            {

                //Windows
                string devicePort = "COM3";
                if (System.Environment.OSVersion.Platform.ToString() != "Win32NT")
                {
                    //Linux等(Raspbian Stretchで動作確認)
                    devicePort = "/dev/ttyUSB0";
                }

                using (var skdevice = serviceProvider.GetService<SKDevice>())
                {
                    var program = new Program(logger, skdevice);
                    skdevice.Open(devicePort, 115200, 8, Parity.None, StopBits.One);
                    skdevice.OnEVENTReceived += (sender, e) =>
                    {
                        logger.LogDebug(e.ToString());
                    };
                    skdevice.OnERXUDPReceived += (sender, e) =>
                    {
                        logger.LogDebug(e.ToString());
                    };
                    program.ExecuteAsync().Wait();
                }
            }
            catch (AggregateException ex)
            {
                logger.LogError(ex, "AggregateException");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception");
            }
            Console.ReadKey();
        }

        private SKDevice _skDevice;
        private ILogger<Program> _logger;

        private Program(ILogger<Program> logger, SKDevice skDevice)
        {
            _logger = logger;
            _skDevice = skDevice;
        }

        public async Task ExecuteAsync()
        {
            var eInfo = await _skDevice.SKInfoAsync();
            _logger.LogInformation(eInfo.ToString());

            var eVer = await _skDevice.SKVerAsync();
            _logger.LogInformation(eVer.ToString());

            var eaddr = await _skDevice.SKTableEAddrAsync();
            _logger.LogInformation(eaddr.ToString());

            var ehandle = await _skDevice.SKTableEHandleAsync();
            _logger.LogInformation(ehandle.ToString());

            var enbr = await _skDevice.SKTableENbrAsync();
            _logger.LogInformation(enbr.ToString());

            var eneighbor = await _skDevice.SKTableENeighborAsync();
            _logger.LogInformation(eneighbor.ToString());

            var esec = await _skDevice.SKTableESecAsync();
            _logger.LogInformation(esec.ToString());
        }
    }
}
