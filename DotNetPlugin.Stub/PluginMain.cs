using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using DotNetPlugin.NativeBindings.SDK;
using RGiesecke.DllExport;
using System.Drawing;

namespace DotNetPlugin
{
    /// <summary>
    /// Contains entry points for plugin lifecycle and debugger event callbacks.
    /// </summary>
    internal static class PluginMain
    {
#if ALLOW_UNLOADING
        private static readonly Lazy<IPluginSession> NullSession = new Lazy<IPluginSession>(() => PluginSession.Null, LazyThreadSafetyMode.PublicationOnly);
        private static volatile Lazy<IPluginSession> s_session = NullSession;
        private static IPluginSession Session => s_session.Value;

        private static readonly string s_controlCommand = PluginBase.PluginName.Replace(' ', '_');

        internal static readonly string ImplAssemblyLocation;
#else
        private static PluginSession Session = PluginSession.Null;
#endif

        private static int s_pluginHandle;
        private static Plugins.PLUG_SETUPSTRUCT s_setupStruct;

        private static Assembly TryLoadAssemblyFrom(AssemblyName assemblyName, string location, bool tryLoadFromMemory = false)
        {
            var pluginBasePath = Path.GetDirectoryName(location);
            var dllPath = Path.Combine(pluginBasePath, assemblyName.Name + ".dll");

            if (!File.Exists(dllPath))
                return null;

            if (tryLoadFromMemory)
            {
                var assemblyBytes = File.ReadAllBytes(dllPath);
                // first we try to load the assembly from memory so that it doesn't get locked
                try { return Assembly.Load(assemblyBytes); }
                // mixed-mode assemblies can't be loaded from memory, so we resort to loading it from the disk
                catch { }
            }

            return Assembly.LoadFile(dllPath);
        }

        static PluginMain()
        {
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) => LogUnhandledException(e.ExceptionObject);

                // by default the runtime will look for referenced assemblies in the directory of the host application,
                // not in the plugin's dictionary, so we need to customize assembly resolving to fix this
                AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
                {
                    var assemblyName = new AssemblyName(e.Name);

                    if (assemblyName.Name == typeof(PluginMain).Assembly.GetName().Name)
                        return typeof(PluginMain).Assembly;

                    return TryLoadAssemblyFrom(assemblyName, typeof(PluginMain).Assembly.Location);
                };
            }
#if ALLOW_UNLOADING
            else
            {
                AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
                {
                    var assemblyName = new AssemblyName(e.Name);

                    if (assemblyName.Name == typeof(PluginMain).Assembly.GetName().Name)
                        return typeof(PluginMain).Assembly;

                    return
                        (ImplAssemblyLocation != null ? TryLoadAssemblyFrom(assemblyName, ImplAssemblyLocation, tryLoadFromMemory: true) : null) ??
                        TryLoadAssemblyFrom(assemblyName, typeof(PluginMain).Assembly.Location, tryLoadFromMemory: true);
                };
            }

            using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("build.meta"))
            {
                if (resourceStream == null)
                    return;

                ImplAssemblyLocation = new StreamReader(resourceStream).ReadLine();
            }
#endif
        }

        public static void LogUnhandledException(object exceptionObject)
        {
            var location = typeof(PluginMain).Assembly.Location;
            var logPath = Path.ChangeExtension(location, ".log");

            var errorMessage = exceptionObject?.ToString();
            if (errorMessage != null)
            {
                errorMessage += Environment.NewLine;
                File.AppendAllText(logPath, errorMessage);
                PluginBase.LogError(errorMessage);
            }
        }

#if ALLOW_UNLOADING
        private static void HandleImplChanged(object sender)
        {
            var session = s_session;
            if (ReferenceEquals(session.Value, sender) && UnloadPlugin(session))
                LoadPlugin(session);
        }
        
        private static bool LoadPlugin(Lazy<IPluginSession> reloadedSession = null)
        {
            if (!TryLoadPlugin(isInitial: false, reloadedSession))
            {
                PluginBase.LogError("Failed to load the implementation assembly.");
                return false;
            }

            Session.PluginHandle = s_pluginHandle;

            if (!Session.Init())
            {
                PluginBase.LogError("Failed to initialize the implementation assembly.");
                TryUnloadPlugin();
                return false;
            }

            Session.Setup(ref s_setupStruct);

            PluginBase.LogInfo("Successfully loaded the implementation assembly.");
            return true;
        }

        private static bool UnloadPlugin(Lazy<IPluginSession> reloadedSession = null)
        {
            if (!TryUnloadPlugin(reloadedSession))
            {
                PluginBase.LogError("Failed to unload the implementation assembly.");
                return false;
            }

            PluginBase.LogInfo("Successfully unloaded the implementation assembly.");
            return true;
        }

        private static bool TryLoadPlugin(bool isInitial, Lazy<IPluginSession> reloadedSession = null)
        {
            var expectedSession = reloadedSession ?? NullSession;
            var newSession = new Lazy<IPluginSession>(() => new PluginSessionProxy(HandleImplChanged), LazyThreadSafetyMode.ExecutionAndPublication);
            var originalSession = Interlocked.CompareExchange(ref s_session, newSession, expectedSession);
            if (originalSession == expectedSession)
            {
                _ = newSession.Value; // forces creation of session

                return true;
            }

            return false;
        }

        private static bool TryUnloadPlugin(Lazy<IPluginSession> reloadedSession = null)
        {
            Lazy<IPluginSession> originalSession;

            if (reloadedSession == null)
            {
                originalSession = Interlocked.Exchange(ref s_session, NullSession);
            }
            else
            {
                originalSession =
                    Interlocked.CompareExchange(ref s_session, reloadedSession, reloadedSession) == reloadedSession ?
                    reloadedSession :
                    NullSession;
            }

            if (originalSession != NullSession)
            {
                originalSession.Value.Dispose();
                return true;
            }

            return false;
        }

#else
        private static bool TryLoadPlugin(bool isInitial)
        {
            if (isInitial)
            {
                Session = new PluginSession();
                return true;
            }

            return false;
        }

        private static bool TryUnloadPlugin()
        {
            return false;
        }
#endif

        [RGiesecke.DllExport.DllExport("pluginit", CallingConvention.Cdecl)]
        public static bool pluginit(ref Plugins.PLUG_INITSTRUCT initStruct)
        {
            if (!TryLoadPlugin(isInitial: true))
                return false;

            initStruct.sdkVersion = Plugins.PLUG_SDKVERSION;
            initStruct.pluginVersion = PluginBase.PluginVersion;
            initStruct.pluginName = PluginBase.PluginName;
            Session.PluginHandle = s_pluginHandle = initStruct.pluginHandle;

#if ALLOW_UNLOADING
            if (!Plugins._plugin_registercommand(s_pluginHandle, s_controlCommand, ControlCommand, false))
            {
                PluginBase.LogError($"Failed to register the \"'{s_controlCommand}'\" command.");
                TryUnloadPlugin();
                return false;
            }
#endif

            if (!Session.Init())
            {
                PluginBase.LogError("Failed to initialize the implementation assembly.");
                TryUnloadPlugin();
                return false;
            }

            return true;
        }

        [RGiesecke.DllExport.DllExport("plugsetup", CallingConvention.Cdecl)]
        private static void plugsetup(ref Plugins.PLUG_SETUPSTRUCT setupStruct)
        {
            s_setupStruct = setupStruct;

            Session.Setup(ref setupStruct);
        }

        [RGiesecke.DllExport.DllExport("plugstop", CallingConvention.Cdecl)]
        private static bool plugstop()
        {
            var success = Session.Stop();

#if ALLOW_UNLOADING
            Plugins._plugin_unregistercommand(s_pluginHandle, s_controlCommand);
#endif

            s_setupStruct = default;
            s_pluginHandle = default;

            return success;
        }

#if ALLOW_UNLOADING
        private static bool ControlCommand(string[] args)
        {
            if (args.Length > 1)
            {
                if ("load".Equals(args[1], StringComparison.OrdinalIgnoreCase))
                {
                    return LoadPlugin();
                }
                else if ("unload".Equals(args[1], StringComparison.OrdinalIgnoreCase))
                {
                    return UnloadPlugin();
                }
            }

            PluginBase.LogError($"Invalid syntax. Usage: {s_controlCommand} [load|unload]");
            return false;
        }
#endif

        [RGiesecke.DllExport.DllExport("CBMENUENTRY", CallingConvention.Cdecl)]
        public static void CBMENUENTRY(Plugins.CBTYPE cbType, ref Plugins.PLUG_CB_MENUENTRY info)
        {
            Session.OnMenuEntry(ref info);
        }

        private static System.Drawing.Icon s_pluginIcon;

        [RGiesecke.DllExport.DllExport("plugingeticon", CallingConvention.Cdecl)]
        public static IntPtr plugingeticon()
        {
            if (s_pluginIcon == null)
            {
                // PNG bytes for the icon
                var iconBytes = new byte[]
                {
                    0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
                    0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x18, 0x00, 0x00, 0x00, 0x18,
                    0x08, 0x06, 0x00, 0x00, 0x00, 0xE0, 0x77, 0x3D, 0xF8, 0x00, 0x00, 0x00,
                    0x09, 0x70, 0x48, 0x59, 0x73, 0x00, 0x00, 0x0B, 0x13, 0x00, 0x00, 0x0B,
                    0x13, 0x01, 0x00, 0x9A, 0x9C, 0x18, 0x00, 0x00, 0x00, 0x01, 0x73, 0x52,
                    0x47, 0x42, 0x00, 0xAE, 0xCE, 0x1C, 0xE9, 0x00, 0x00, 0x00, 0x04, 0x67,
                    0x41, 0x44, 0x41, 0x00, 0x00, 0xB1, 0x8F, 0x0B, 0xFC, 0x61, 0x05, 0x00,
                    0x00, 0x01, 0xA4, 0x49, 0x44, 0x41, 0x54, 0x78, 0x01, 0xE5, 0x95, 0x4B,
                    0x6E, 0xC2, 0x30, 0x10, 0x86, 0x67, 0x6C, 0x90, 0x5A, 0xA9, 0xAA, 0xD4,
                    0x5D, 0x25, 0x02, 0x0A, 0x27, 0x80, 0xDE, 0x00, 0x4E, 0xD0, 0x72, 0x82,
                    0x2A, 0x8B, 0x76, 0x8B, 0x38, 0x01, 0xE5, 0x04, 0xB0, 0x6E, 0xA5, 0x04,
                    0x4E, 0xC0, 0x11, 0xC8, 0x11, 0x72, 0x83, 0xA4, 0x3C, 0xD6, 0x65, 0x51,
                    0x76, 0xB1, 0xA7, 0x36, 0x25, 0x2A, 0xE2, 0x91, 0xC4, 0xD0, 0x4D, 0xD5,
                    0x7F, 0x93, 0xD8, 0xF2, 0x7C, 0xBF, 0xC6, 0x63, 0x8F, 0x01, 0xFE, 0xBA,
                    0xD0, 0x64, 0xB1, 0xF5, 0x36, 0x6B, 0x00, 0xA7, 0xAE, 0xFA, 0x6D, 0xA8,
                    0xC0, 0x88, 0x10, 0x87, 0x73, 0xA7, 0xDC, 0x4B, 0x8B, 0x61, 0x90, 0x53,
                    0x15, 0x77, 0xFE, 0xA8, 0xE0, 0x13, 0x02, 0xB0, 0x81, 0xA8, 0x47, 0x04,
                    0x81, 0xFA, 0xBE, 0x94, 0xBC, 0x69, 0x3F, 0x2D, 0x2E, 0x57, 0x06, 0x1A,
                    0x2E, 0x41, 0x0E, 0x09, 0x28, 0x58, 0xA1, 0x6C, 0x2E, 0x9D, 0xEA, 0x52,
                    0xCF, 0x5B, 0xEE, 0xFB, 0x40, 0x21, 0xDA, 0x9F, 0x28, 0x6E, 0x92, 0x39,
                    0xE3, 0x0C, 0x76, 0xE1, 0xD7, 0x54, 0xBC, 0xB7, 0xDC, 0xE9, 0x47, 0xC9,
                    0x0B, 0xEB, 0x28, 0xD1, 0xD7, 0x6B, 0xAE, 0x62, 0xA8, 0x1F, 0x8B, 0x67,
                    0xA6, 0xF0, 0xCD, 0x38, 0x5A, 0x38, 0xD5, 0x80, 0x38, 0xD6, 0xD6, 0x0B,
                    0x39, 0x9F, 0x94, 0xDD, 0x69, 0x68, 0x79, 0xB3, 0xEE, 0x2E, 0x03, 0x4F,
                    0x80, 0xEF, 0x8C, 0x21, 0x42, 0xA2, 0x91, 0x42, 0xD5, 0x14, 0xED, 0x81,
                    0x10, 0x06, 0x0B, 0xA7, 0xD2, 0x49, 0x35, 0xC8, 0x0F, 0xCF, 0xAE, 0x09,
                    0x3B, 0x17, 0x9E, 0x55, 0x13, 0x76, 0x2E, 0xFC, 0x50, 0x4D, 0xE2, 0x18,
                    0xA2, 0xBD, 0x2D, 0xBA, 0xF5, 0x42, 0xBB, 0x40, 0x3C, 0x4C, 0x82, 0x2F,
                    0x05, 0x34, 0x38, 0xE3, 0x63, 0x63, 0x33, 0x84, 0xA1, 0xAA, 0x81, 0xB3,
                    0x97, 0xC1, 0x05, 0xC0, 0x92, 0x88, 0x3A, 0xC9, 0x9E, 0x16, 0x18, 0xEF,
                    0xEB, 0x02, 0x1A, 0x66, 0x12, 0xAC, 0x40, 0x74, 0x20, 0xEB, 0x14, 0x6D,
                    0x5A, 0xC2, 0x04, 0x84, 0x68, 0xC6, 0x05, 0x88, 0xB6, 0x33, 0xCB, 0x5B,
                    0xF0, 0x83, 0x35, 0x48, 0x44, 0x14, 0xAF, 0x17, 0x11, 0xE3, 0xF6, 0x76,
                    0x66, 0xA6, 0xF0, 0xA3, 0x19, 0x68, 0x95, 0xD4, 0xC5, 0xD1, 0xEE, 0x32,
                    0x66, 0xAD, 0xC5, 0xB3, 0x15, 0x54, 0xBC, 0x59, 0x5B, 0x12, 0x0D, 0x4C,
                    0xE0, 0xE9, 0x06, 0xAF, 0x61, 0x1D, 0x0A, 0x7C, 0x8C, 0xBA, 0xB9, 0xFD,
                    0xC8, 0x57, 0x67, 0xBC, 0x95, 0x17, 0x9E, 0x6A, 0xA0, 0xA5, 0x4F, 0x16,
                    0x97, 0xBA, 0x35, 0x33, 0x1B, 0xA4, 0xF4, 0xE7, 0x4F, 0x55, 0xFF, 0x58,
                    0xE3, 0x3B, 0xC9, 0xE0, 0x90, 0xE1, 0x76, 0xC1, 0xB3, 0xE0, 0x5A, 0xB9,
                    0xDF, 0x03, 0xAD, 0xA2, 0xE0, 0xDF, 0x37, 0x94, 0x60, 0x94, 0x07, 0x6E,
                    0x6C, 0xC0, 0xB9, 0xF0, 0x15, 0x3D, 0x40, 0xC4, 0xFE, 0xFA, 0x01, 0xFA,
                    0x6D, 0x83, 0x48, 0x5F, 0x40, 0xB5, 0x35, 0xDA, 0x44, 0x82, 0xB8, 0x83,
                    0x7F, 0xA1, 0x2F, 0x6C, 0x6C, 0xDB, 0x60, 0x8A, 0x38, 0x41, 0x6F, 0x00,
                    0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
                };

                using (var ms = new System.IO.MemoryStream(iconBytes))
                using (var bmp = new System.Drawing.Bitmap(ms))
                {
                    s_pluginIcon = System.Drawing.Icon.FromHandle(bmp.GetHicon());
                }
            }

            return s_pluginIcon?.Handle ?? IntPtr.Zero;
        }
    }
}
