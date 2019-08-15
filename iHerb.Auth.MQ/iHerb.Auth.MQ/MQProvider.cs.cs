using iHerb.Auth.MQ.Interface;
using iHerb.Auth.MQ.Utilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iHerb.Auth.MQ
{
    public class MQProvider : IProduct, IConsume
    {
        private MQProvider()
        {

        }

        private static MQConnectHelper connHelper;

        private static int _maxRecord = 1;

        static MQProvider()
        {
            TaskDo();
        }


        private static readonly ConcurrentQueue<Task> TasksCache = new ConcurrentQueue<Task>(); //缓存队列
        public static MQProvider CreateInstance(IConfiguration config)
        {
            if(connHelper == null)
            {
                connHelper = MQConnectHelper.CreateInstance(config);
            }
            return new MQProvider();
        }

        public void Listen<T>(string qName, Func<T, bool> allwaysRunAction, SerializeType type = SerializeType.Json) where T : class
        {
            if (allwaysRunAction == null) return;
           
            var listenChannel = connHelper.GetConnection().CreateModel();

            listenChannel.QueueDeclare(qName, true, false, false, null);
            var consumer = new EventingBasicConsumer(listenChannel);
            consumer.Received += (model, ea) =>
            {
                var message = DeSerialize<T>(type,ea.Body);
                if (MQConnectHelper.IsTask>0)
                {
                    Execute(listenChannel, allwaysRunAction, message, ea.DeliveryTag);
                }
                else
                {
                    var task = new Task(
                        () => { Execute(listenChannel, allwaysRunAction, message, ea.DeliveryTag, qName); });
                    TasksCache.Enqueue(task);
                }
            };
            //公平分发,不要同一时间给一个工作者发送多于一个消息
            listenChannel.BasicQos(0, 50, false);
            // 消费消息；false 为手动应答 
            listenChannel.BasicConsume(qName, false, consumer);
        }

        public void Listen<T>(string exchangeName, Func<T, bool> allwaysRunAction, string exchangeType, string routingKey="", SerializeType type = SerializeType.Json) where T : class
        {
            using (var channel = connHelper.GetConnection().CreateModel())
            {
                channel.ExchangeDeclare(exchangeName, exchangeType);
                var queueName = channel.QueueDeclare(exclusive: true, autoDelete: false).QueueName;
                channel.QueueBind(queueName, exchangeName, routingKey);
                //定义这个队列的消费者
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var message = DeSerialize<T>(type, ea.Body);
                    if (MQConnectHelper.IsTask > 0)
                    {
                        Execute(channel, allwaysRunAction, message, ea.DeliveryTag);
                    }
                    else
                    {
                        var task = new Task(
                            () => { Execute(channel, allwaysRunAction, message, ea.DeliveryTag, queueName); });
                        TasksCache.Enqueue(task);
                    }
                };
                //公平分发,不要同一时间给一个工作者发送多于一个消息
                channel.BasicQos(0, 50, false);
                // 消费消息；false 为手动应答 
                channel.BasicConsume(queueName, false, consumer);
            }
        }

        public void SendMessage<T>(string qName, T msg, SerializeType type = SerializeType.Json) where T : class
        {
            if (string.IsNullOrEmpty(qName) || msg == null) return;
            var body = SerializeObject(type, msg);
            Action a1 = () =>
            {
                using (var channel = connHelper.GetConnection().CreateModel())
                {
                    channel.QueueDeclare(qName, durable:true, false, false, null);
                    var basicProperties = new BasicProperties { Persistent = true };
                    //RabbitMQ有一个默认的exchange="",此时qName即为routingKey
                    channel.BasicPublish("", qName, basicProperties, body);
                }
            };
            PolicyHelper.GetRetryTimesPolicy(3, ex =>
            {
                var task = new Task(a1);
                TasksCache.Enqueue(task);

            }).Execute(() => { a1.Invoke(); });
        }

        public void SendMessage<T>(string exchangeName, T msg, string exchangeType, string qName="", string routingKey="",  SerializeType type = SerializeType.Json) where T : class
        {
            if (string.IsNullOrEmpty(exchangeName) || msg == null) return;
            var body = SerializeObject(type, msg);
            Action a1 = () =>
            {
                using (var channel = connHelper.GetConnection().CreateModel())
                {
                    //设置交换器的类型
                    channel.ExchangeDeclare(exchangeName, exchangeType);
                    if(!string.IsNullOrWhiteSpace(qName))
                    {
                        //声明一个队列，设置队列是否持久化，排他性，与自动删除
                        channel.QueueDeclare(qName, durable: true, false, false, null);
                        //绑定消息队列，交换器，routingkey
                        channel.QueueBind(qName, exchangeName, routingKey);
                    }
                    var properties = channel.CreateBasicProperties();
                    //队列持久化
                    properties.Persistent = true;
                    //发送信息
                    channel.BasicPublish(exchangeName, routingKey, properties, body);
                }
            };
            PolicyHelper.GetRetryTimesPolicy(3, ex =>
            {
                var task = new Task(a1);
                TasksCache.Enqueue(task);

            }).Execute(() => { a1.Invoke(); });
           
        }


        private byte[] SerializeObject<T>(SerializeType type,T msg)
        {
            byte[] msgbytes;
            switch(type)
            {
                case SerializeType.Json:
                    var msgstr = JsonConvert.SerializeObject(msg);
                    msgbytes =  Encoding.UTF8.GetBytes(msgstr);
                    break;
                default:
                    msgbytes = ProtobufSerializer.SerializeBytes(msg);
                    break;
            }
            return msgbytes;
        }

        private T DeSerialize<T>(SerializeType type,byte[] msg)
        {
            T data=default(T);
            switch (type)
            {
                case SerializeType.Json:
                    var message = Encoding.UTF8.GetString(msg);
                    data = JsonConvert.DeserializeObject<T>(message);
                    break;
                default:
                    data = ProtobufSerializer.DeSerialize<T>(msg);
                    break;
            }
            return data;
        }

        private static void TaskDo()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    _maxRecord++;
                    if (_maxRecord % 2000 == 0)
                    {
                        Thread.Sleep(2000);
                        _maxRecord = 1;
                    }
                    Task item;
                    var result = TasksCache.TryDequeue(out item);
                    if (result)
                        try
                        {
                            item.Start();
                        }
                        catch
                        {
                            TasksCache.Enqueue(item);
                        }
                }
            });
        }

        /// <summary>
        ///     执行方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listenChannel"></param>
        /// <param name="allwaysRunAction"></param>
        /// <param name="resp"></param>
        /// <param name="deliveryTag"></param>
        /// <param name="consumer"></param>
        private void Execute<T>(IModel listenChannel, Func<T, bool> allwaysRunAction, T resp, ulong deliveryTag,
            string qName = null, EventingBasicConsumer consumer = null)
        {
            if (listenChannel == null) return;
            var isSuccess = allwaysRunAction(resp);
            //手动应答的时候  需要加上如下代码
            if (isSuccess)
                listenChannel.BasicAck(deliveryTag, false);
            else
                listenChannel.BasicNack(deliveryTag, false, true);
        }

      
    }
}
