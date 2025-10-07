using System;
using System.IO;
using System.Windows.Forms;
using DotNetPlugin.NativeBindings.SDK;
using DotNetPlugin.Properties;

namespace DotNetPlugin
{
    partial class Plugin
    {
        protected override void SetupMenu(Menus menus)
        {
            // Set main plugin menu icon (best-effort): prefer embedded mcp.ico; fallback to AboutIcon resource
            try
            {
                var asm = typeof(Plugin).Assembly; // current assembly
                Stream stream = null;
                // Try fully-qualified name first
                stream = asm.GetManifestResourceStream("DotNetPlugin.Resources.mcp.ico");
                // Fallback: search any resource ending with mcp.ico
                if (stream == null)
                {
                    foreach (var name in asm.GetManifestResourceNames())
                    {
                        if (name.EndsWith(".mcp.ico", StringComparison.OrdinalIgnoreCase))
                        {
                            stream = asm.GetManifestResourceStream(name);
                            break;
                        }
                    }
                }
                if (stream != null)
                {
                    using (stream)
                    using (var ico = new System.Drawing.Icon(stream))
                        menus.Main.SetIcon(ico);
                }
                else
                {
                    // Fallback to existing resource icon so the menu still shows an icon
                    menus.Main.SetIcon(Resources.AboutIcon);
                }
            }
            catch
            {
                // Last resort: keep default without icon
            }

            menus.Main
                .AddAndConfigureItem("&Start MCP Server", StartMCPServer).SetIcon(Resources.AboutIcon).Parent
                .AddAndConfigureItem("&Stop MCP Server", StopMCPServer).SetIcon(Resources.AboutIcon).Parent
                .AddAndConfigureItem("&About...", OnAboutMenuItem).SetIcon(Resources.AboutIcon);
            //.AddAndConfigureItem("&CustomCommand", ExecuteCustomCommand).SetIcon(Resources.AboutIcon).Parent
            //.AddAndConfigureItem("&DotNetDumpProcess", OnDumpMenuItem).SetHotKey("CTRL+F12").Parent
            //.AddAndConfigureSubMenu("sub menu")
            //    .AddItem("sub menu entry1", menuItem => Console.WriteLine($"hEntry={menuItem.Id}"))
            //    .AddSeparator()
            //    .AddItem("sub menu entry2", menuItem => Console.WriteLine($"hEntry={menuItem.Id}"));
        }

        public void OnAboutMenuItem(MenuItem menuItem)
        {
            MessageBox.Show(HostWindow, "x64DbgMCPServer Plugin For x64dbg\nCoded By AgentSmithers", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void OnDumpMenuItem(MenuItem menuItem)
        {
            if (!Bridge.DbgIsDebugging())
            {
                Console.WriteLine("You need to be debugging to use this Command");
                return;
            }
            Bridge.DbgCmdExec("DotNetDumpProcess");
        }

        public static void ExecuteCustomCommand(MenuItem menuItem)
        {
            if (!Bridge.DbgIsDebugging())
            {
                Console.WriteLine("You need to be debugging to use this Command");
                return;
            }
            Bridge.DbgCmdExec("DumpModuleToFile");
        }
        public static void StartMCPServer(MenuItem menuItem)
        {
            Bridge.DbgCmdExec("StartMCPServer");
        }
        public static void StopMCPServer(MenuItem menuItem)
        {
            Bridge.DbgCmdExec("StopMCPServer");
        }
    }
}
