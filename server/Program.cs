namespace Paradigm.Server
{
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using StructureMap;
    using System;

    public class WebApp
    {
        internal static IConfigurationRoot Configuration;

        public static void Main(string[] args)
        {
            var root = Directory.GetCurrentDirectory();

            Configuration = new ConfigurationBuilder()
                .SetBasePath(root)
                .AddConfiguration()
                .Build();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var root = Directory.GetCurrentDirectory();
            IConfigurationSection logging = Configuration.GetSection("Logging");

            return Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new StructureMapServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .ConfigureAppConfiguration((context, config) =>
                        {
                            config.Sources.Clear();
                            config.AddConfiguration(Configuration);
                        })
                        .ConfigureLogging(factory =>
                        {
                            if (logging.GetSection("Debug").Exists())
                                factory.AddConsole();
                            if (logging.GetSection("Console").Exists())
                                factory.AddDebug();
                        })
                        .UseKestrel()
                        .UseContentRoot(root)
                        .UseStartup<Startup>();
                });
        }
    }

    public class StructureMapServiceProviderFactory : IServiceProviderFactory<Registry>
    {
        public Registry CreateBuilder(IServiceCollection services)
        {
            var registry = new Registry();
            registry.Populate(services);
            return registry;
        }

        public IServiceProvider CreateServiceProvider(Registry containerRegistry)
        {
            var container = new Container(c =>
            {
                c.AddRegistry(containerRegistry);
                c.AddRegistry<ContainerRegistry>();
                c.AddRegistry<Data.ContainerRegistry>();
                c.AddRegistry<Common.ContainerRegistry>();
                c.AddRegistry<Service.ContainerRegistry>();
            });

            return container.GetInstance<IServiceProvider>();
        }
    }
}