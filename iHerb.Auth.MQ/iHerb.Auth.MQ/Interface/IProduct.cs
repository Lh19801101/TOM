using System;
using System.Collections.Generic;
using System.Text;

namespace iHerb.Auth.MQ.Interface
{
    public interface IProduct
    {
        /// <summary>
        ///     发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="qName">队列名称</param>
        /// <param name="msg"></param>
         void SendMessage<T>(string qName, T msg, SerializeType tyep= SerializeType.Json) where T : class;

        /// <summary>
        ///  发送消息
        /// </summary>
        /// <typeparam name="T">用户自定义内容信息类型</typeparam>
        /// <param name="exchangeName">交换器名</param>
        /// <param name="routingKey">路由键（*匹配一个词，#匹配多个词）</param>
        /// <param name="qName"></param>
        /// <param name="msg"></param>
        /// <param name="exchangeType">交换器类型(direct,fanout,topic,headers)</param>
        void SendMessage<T>(string exchangeName, T msg, string exchangeType, string qName = "", string routingKey = "", SerializeType type = SerializeType.Json) where T : class;
    }

    public enum SerializeType
    {
        Json,
        Protobuf
    }
}
