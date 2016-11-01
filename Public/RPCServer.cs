using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace mRPC
{
    /// <summary>
    /// Main RPC hub
    /// </summary>
    public class RPCServer
    {
        private readonly ILogger<RPCServer> _logger;
        private readonly IServiceProvider _services;
        private readonly IControllerFactory _factory;
        private readonly IAuthorizationService _authorization;

        private readonly ImmutableDictionary<string, IRPCManager> _managers;

        public RPCServer(
            ILogger<RPCServer> logger,
            IServiceProvider services,
            IControllerFactory factory,
            IAuthorizationService authorization)
        {
            _logger = logger;
            _services = services;
            _factory = factory;
            _authorization = authorization;
            _managers =
                Reflections.GetRPCManagerTypes()
                .Select(x => (IRPCManager)_services.GetService(x))
                .ToImmutableDictionary(x => x.Name, x => x);
        }

        // message loop for all RPC connections
        public async Task HandleConnection(IConnection connection)
        {
            try
            {
                await UpgradeAsync(connection);
                while (!connection.Closed)
                {
                    await HandleMessageAsync(connection);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in RPC message loop: {ex.Message}");
            }
            finally
            {
                await DowngradeAsync(connection);
            }
        }

        // register connection with requested RPC managers
        private async Task UpgradeAsync(IConnection connection)
        {
            foreach (var manager in _managers.Values)
            {
                if (await Authorizations.AuthorizeControllerAsync(_authorization, manager.SubType, connection.Context.User))
                {
                    manager.Upgrade(connection);
                }
            }
            var readyMessage = new JObject();
            var controllers =
                from manager in _managers.Values
                select manager.Name;
            readyMessage.Add("Controllers", JToken.FromObject(controllers));
            var routes =
                from manager in _managers.Values
                from action in manager.Actions
                select new { Controller = manager.Name, Action = action.Key };
            readyMessage.Add("Routes", JToken.FromObject(routes));
            await connection.Send(readyMessage);
        }

        // unregister connection with registered RPC managers
        private async Task DowngradeAsync(IConnection connection)
        {
            foreach (var manager in _managers.Values)
            {
                manager.Downgrade(connection);
            }
            await Task.FromResult(0);
        }

        // route a remote call to the appropriate action
        private async Task HandleMessageAsync(IConnection connection)
        {
            // get next message
            var message = await connection.Receive();

            // extract controller name and action name from message
            var controllerName = message.Value<string>("Controller");
            var actionName = message.Value<string>("Action");
            if (controllerName == null || actionName == null)
            {
                _logger.LogWarning("RPC call failed because of malformed message");
                return;
            }

            // get specified controller type
            IRPCManager manager;
            if (!_managers.TryGetValue(controllerName, out manager))
            {
                _logger.LogWarning($"RPC call [{controllerName}] failed because no such manager existed ");
                return;
            }

            // get specified action method info
            MethodInfo actionMethod;
            if (!manager.Actions.TryGetValue(actionName, out actionMethod))
            {
                _logger.LogWarning($"RPC call [{controllerName}.{actionName}] failed because no such action existed ");
                return;
            }

            // check authorization
            bool authorized = await Authorizations.AuthorizeControllerAsync(_authorization, manager.SubType, connection.Context.User);
            if (!authorized)
            {
                _logger.LogWarning($"RPC call [{controllerName}.{actionName}] failed because user wasn't authorized");
                return;
            }
            authorized = await Authorizations.AuthorizeActionAsync(_authorization, actionMethod, connection.Context.User);
            if (!authorized)
            {
                _logger.LogWarning($"RPC call [{controllerName}.{actionName}] failed because user wasn't authorized");
                return;
            }

            // extract the method parameters
            object[] parameters;
            if (!TryExtractParameters(message, actionMethod, out parameters))
            {
                _logger.LogWarning($"RPC call [{controllerName}.{actionName}] failed because invalid parameters were sent");
                return;
            }

            // create a new controller instance
            var descriptor = new ControllerActionDescriptor();
            descriptor.ControllerTypeInfo = manager.SubType.GetTypeInfo();
            descriptor.RouteValues["controller"] = controllerName;
            descriptor.RouteValues["action"] = actionName;
            var actionContext = new ActionContext(connection.Context, new RouteData(), descriptor);
            var controllerContext = new ControllerContext(actionContext);
            var controller = _factory.CreateController(controllerContext);
            if (controller == null)
            {
                _logger.LogWarning($"RPC call failed, because {controllerName} Controller could not be created");
                return;
            }

            // call the action
            try
            {
                actionMethod.Invoke(controller, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RPC Connection failed call to {controllerName}.{actionName}, because of exception: {ex.Message}");
                return;
            }
            finally
            {
                _factory.ReleaseController(controllerContext, controller);
            }
        }

        // convert passed JSON parameters to those needed by the given method
        private bool TryExtractParameters(
            JObject message,
            MethodInfo method,
            out object[] parameters)
        {
            var paramInfo = method.GetParameters();
            var paramTokens = message.GetValue("Parameters").ToArray();
            parameters = new object[paramTokens.Length];

            if (paramTokens.Length > paramInfo.Length)
            {
                return false;
            }

            for (int i = 0; i < paramInfo.Length; i++)
            {
                if (i < paramTokens.Length)
                {
                    try
                    {
                        var type = paramInfo[i].ParameterType;
                        parameters[i] = paramTokens[i].ToObject(type);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                else if (!paramInfo[i].IsOptional)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
