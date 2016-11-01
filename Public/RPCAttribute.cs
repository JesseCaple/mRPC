using System;

namespace mRPC
{
    /// <summary>
    /// Remote procedure call marker.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
    public class RPCAttribute : Attribute
    {

    }
}
