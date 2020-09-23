using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;
using Object = System.Object;

namespace Signals
{
    public static class BinarySerializationHelper
    {
        #region Private fields
        private static SurrogateSelector SurrogateSelector;
        #endregion

        public static byte[] Serialize(this byte val) => new byte[] { val };
        public static byte[] Serialize(this sbyte val) => new byte[] { (byte)val };
        public static byte[] Serialize(this byte[] val) => val ?? new byte[0];
        public static byte[] Serialize(this bool val) => BitConverter.GetBytes(val);
        public static byte[] Serialize(this char val) => BitConverter.GetBytes(val);
        public static byte[] Serialize(this double val) => BitConverter.GetBytes(val);
        public static byte[] Serialize(this short val) => BitConverter.GetBytes(val);
        public static byte[] Serialize(this int val) => BitConverter.GetBytes(val);
        public static byte[] Serialize(this long val) => BitConverter.GetBytes(val);
        public static byte[] Serialize(this float val) => BitConverter.GetBytes(val);
        public static byte[] Serialize(this ushort val) => BitConverter.GetBytes(val);
        public static byte[] Serialize(this uint val) => BitConverter.GetBytes(val);
        public static byte[] Serialize(this ulong val) => BitConverter.GetBytes(val);
        public static byte[] Serialize(this string val, Encoding encoding = null) => encoding == null ? Encoding.UTF8.GetBytes(val ?? "") : encoding.GetBytes(val ?? "");
        public static byte[] Serialize(this Guid val) => val.ToByteArray();
        public static byte[] Serialize(this Vector3 val) => JoinArrays(BitConverter.GetBytes(val.x), BitConverter.GetBytes(val.y), BitConverter.GetBytes(val.z));
        public static byte[] Serialize(this Vector2 val) => JoinArrays(BitConverter.GetBytes(val.x), BitConverter.GetBytes(val.y));
        public static byte[] Serialize(this Quaternion val) => JoinArrays(BitConverter.GetBytes(val.x), BitConverter.GetBytes(val.y), BitConverter.GetBytes(val.z), BitConverter.GetBytes(val.w));

        /// <summary>Serialize object to byte[]</summary>
        public static byte[] Serialize(this object obj, Encoding encoding = null)
        {
            if (obj == null)
                return new byte[0];

            var type = obj.GetType();

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte: return Serialize((byte)obj);
                case TypeCode.SByte: return Serialize((sbyte)obj);
                case TypeCode.Boolean: return Serialize((bool)obj);
                case TypeCode.Char: return Serialize((char)obj);
                case TypeCode.Double: return Serialize((double)obj);
                case TypeCode.Int16: return Serialize((short)obj);
                case TypeCode.Int32: return Serialize((int)obj);
                case TypeCode.Int64: return Serialize((long)obj);
                case TypeCode.Single: return Serialize((float)obj);
                case TypeCode.UInt16: return Serialize((ushort)obj);
                case TypeCode.UInt32: return Serialize((uint)obj);
                case TypeCode.UInt64: return Serialize((ulong)obj);
                case TypeCode.String: return Serialize((string)obj, encoding);
            }

            if (type == typeof(Guid)) return Serialize((Guid)obj);
            if (type == typeof(Vector3)) return Serialize((Vector3)obj);
            if (type == typeof(Vector2)) return Serialize((Vector2)obj);
            if (type == typeof(Quaternion)) return Serialize((Quaternion)obj);
            if (type == typeof(byte[])) return Serialize((byte[])obj);

            //serialize as complex object
            using (var ms = new MemoryStream())
            {
                if (SurrogateSelector == null)
                    CreateSurrogateSelector();

                var formatter = new BinaryFormatter();
                formatter.SurrogateSelector = SurrogateSelector;
                formatter.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        /// <summary>Deserialize byte[] to object</summary>
        public static T DeSerialize<T>(this byte[] bytes, Encoding encoding = null)
        {
            if (bytes == null || bytes.Length == 0)
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)string.Empty;
                else
                    return default(T);
            }

            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Byte: return (T)(object)bytes[0];
                case TypeCode.SByte: return (T)(object)bytes[0];
                case TypeCode.Boolean: return (T)(object)BitConverter.ToBoolean(bytes, 0);
                case TypeCode.Char: return (T)(object)BitConverter.ToChar(bytes, 0);
                case TypeCode.Double: return (T)(object)BitConverter.ToDouble(bytes, 0);
                case TypeCode.Int16: return (T)(object)BitConverter.ToInt16(bytes, 0);
                case TypeCode.Int32: return (T)(object)BitConverter.ToInt32(bytes, 0);
                case TypeCode.Int64: return (T)(object)BitConverter.ToInt64(bytes, 0);
                case TypeCode.Single: return (T)(object)BitConverter.ToSingle(bytes, 0);
                case TypeCode.UInt16: return (T)(object)BitConverter.ToUInt16(bytes, 0);
                case TypeCode.UInt32: return (T)(object)BitConverter.ToUInt32(bytes, 0);
                case TypeCode.UInt64: return (T)(object)BitConverter.ToUInt64(bytes, 0);
                case TypeCode.String: return (T)(object)(encoding == null ? Encoding.UTF8.GetString(bytes) : encoding.GetString(bytes));
            }

            var type = typeof(T);
            if (type == typeof(Guid)) return (T)(object)new Guid(bytes);
            if (type == typeof(Vector3)) return (T)(object)new Vector3(BitConverter.ToSingle(bytes, 0), BitConverter.ToSingle(bytes, 4), BitConverter.ToSingle(bytes, 8));
            if (type == typeof(Vector2)) return (T)(object)new Vector2(BitConverter.ToSingle(bytes, 0), BitConverter.ToSingle(bytes, 4));
            if (type == typeof(Quaternion)) return (T)(object)new Quaternion(BitConverter.ToSingle(bytes, 0), BitConverter.ToSingle(bytes, 4), BitConverter.ToSingle(bytes, 8), BitConverter.ToSingle(bytes, 12));
            if (type == typeof(byte[])) return (T)(object)bytes;

            //deserialize as complex object
            using (var ms = new MemoryStream(bytes))
            {
                if (SurrogateSelector == null)
                    CreateSurrogateSelector();

                var formatter = new BinaryFormatter();
                formatter.SurrogateSelector = SurrogateSelector;
                return (T)formatter.Deserialize(ms);
            }
        }

        private static void CreateSurrogateSelector()
        {
            SurrogateSelector = new SurrogateSelector();

            SurrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), new SerializationSurrogateVector3());
            SurrogateSelector.AddSurrogate(typeof(Vector2), new StreamingContext(StreamingContextStates.All), new SerializationSurrogateVector2());
            SurrogateSelector.AddSurrogate(typeof(Quaternion), new StreamingContext(StreamingContextStates.All), new SerializationSurrogateQuaternion());
        }

        #region Serialization Surrogates

        sealed class SerializationSurrogateVector3 : ISerializationSurrogate
        {
            public void GetObjectData(Object obj, SerializationInfo info, StreamingContext context)
            {
                var val = (Vector3)obj;
                info.AddValue("x", val.x);
                info.AddValue("y", val.y);
                info.AddValue("z", val.z);
            }

            public Object SetObjectData(Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var val = (Vector3)obj;
                val.x = info.GetSingle("x");
                val.y = info.GetSingle("y");
                val.z = info.GetSingle("z");
                return val;
            }
        }

        sealed class SerializationSurrogateVector2 : ISerializationSurrogate
        {
            public void GetObjectData(Object obj, SerializationInfo info, StreamingContext context)
            {
                var val = (Vector2)obj;
                info.AddValue("x", val.x);
                info.AddValue("y", val.y);
            }

            public Object SetObjectData(Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var val = (Vector2)obj;
                val.x = info.GetSingle("x");
                val.y = info.GetSingle("y");
                return val;
            }
        }

        sealed class SerializationSurrogateQuaternion : ISerializationSurrogate
        {
            public void GetObjectData(Object obj, SerializationInfo info, StreamingContext context)
            {
                var val = (Quaternion)obj;
                info.AddValue("x", val.x);
                info.AddValue("y", val.y);
                info.AddValue("z", val.z);
                info.AddValue("w", val.w);
            }

            public Object SetObjectData(Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var val = (Quaternion)obj;
                val.x = info.GetSingle("x");
                val.y = info.GetSingle("y");
                val.z = info.GetSingle("z");
                val.w = info.GetSingle("w");
                return val;
            }
        }

        #endregion

        #region Utils

        public static byte[] JoinArrays(byte[] arr1, byte[] arr2)
        {
            var len1 = arr1.Length;
            Array.Resize(ref arr1, arr1.Length + arr2.Length);
            Array.Copy(arr2, 0, arr1, len1, arr2.Length);
            return arr1;
        }

        public static byte[] JoinArrays(byte[] arr1, byte[] arr2, byte[] arr3)
        {
            var len1 = arr1.Length;
            var len2 = arr2.Length;
            Array.Resize(ref arr1, arr1.Length + arr2.Length + arr3.Length);
            Array.Copy(arr2, 0, arr1, len1, arr2.Length);
            Array.Copy(arr3, 0, arr1, len1 + len2, arr3.Length);
            return arr1;
        }

        public static byte[] JoinArrays(byte[] arr1, byte[] arr2, byte[] arr3, byte[] arr4)
        {
            var len1 = arr1.Length;
            var len2 = arr2.Length;
            var len3 = arr3.Length;
            Array.Resize(ref arr1, arr1.Length + arr2.Length + arr3.Length + arr4.Length);
            Array.Copy(arr2, 0, arr1, len1, arr2.Length);
            Array.Copy(arr3, 0, arr1, len1 + len2, arr3.Length);
            Array.Copy(arr4, 0, arr1, len1 + len2 + len3, arr4.Length);
            return arr1;
        }
        #endregion
    }
}