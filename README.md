# X64Dbg MCP Server
x64Dbg MCP Server
This project is a starting point for building an MCP (Memory Command Protocol) server plugin for x64dbg using C# on the classic Windows-only .NET Framework platform (No ASP.NET Core hosting required).

The plugin acts as a lightweight HTTP interface into x64dbg, allowing you to send structured commands to inspect memory, disassemble, query registers, manipulate labels/comments, and more—all remotely and programmatically. It’s especially useful for driving x64dbg from external tools or automating analysis workflows using an LLM MCP client.

On top of essential bindings to the x64dbg debugger engine, this template offers a clean project structure, a built-in command system, and a simple HTTP listener that exposes your commands through a text-based API. The design encourages rapid development and quick testing through real-time plugin reloading.

## Prerequisites
To build and run this project, you'll need:

Visual Studio Build Tools (2019 v16.7 or later)

.NET Framework 4.7.2 SDK

## Getting Started
Clone or fork the project: git clone <your-repo-url>

Open the solution and customize the plugin name and output by editing Directory.Build.props

The main logic resides in the DotNetPlugin.Impl project. Start by editing Plugin.cs — this is the entry point where plugin registration and MCP startup occurs.

## Features
✅ Self-hosted HTTP command interface (no ASP.NET Core required)

✅ Lightweight, zero-dependency binary deployment

✅ Modular commands with parameter mapping

✅ Direct interaction with registers, memory, threads, disassembly

✅ Bi-directional AI/LLM command support

✅ Plugin reload without restarting x64dbg

✅ Expression function and menu extension support

## Sample Commands
Once the MCP server is running (via the plugin menu in x64dbg), you can issue commands like:

ExecuteDebuggerCommand command=init C:\PathTo\Binary.exe
GetAllRegisters
ReadMemAtAddress addressStr=0x000000014000153f, byteCount=5
WriteMemToAddress addressStr=0x000000014000153f, byteString=90 90 90 90 90
CommentOrLabelAtAddress addressStr=0x000000014000153f, value=Test, mode=Comment
CommentOrLabelAtAddress addressStr=0x000000014000153f, value=        # removes comment
GetLabel addressStr=0x000000014000153f
GetAllActiveThreads
GetCallStack
GetAllModulesFromMemMap
These commands return JSON or text-formatted output that’s suitable for ingestion by AI models or integration scripts. Example:

## Actively working on implementing several functions
[GetAllActiveThreads] Found 4 active threads:
TID: 121428560 | EntryPoint: 0x0 | TEB: 0x0
TID:        0 | EntryPoint: 0x0 | TEB: 0x0
TID:        0 | EntryPoint: 0x0 | TEB: 0x0
TID:        0 | EntryPoint: 0x0 | TEB: 0x0
I’ve validated several commands already and they are working wonders. I’m especially excited to be using this system to explore how AI-assisted reverse engineering could streamline security workflows.

## How It Works
The MCP server runs a simple HTTP listener and routes incoming commands to C# methods marked with the [Command] attribute. These methods can perform any logic (e.g., memory reads, disassembly, setting breakpoints) and return data in a structured format. Think of it as a bridge between web-friendly tools and native debugging environments.

## Special thanks
⚡ With the help of DotNetPluginCS by Adams85. That and roughly ~20 hours of focused coding, MCP Protocol review resulted in a decent proof-of-concept self-contained HTTP MCP server plugin for x64dbg.

## Integration Notes
One of the most satisfying aspects of this project was overcoming the challenge of building an HTTP server entirely self-contained — no Kestrel, no ASP.NET, just raw HttpListener powering your reverse engineering automation.

I plan to continue improving this codebase as part of my journey into AI-assisted analysis, implementation security, and automation tooling.

If you'd like help creating your own integration, extending this plugin, or discussing potential use cases — feel free to reach out (see contact info in the repo or my profile). I’m eager to collaborate and learn with others exploring this space.

💻 Let’s reverse engineer smarter. Not harder.

Cheers 🎉
