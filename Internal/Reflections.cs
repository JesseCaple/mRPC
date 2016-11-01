using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace mRPC
{
    /// <summary>
    /// Utility class to perform assembly reflection
    /// </summary>
    internal static class Reflections
    {
        /// <summary>
        /// Constructs a collection of <see cref="Type"/>s (of <see cref="RPCManager{T}"/>) for all <see cref="Controller"/>s
        /// in the entry assembly that either define the <see cref="RPCAttribute"/> themselves, or contain a member
        /// function that defines the <see cref="RPCAttribute"/>
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<Type> GetRPCManagerTypes()
        {
            return
                Assembly.GetEntryAssembly().GetTypes()
                .Where(type => typeof(Controller).IsAssignableFrom(type))
                .Where(m => m.GetTypeInfo().IsDefined(typeof(RPCAttribute), false))
                .Union
                (
                    Assembly.GetEntryAssembly().GetTypes()
                    .Where(type => typeof(Controller).IsAssignableFrom(type))
                    .SelectMany(t => t.GetMethods())
                    .Where(m => m.IsDefined(typeof(RPCAttribute), false))
                    .Select(m => m.DeclaringType)
                )
                .Distinct()
                .Select(t => typeof(RPCManager<>).MakeGenericType(t));
        }
    }
}
