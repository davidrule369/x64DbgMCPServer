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
            // Set main plugin menu icon from embedded stub resource (DotNetPlugin.Resources.mcp.ico)
            try
            {
                var asm = typeof(PluginMain).Assembly; // Stub assembly contains the icon resource
                using (var stream = asm.GetManifestResourceStream("DotNetPlugin.Resources.mcp.ico"))
                {
                    if (stream != null)
                    {
                        using (var ico = new System.Drawing.Icon(stream))
                        {
                            menus.Main.SetIcon(ico);
                        }
                    }
                }
            }
            catch { /* best-effort: skip if resource not found */ }

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
