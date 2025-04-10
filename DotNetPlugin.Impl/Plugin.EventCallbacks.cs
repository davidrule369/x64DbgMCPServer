using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DotNetPlugin.NativeBindings;
using DotNetPlugin.NativeBindings.SDK;

namespace DotNetPlugin
{
    partial class Plugin
    {
        [EventCallback(Plugins.CBTYPE.CB_INITDEBUG)]
        public static void OnInitDebug(ref Plugins.PLUG_CB_INITDEBUG info)
        {
            var szFileName = info.szFileName;
            LogInfo($"DotNet test debugging of file {szFileName} started!");
        }

        [EventCallback(Plugins.CBTYPE.CB_STOPDEBUG)]
        public static void OnStopDebug(ref Plugins.PLUG_CB_STOPDEBUG info)
        {
            LogInfo($"DotNet test debugging stopped!");
        }

        [EventCallback(Plugins.CBTYPE.CB_CREATEPROCESS)]
        public static void OnCreateProcess(IntPtr infoPtr)
        {
            // info can also be cast manually
            var info = infoPtr.ToStructUnsafe<Plugins.PLUG_CB_CREATEPROCESS>();

            var CreateProcessInfo = info.CreateProcessInfo;
            var modInfo = info.modInfo;
            string DebugFileName = info.DebugFileName;
            var fdProcessInfo = info.fdProcessInfo;
            LogInfo($"Create process {info.DebugFileName}");
        }

        [EventCallback(Plugins.CBTYPE.CB_LOADDLL)]
        public static void OnLoadDll(ref Plugins.PLUG_CB_LOADDLL info)
        {
            var LoadDll = info.LoadDll;
            var modInfo = info.modInfo;
            string modname = info.modname;
            LogInfo($"Load DLL {modname}");
        }

        [EventCallback(Plugins.CBTYPE.CB_DEBUGEVENT)]
        public static void DebugEvent(ref Plugins.PLUG_CB_DEBUGEVENT info)
        {
            LogInfo($"DebugEvent callback received.");

            // *** Replace 'PointerToTheStringField' with the actual field name ***
            IntPtr stringPointer = info.DebugEvent.Value.u.DebugString.lpDebugStringData;
            string? retrievedString = null;

            if (stringPointer != IntPtr.Zero)
            {
                try
                {
                    // Try reading as an ANSI string (common for C/C++ interop)
                    retrievedString = Marshal.PtrToStringAnsi(stringPointer);

                    // --- OR ---

                    // If you know it's a Unicode (UTF-16) string (common in Windows API)
                    // retrievedString = Marshal.PtrToStringUni(stringPointer);

                    // --- OR ---

                    // If you know it's a UTF-8 string (less common for basic C strings in Win)
                    // retrievedString = Marshal.PtrToStringUTF8(stringPointer);

                    if (!string.IsNullOrEmpty(retrievedString))
                    {
                        LogInfo($"Successfully retrieved string from IntPtr: '{retrievedString}'");
                        // Now you can use the 'retrievedString' variable
                    }
                    else
                    {
                        LogInfo($"Pointer {stringPointer} did not point to a valid readable string (or it was empty).");
                    }
                }
                catch (AccessViolationException accEx)
                {
                    LogInfo($"Error: Access Violation trying to read string from pointer {stringPointer}. Check if pointer is valid. {accEx.Message}");
                }
                catch (Exception ex)
                {
                    LogInfo($"Error marshalling string from pointer {stringPointer}: {ex.Message}");
                }
            }
            else
            {
                LogInfo("The relevant string pointer in PLUG_CB_DEBUGEVENT is null (IntPtr.Zero).");
            }

            // You can add more processing for other parts of the 'info' struct here
        }

        [EventCallback(Plugins.CBTYPE.CB_OUTPUTDEBUGSTRING)]
        public static void OutputDebugString(ref Plugins.PLUG_CB_OUTPUTDEBUGSTRING info)
        {
            LogInfo($"Load DLL ");
        }
    }
}
