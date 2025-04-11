# X64Dbg MCP Server (plugin)
This project is a starting point for building an MCP (Memory Command Protocol) server plugin for x96/x64/x32dbg https://github.com/x64dbg/x64dbg/ using C# on the classic Windows-only .NET Framework platform (No ASP.NET Core hosting required).

The plugin acts as a lightweight HTTP interface bridge between an MCP client and the debugger, allowing you to have an LLM MCP client interactively send commands to inspect memory, disassemble, query registers, manipulate labels/comments, and more—all remotely and programmatically.

On top of essential bindings to the x64dbg debugger engine, this template offers a clean project structure, a built-in command system, and a simple HTTP listener that exposes your commands through a text-based API. 

## Features
✅ Self-hosted HTTP command interface (no ASP.NET Core required)
✅ Lightweight, zero-dependency binary deployment
✅ Modular commands with parameter mapping
✅ Direct interaction with registers, memory, threads, disassembly
✅ Bi-directional AI/LLM command support
✅ Plugin reload without restarting x64dbg
✅ Expression function and menu extension support

## Sample Conversations:
### AI Tasked with loading a file, counting the internal modules and begin labeling important material functions.
https://github.com/AgentSmithers/x64DbgMCPServer/blob/master/Sample1

## Prerequisites
To build and run this project, you'll need:
Visual Studio Build Tools (2019 v16.7 or later)
.NET Framework 4.7.2 SDK

## Getting Started
Clone or fork the project: git clone https://github.com/AgentSmithers/x64DbgMCPServer

Open the solution and build.

copy the files (DotNetPluginCS\bin\x64\Debug) into the x64DBG plugin (x96\release\x64\plugins) folder to run
![image](https://github.com/user-attachments/assets/d307d3e0-4215-4fc4-a702-a9fd814703ac)
![image](https://github.com/user-attachments/assets/02eb35d8-8584-46de-83c6-b535d23976b9)

Start the Debugger, goto plugins -> Click "Start MCP Server"

Connect to it with your prefered MCP Client on port 3001 via SSE.

## Sample Commands
I’ve validated several commands already and they are working wonders. I’m especially excited to be using this system to explore how AI-assisted reverse engineering could streamline security workflows.
Once the MCP server is running (via the plugin menu in x64dbg), you can issue commands like:
```
ExecuteDebuggerCommand command=init C:\InjectGetTickCount\InjectSpeed.exe
ExecuteDebuggerCommand command="AddFavouriteCommand Log s, NameOfCmd"
ReadDismAtAddress addressStr=0x000000014000153f, byteCount=5
ReadMemAtAddress addressStr=00007FFA1AC81000, byteCount=5
WriteMemToAddress addressStr=0x000000014000153f, byteString=90 90 90 90 90 90
CommentOrLabelAtAddress addressStr=0x000000014000153f, value=Test, mode=Comment
CommentOrLabelAtAddress addressStr=0x000000014000153f, value=
GetAllRegisters
GetLabel addressStr=0x000000014000153f
GetAllActiveThreads
GetAllModulesFromMemMap
GetCallStack
These commands return JSON or text-formatted output that’s suitable for ingestion by AI models or integration scripts. Example:
```
![image](https://github.com/user-attachments/assets/f954feab-4518-4368-8b0a-d6ec07212122)
![image](https://github.com/user-attachments/assets/2952e4eb-76ef-460c-9124-0e3c1167fa3d)

## Debugging
DotNetPlugin.Impl contains the following within the project build post commands. Update it to reflect the corret path to x64dbg for faster debugging:
xcopy /Y /I "$(TargetDir)*.*" "C:\Users\User\Desktop\x96\release\x64\plugins\"
C:\Users\User\Desktop\x96\release\x64\x64dbg.exe

## Actively working on implementing several functions
Not every command is fully implemented althrough I am actively working on getting this project moving to support full stack, thread and module dumps for the AI to query.

## How It Works
The MCP server runs a simple HTTP listener and routes incoming commands to C# methods marked with the [Command] attribute. These methods can perform any logic (e.g., memory reads, disassembly, setting breakpoints) and return data in a structured format back to a MCP client.

## Known Issues
ExecuteDebuggerCommand always returns true as it pertains to the comment successfully being execute and not the results of the actual command.

## Special thanks
⚡ With the help of DotNetPluginCS by Adams85. That and roughly ~20 hours of focused coding, MCP Protocol review resulted in a decent proof-of-concept self-contained HTTP MCP server plugin for x64dbg.

## Integration Notes
One of the most satisfying aspects of this project was overcoming the challenge of building an HTTP server entirely self-contained — no Kestrel, no ASP.NET, just raw HttpListener powering your reverse engineering automation.

I plan to continue improving this codebase as part of my journey into AI-assisted analysis, implementation security, and automation tooling.

If you'd like help creating your own integration, extending this plugin, or discussing potential use cases — feel free to reach out (see contact info in the repo or my profile). I’m eager to collaborate and learn with others exploring this space.

💻 Let’s reverse engineer smarter. Not harder.

Cheers 🎉

Https://ControllingTheInter.net
