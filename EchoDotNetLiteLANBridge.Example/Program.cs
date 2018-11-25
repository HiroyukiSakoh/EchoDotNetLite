using EchoDotNetLite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EchoDotNetLiteLANBridge.Example
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
            serviceCollection.AddSingleton<LANClient>();
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

            var lanClient = serviceProvider.GetService<LANClient>();
            serviceCollection.AddSingleton<IPANAClient, LANClient>(f => lanClient);
            serviceCollection.AddSingleton<EchoClient>();
            serviceProvider = serviceCollection.BuildServiceProvider();
            try
            {
                Task.Run(() => serviceProvider.GetService<Example>().ExecuteAsync());
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
                lanClient?.Dispose();
            }
            Task.WaitAll(Task.Delay(-1));
        }
    }
}
