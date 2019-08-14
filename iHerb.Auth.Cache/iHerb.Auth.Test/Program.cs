using iHerb.Auth.Cache;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace iHerb.Auth.Test
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var config = LoadConfig();
            var oper= RedisCacheProvider.CreateInstance(config);
            await oper.UpdateOrAddAsync<string>("lh", "Test");

        }

        static IConfigurationRoot LoadConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = builder.Build();
            return config;
        }
    }
}
