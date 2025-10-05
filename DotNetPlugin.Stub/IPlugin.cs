using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetPlugin.SDK
{
    public interface IPlugin
    {
        void OnLoad();
        void OnUnload();

        void SetupMenu(IMenuBuilder menu);
        void OnMenu(int id);
        byte[] GetIcon();
    }
}
