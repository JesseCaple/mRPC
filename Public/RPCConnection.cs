using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Claims;
using System;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace mRPC
{

    /// <summary>
    /// Non-generic internal base of <see cref="RPCConnection{T}"/>
    /// </summary>
    internal interface IRPCConnection
    {
        IConnection Connection { get; }
    }

    /// <summary>
    /// A connection that can receive RPC calls
    /// </summary>
    public class RPCConnection<T> : IRPCConnection where T : Controller
    {
        private IConnection _connection;
        IConnection IRPCConnection.Connection
        {
            get
            {
                return _connection;
            }
        }

        public dynamic DynamicRPC { get; }
        public ClaimsPrincipal User { get; }

        internal RPCConnection(IConnection connection)
        {
            _connection = connection;
            User = connection.Context.User;
            DynamicRPC = new DynamicRouter<T>(this);
        }

    }
}