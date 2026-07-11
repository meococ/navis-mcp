using System;

namespace NavisMcp.Plugin.Navis2026.Bridge
{
    internal sealed class BridgeRpcException : Exception
    {
        public BridgeRpcException(string code, string message, object details = null)
            : base(message)
        {
            Code = code;
            Details = details;
        }

        public string Code { get; }
        public object Details { get; }
    }
}
