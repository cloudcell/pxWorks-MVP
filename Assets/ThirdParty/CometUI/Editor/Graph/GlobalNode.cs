using System;
using System.Collections.Generic;

namespace CometUI
{
    [NodeTint("#664444")]
    [NodeWidth(100)]
    public class GlobalNode : BaseNode
    {
        public void Build()
        {
            AddDynamic(typeof(ActionInputPort), "Back");
        }

        public override IEnumerable<(Type, string)> GetAllowedAddPorts()
        {
            yield return (typeof(ActionInputPort), "Back");
            yield return (typeof(ActionInputPort), "CloseApp");
        }
    }
}