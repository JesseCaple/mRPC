using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace mRPC
{
    /// <summary>
    /// MVCRPC extensions to <see cref="IServiceCollection"/> and <see cref="IApplicationBuilder"/>
    /// </summary>
    static public class BuilderExtensions
    {
        /// <summary>
        /// Adds Tubes services.
        /// </summary>
        public static void AddRPC(this IServiceCollection services)
        {
            services.AddSingleton<RPCServer>();
            foreach (var type in Reflections.GetRPCManagerTypes())
            {
                services.AddSingleton(type);
            }
        }

        /// <summary>
        /// Adds middleware that connects clients via WebSockets.
        /// </summary>
        public static IApplicationBuilder UseRPCWebSockets(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WebSocketMiddleware>();
        }
    }
}
