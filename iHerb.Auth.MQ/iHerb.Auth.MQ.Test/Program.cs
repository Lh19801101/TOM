using Microsoft.Extensions.Configuration;
using Polly;
using System;
using System.IO;

namespace iHerb.Auth.MQ.Test
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var config = LoadConfig();
            var msg = new mytest();
            msg.Num = 123;
            var maoper=MQProvider.CreateInstance(config);
            //maoper.SendMessage("lh", msg);
            maoper.Listen<mytest>("lh", GetMsg);

        }

        static IConfigurationRoot LoadConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = builder.Build();
            return config;
        }

        public class mytest
        {
            public int Num { get; set; } = 0;
        }

        public static bool GetMsg(mytest ms)
        {
            Console.WriteLine("Hello World2!");
            return true;
        }
    }
}
