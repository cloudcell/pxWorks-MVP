using System;

namespace Signals
{
    /// <summary>
    /// Do not show the Signal in Log
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class HideInLogAttribute : Attribute
    {
    }
}