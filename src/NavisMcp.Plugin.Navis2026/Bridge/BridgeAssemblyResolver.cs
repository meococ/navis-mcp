using System;
using System.IO;
using System.Reflection;

namespace NavisMcp.Plugin.Navis2026.Bridge
{
    internal static class BridgeAssemblyResolver
    {
        private static readonly object Gate = new object();
        private static bool _installed;

        public static void Install()
        {
            lock (Gate)
            {
                if (_installed)
                {
                    return;
                }

                AppDomain.CurrentDomain.AssemblyResolve += ResolveFromPluginFolder;
                _installed = true;
            }
        }

        private static Assembly ResolveFromPluginFolder(object sender, ResolveEventArgs args)
        {
            try
            {
                var requestedName = new AssemblyName(args.Name).Name + ".dll";
                var pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(pluginFolder))
                {
                    return null;
                }

                var candidate = Path.Combine(pluginFolder, requestedName);
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
