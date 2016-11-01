using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Linq;

namespace mRPC
{
    /// <summary>
    /// Non-generic internal base of <see cref="IRPCManager{T}"/>
    /// </summary>
    internal interface IRPCManager
    {
        Type SubType { get; }
        string Name { get; }
        void Upgrade(IConnection connection);
        void Downgrade(IConnection connection);
        ImmutableDictionary<string, MethodInfo> Actions { get; }
    }

    /// <summary>
    /// Serves server-to-client and client-to-server RPC calls for <see cref="Controller"/> of type <see cref="{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RPCManager<T> : IRPCManager
        where T : Controller
    {
        private readonly ConcurrentDictionary<IConnection, RPCConnection<T>> _clients;

        public dynamic DynamicRPC { get; }

        public RPCManager()
        {
            _subType = typeof(T);
            _name = _subType.Name;
            if (_name.EndsWith("Controller"))
            {
                _name = _name.Substring(0, _name.Length - 10);
            }
            _actions =
                typeof(T).GetMethods()
                .Where(x => x.IsDefined(typeof(RPCAttribute), false))
                .ToImmutableDictionary(x => x.Name, x => x);
            _clients =
                new ConcurrentDictionary<IConnection, RPCConnection<T>>();
            DynamicRPC = new DynamicMultiRouter<T>(this);
        }

        public IEnumerable<RPCConnection<T>> Clients
        {
            get
            {
                return _clients.Values;
            }
        }

        // internal
        private readonly Type _subType;
        Type IRPCManager.SubType
        {
            get
            {
                return _subType;
            }
        }

        // internal
        private readonly string _name;
        string IRPCManager.Name
        {
            get
            {
                return _name;
            }
        }

        // internal
        internal readonly ImmutableDictionary<string, MethodInfo> _actions;
        ImmutableDictionary<string, MethodInfo> IRPCManager.Actions
        {
            get
            {
                return _actions;
            }
        }

        // internal
        void IRPCManager.Upgrade(IConnection connection)
        {
            var upgraded = new RPCConnection<T>(connection);
            _clients.TryAdd(connection, upgraded);
        }

        // internal
        void IRPCManager.Downgrade(IConnection connection)
        {
            RPCConnection<T> upgraded;
            _clients.TryRemove(connection, out upgraded);
        }

    }
}
