using System;
using System.Collections.Generic;
using XNode;

namespace CometUI
{
    [Serializable]
    abstract class BasePort
    {
        public abstract Dir Direction { get; }
        public abstract bool CanConnect(NodePort from, NodePort to);
        public abstract int Order { get; }
        public virtual string SubMenu => "";
    }

    public enum Dir
    {
        Output, Input
    }

    static class PortExtensions
    {
        static Dictionary<Type, BasePort> portTypeToInstance = new Dictionary<Type, BasePort>();

        public static BasePort GetPortInfo(this Type type)
        {
            BasePort instance = null;
            if (!portTypeToInstance.TryGetValue(type, out instance))
                instance = portTypeToInstance[type] = (BasePort)Activator.CreateInstance(type);

            return instance;
        }
    }
}