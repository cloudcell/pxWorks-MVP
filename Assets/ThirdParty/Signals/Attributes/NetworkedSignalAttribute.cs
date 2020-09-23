using System;

namespace Signals
{
    /// <summary>
    /// Publish this signal for all players in room
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NetworkedSignalAttribute : Attribute
    {
    }
}