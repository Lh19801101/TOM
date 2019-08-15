using System;
using System.Collections.Generic;
using System.Text;

namespace iHerb.Auth.MQ.Interface
{
    public interface IConsume
    {
        void Listen<T>(string qName, Func<T, bool> allwaysRunAction, SerializeType tyep = SerializeType.Json) where T : class;


        void Listen<T>(string exchangeName, Func<T, bool> allwaysRunAction, string exchangeType, string routingKey = "", SerializeType type = SerializeType.Json) where T : class;

    }
}
