using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace mRPC
{
    /// <summary>
    /// A connection to a client that can send and receive JSON messages.
    /// </summary>
    public interface IConnection
    {
        bool Closed { get; }
        HttpContext Context { get; }
        Task Send(JObject message);
        Task<JObject> Receive();
    }
}
