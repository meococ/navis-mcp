using System;

namespace NavisMcp.Plugin.Navis2026.Bridge
{
    internal static class BridgeRuntime
    {
        private static readonly object Gate = new object();
        private static UiWorkDispatcher _dispatcher;
        private static BridgeHost _host;

        public static bool IsRunning
        {
            get
            {
                lock (Gate)
                {
                    return _host != null && _host.IsRunning;
                }
            }
        }

        public static string Status
        {
            get
            {
                lock (Gate)
                {
                    if (_host == null)
                    {
                        return "Navis MCP bridge is stopped.";
                    }

                    return _host.Status;
                }
            }
        }

        public static void Start()
        {
            lock (Gate)
            {
                if (_host != null && _host.IsRunning)
                {
                    return;
                }

                _dispatcher = new UiWorkDispatcher();
                _host = new BridgeHost(_dispatcher);
                _host.Start();
            }
        }

        public static void Restart()
        {
            lock (Gate)
            {
                StopNoLock();
                _dispatcher = new UiWorkDispatcher();
                _host = new BridgeHost(_dispatcher);
                _host.Start();
            }
        }

        public static void Stop()
        {
            lock (Gate)
            {
                StopNoLock();
            }
        }

        private static void StopNoLock()
        {
            try
            {
                _host?.Dispose();
            }
            catch
            {
            }

            try
            {
                _dispatcher?.Dispose();
            }
            catch
            {
            }

            _host = null;
            _dispatcher = null;
        }
    }
}
