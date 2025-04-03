# X64Dbg MCP Server
x64Dbg MCP Server
This project provides a foundation for building an MCP (Memory Command Protocol) server plugin for x64dbg using C# on the classic Windows-only .NET Framework platform.

The plugin acts as a lightweight HTTP interface into x64dbg, allowing you to send structured commands to inspect memory, disassemble, query registers, manipulate labels/comments, and more—all remotely and programmatically. It’s especially useful for driving x64dbg from external tools or automating analysis workflows.

On top of essential bindings to the x64dbg debugger engine, this template offers a clean project structure, a built-in command system, and a simple HTTP listener that exposes your commands through a text-based API. The design encourages rapid development and quick testing through real-time plugin reloading.

## Prerequisites
To build and run this project, you'll need:

Visual Studio Build Tools (2019 v16.7 or later)

.NET Framework 4.7.2 SDK

.NET Core 3.1 or .NET 6+ SDK (optional, for utilities)

While it's possible to build with just the CLI tools, using the full Visual Studio IDE (Community edition is fine) is highly recommended for the best experience. Other editors supporting C# 9+ may work but haven’t been tested.

## Getting Started
Clone or fork the project: git clone <your-repo-url>

Open the solution and customize the plugin name and output by editing Directory.Build.props

The main logic resides in the DotNetPlugin.Impl project. Start by editing Plugin.cs — this is the entry point where plugin registration and MCP startup occurs.

Features
Built-in HTTP Server (MCP)
The server listens for incoming HTTP requests and maps them to registered command methods using reflection. For example:

bash
Copy
Edit
POST /message?sessionId=abc123
{
  "jsonrpc": "2.0",
  "method": "GetCallStack",
  "params": [],
  "id": "1"
}
Example Extension Points
Plugin.Commands.cs
Define new MCP-accessible commands with the [Command] attribute. Methods can take structured arguments (like strings, integers, or arrays) and return results directly to the caller.

Plugin.EventCallbacks.cs
Register debugger event handlers with [EventCallback]. Useful for integrating your MCP logic with x64dbg's runtime (e.g. reacting to breakpoints or execution state).

Plugin.ExpressionFunction.cs
Define custom expression functions usable inside x64dbg using [ExpressionFunction]. These can return nuint values and support simple automation logic.

Plugin.Menus.cs
Add context menu items to the x64dbg UI through a fluent-style API.

What It's Good For
Building remote automation tools that interact with live debugging sessions

Inspecting memory, threads, disassembly, and registers without manually operating the GUI

Scripted fuzzing, analysis, and patching workflows

Extending x64dbg with your own protocols or APIs

How It Works
The MCP server runs a simple HTTP listener and routes incoming commands to C# methods marked with the [Command] attribute. These methods can perform any logic (e.g., memory reads, disassembly, setting breakpoints) and return data in a structured format. Think of it as a bridge between web-friendly tools and native debugging environments.
