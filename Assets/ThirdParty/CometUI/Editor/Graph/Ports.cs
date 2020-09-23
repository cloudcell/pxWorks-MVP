using System;
using System.Collections.Generic;
using XNode;

namespace CometUI
{
    [Serializable]
    class BindInputPort : BasePort
    {
        public override Dir Direction => Dir.Input;

        public override int Order => 100;

        public override bool CanConnect(NodePort from, NodePort to) => true;
    };

    [Serializable]
    class BindOutputPort : BasePort
    {
        public override Dir Direction => Dir.Output;

        public override int Order => 150;

        public override bool CanConnect(NodePort from, NodePort to)
        {
            if (from.node == to.node)
                return false;
            return to.ValueType == typeof(BindInputPort);
        }
    };

    [Serializable]
    class ActionInputPort : BasePort
    {
        public override Dir Direction => Dir.Input;

        public override int Order => 300;

        public override bool CanConnect(NodePort from, NodePort to) => true;
    };

    [Serializable]
    class ActionOutputPort : BasePort
    {
        public override Dir Direction => Dir.Output;

        public override int Order => 400;

        public override bool CanConnect(NodePort from, NodePort to)
        {
            return to.ValueType == typeof(ActionInputPort);
        }
    };

    [Serializable]
    class EventPort : BasePort
    {
        public override Dir Direction => Dir.Output;

        public override int Order => 405;

        public override bool CanConnect(NodePort from, NodePort to)
        {
            return to.ValueType == typeof(ActionInputPort);
        }
    };

    [Serializable]
    class GesturePort : BasePort
    {
        public override Dir Direction => Dir.Output;

        public override int Order => 410;
        public override string SubMenu => "Gestures";

        public override bool CanConnect(NodePort from, NodePort to)
        {
            return to.ValueType == typeof(ActionInputPort);
        }
    };
}