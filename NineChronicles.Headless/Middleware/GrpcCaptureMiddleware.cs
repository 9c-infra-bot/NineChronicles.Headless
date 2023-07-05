using System.Collections.Generic;
using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Threading.Tasks;
using Libplanet;
using Libplanet.Action;
using Libplanet.Tx;
using Serilog;
using static NineChronicles.Headless.NCActionUtils;

namespace NineChronicles.Headless.Middleware
{
    public class GrpcCaptureMiddleware : Interceptor
    {
        private readonly ILogger _logger;
        private StandaloneContext _standaloneContext;
        private Dictionary<string, HashSet<Address>> _ipSignerList;

        public GrpcCaptureMiddleware(StandaloneContext standaloneContext, Dictionary<string, HashSet<Address>> ipSignerList)
        {
            _logger = Log.Logger.ForContext<GrpcCaptureMiddleware>();
            _standaloneContext = standaloneContext;
            _ipSignerList = ipSignerList;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            if (context.Method is "/IBlockChainService/AddClient" or "/IBlockChainService/GetNextTxNonce" && request is byte[] addressBytes)
            {
                var agent = new Address(addressBytes);
                var httpContext = context.GetHttpContext();
                var ipAddress = httpContext.Connection.RemoteIpAddress + ":" + httpContext.Connection.RemotePort;
                _logger.Information(
                    "[GRPC-REQUEST-CAPTURE] IP: {IP} Method: {Method} Agent: {Agent}",
                    ipAddress, context.Method, agent);
                if (!_ipSignerList.ContainsKey(httpContext.Connection.RemoteIpAddress!.ToString()))
                {
                    _logger.Information(
                        "[GRPC-REQUEST-CAPTURE] Creating a new list for IP: {IP}",
                        httpContext.Connection.RemoteIpAddress!.ToString());
                    _ipSignerList[httpContext.Connection.RemoteIpAddress!.ToString()] = new HashSet<Address>();
                }
                else
                {
                    _logger.Information(
                        "[GRPC-REQUEST-CAPTURE] List already created for IP: {IP} Count: {Count}",
                        httpContext.Connection.RemoteIpAddress!.ToString(),
                    _ipSignerList[httpContext.Connection.RemoteIpAddress!.ToString()].Count);
                }

                _ipSignerList[httpContext.Connection.RemoteIpAddress!.ToString()].Add(agent);
                if (_ipSignerList[httpContext.Connection.RemoteIpAddress!.ToString()].Count > 100)
                {
                    _logger.Information(
                        "[GRPC-REQUEST-CAPTURE] IP: {IP} List Count: {Count}, AgentAddresses: {Agent}",
                        httpContext.Connection.RemoteIpAddress!.ToString(),
                        _ipSignerList[httpContext.Connection.RemoteIpAddress!.ToString()].Count,
                        _ipSignerList[httpContext.Connection.RemoteIpAddress!.ToString()]);
                }
            }

            if (context.Method is "/IBlockChainService/PutTransaction" && request is byte[] txBytes)
            {
                Transaction tx =
                    Transaction.Deserialize(txBytes);
                var actionName = ToAction(tx.Actions[0]) is { } action
                    ? $"{action}"
                    : "NoAction";
                var httpContext = context.GetHttpContext();
                var ipAddress = httpContext.Connection.RemoteIpAddress + ":" + httpContext.Connection.RemotePort;
                _logger.Information(
                    "[GRPC-REQUEST-CAPTURE] IP: {IP} Method: {Method} Agent: {Agent} Action: {Action}",
                    ipAddress, context.Method, tx.Signer, actionName);
            }

            return await base.UnaryServerHandler(request, context, continuation);
        }
    }
}
