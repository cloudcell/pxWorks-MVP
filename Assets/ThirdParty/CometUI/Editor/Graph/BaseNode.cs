using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;

namespace CometUI
{
    [NodeWidth(150)]
    public class BaseNode: XNode.Node
    {
        /// <summary>GUID of the node</summary>
        [HideInInspector]
        public string NodeId { get; set; }

        public virtual IEnumerable<(Type, string)> GetAllowedAddPorts()
        {
            yield break;
        }

        public virtual void OnCreated()
        {
        }

        public override object GetValue(NodePort port)
        {
            return 1;
        }

        public override void OnCreateConnection(NodePort from, NodePort to)
        {
            base.OnCreateConnection(from, to);
        }

        protected void AddDynamic(Type portType, string fieldName)
        {
            var desc = portType.GetPortInfo();
            NodePort port;
            if (desc.Direction == Dir.Input)
                port = AddDynamicInput(portType, fieldName: fieldName);
            else
                port = AddDynamicOutput(portType, fieldName: fieldName);
        }

        [ContextMenu("Delete unused ports", false, 1)]
        public void DeleteUnusedPorts()
        {
            foreach (var port in this.Ports.ToArray())
                if (port.ConnectionCount == 0)
                    RemoveDynamicPort(port);
        }
    }
}