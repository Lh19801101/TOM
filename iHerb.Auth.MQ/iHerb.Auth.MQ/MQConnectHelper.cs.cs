using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace iHerb.Auth.MQ
{
   internal class MQConnectHelper
    {
        private static object Lockobject = new object();
        private static IConnection connection;
        private static MQConfig mqConfig;
        private MQConnectHelper()
        {

        }

        public static int IsTask { private set; get; }
       public static MQConnectHelper CreateInstance(IConfiguration config)
        {
            if(connection == null)
            {
                lock(Lockobject)
                {
                    if(connection==null)
                    {
                        if (config == null)
                        {
                            throw new ArgumentException("MQ：Configuration is not Exists");
                        }
                        mqConfig = config.GetSection("MQConfig").Get<MQConfig>();
                        Valieddate();
                        connection = CreateConnection();
                    }
                  
                }
              
            }
           
            return new MQConnectHelper();
        }

        public   IConnection GetConnection()
        {
            if(connection!=null&& connection.IsOpen)
            {
                return connection;
            }
            Valieddate();
            connection = null;
            lock (Lockobject)
            {
                if(connection==null)
                {
                    connection = CreateConnection();
                }
            }
            return connection;
        }
        private class MQConfig
        {
            public string HostName { get; set; }

            public string Password { get; set; }

            public string UserName { get; set; }

            public int IsTask { get; set; }
        }

        private static IConnection CreateConnection()
        {
            
            var connFactory = new ConnectionFactory
            {
                HostName = mqConfig.HostName,
                UserName = mqConfig.UserName,
                Password = mqConfig.Password,
                //请求脉搏
                RequestedHeartbeat = 60,
                //自动重连
                AutomaticRecoveryEnabled = true
            };
            connection = connFactory.CreateConnection();
            return connection;
        }

        private static void Valieddate()
        {
            if (mqConfig == null)
            {
                throw new ArgumentException("MQ：mqConfig is not Exists");
            }
            if (string.IsNullOrWhiteSpace(mqConfig.HostName)
                || string.IsNullOrWhiteSpace(mqConfig.Password)
                || string.IsNullOrWhiteSpace(mqConfig.UserName))
            {
                throw new ArgumentException("MQ：mqConfig is Error");
            }
            IsTask = mqConfig.IsTask;
        }


    }
   
   
}
