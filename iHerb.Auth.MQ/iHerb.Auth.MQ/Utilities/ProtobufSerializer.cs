using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace iHerb.Auth.MQ.Utilities
{
    public class ProtobufSerializer
    {
        /// <summary>
        /// 序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string Serialize<T>(T t)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize<T>(ms, t);
                return Encoding.Unicode.GetString(ms.ToArray());
            }
        }

        public static Byte[] SerializeBytes<T>(T t)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize<T>(ms, t);
                return ms.ToArray();
            }
        }
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content"></param>
        /// <returns></returns>
        public static T DeSerialize<T>(byte[] msg)
        {
            using (MemoryStream ms = new MemoryStream(msg))
            {
                T t = Serializer.Deserialize<T>(ms);
                return t;
            }
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content"></param>
        /// <returns></returns>
        public static T DeSerializeBytes<T>(byte[] content)
        {
            using (MemoryStream ms = new MemoryStream(content))
            {
                T t = Serializer.Deserialize<T>(ms);
                return t;
            }
        }
    }
}
