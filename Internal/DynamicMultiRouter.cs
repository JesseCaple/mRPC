using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace mRPC
{
    /// <summary>
    /// Performs RPC calls for all connections on a specific <see cref="RPCManager{T}"/>
    /// </summary>
    internal class DynamicMultiRouter<T> : DynamicObject where T : Controller
    {
        private readonly RPCManager<T> _manager;
        private readonly string _controller;

        internal DynamicMultiRouter(RPCManager<T> tube)
        {
            _manager = tube;
            _controller = typeof(T).Name;
            if (_controller.EndsWith("Controller"))
            {
                _controller = _controller.Substring(
                    0, _controller.Length - 10);
            }
        }

        public override bool TryInvokeMember(
            InvokeMemberBinder binder,
            object[] args,
            out object result)
        {
            var message = new JObject();
            message.Add("Intent", "Call");
            message.Add("Controller", _controller);
            message.Add("Action", binder.Name);
            var parameters = JToken.FromObject(args);
            message.Add("Parameters", parameters);
            var tasks = new List<Task>();
            foreach (IRPCConnection client in _manager.Connections)
            {
                tasks.Add(
                    client.Connection.Send(message)
                );
            }
            result = Task.WhenAll(tasks.ToArray());
            return true;
        }
    }
}
