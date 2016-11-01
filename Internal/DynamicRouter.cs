using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Dynamic;

namespace mRPC
{
    /// <summary>
    /// Performs RPC calls for a specific <see cref="RPCConnection{T}"/>
    /// </summary>
    internal class DynamicRouter<T> : DynamicObject where T : Controller
    {
        private readonly IRPCConnection _connection;
        private readonly string _controller;

        internal DynamicRouter(IRPCConnection connection)
        {
            _connection = connection;
            _controller = typeof(T).Name;
            if (_controller.EndsWith("Controller"))
            {
                _controller = _controller.Substring(0, _controller.Length - 10);
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
            result = _connection.Connection.Send(message);
            return true;
        }
    }
}
