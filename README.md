# X64Dbg MCP Server (plugin)
This project is a starting point for building an MCP (Memory Command Protocol) server plugin for x96/x64/x32dbg https://github.com/x64dbg/x64dbg/ using C# on the classic Windows-only .NET Framework platform (No ASP.NET Core hosting required).

The plugin acts as a lightweight HTTP interface bridge between an MCP client and the debugger, allowing you to have an LLM MCP client interactively send commands to inspect memory, disassemble, query registers, manipulate labels/comments, and more—all remotely and programmatically.

On top of essential bindings to the x64dbg debugger engine, this template offers a clean project structure, a built-in command system, and a simple HTTP listener that exposes your commands through a text-based API. 

# X64Dbg MCP Client - Need a client to sample?
[mcp-csharp-sdk-client.zip](https://github.com/user-attachments/files/19697365/mcp-csharp-sdk-client.zip)

Open the project
Edit line 590 in Program.cs and enter your GeminiAI key from Google Cloud API.
Edit line 615 in Program.cs and enter in your MCP Server IP: Location = "http://192.168.x.x:50300/sse",
Open your x96 debugger, your logs should reflect that the server automatically loaded.
To interact with the server by hand instead of using the AI, uncomment line 634 and comment out line 635.
Hit start debug on the client and the AI should automatically execute the Prompt located on line 434 (Program.cs)

![image](https://github.com/user-attachments/assets/ebf2ad81-0672-4ceb-be6e-a44c625cd6d0)

Access the latest sample client to use as a starting point of integration with this project: https://github.com/AgentSmithers/mcp-csharp-sdk-client/

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

### Singleshot Speedhack identification
https://github.com/AgentSmithers/x64DbgMCPServer/blob/master/Sample2

## Prerequisites
To build and run this project, you'll need:
Visual Studio Build Tools (2019 v16.7 or later)
.NET Framework 4.7.2 SDK

## Getting Started
Clone or fork the project: git clone https://github.com/AgentSmithers/x64DbgMCPServer

Open the solution and build.

copy the files (x64DbgMCPServer\bin\x64\Debug) into the x64DBG plugin (x96\release\x64\plugins\x64DbgMCPServer) folder to run
![image](https://github.com/user-attachments/assets/8511452e-b65c-4bc8-83ff-885c384d0bbe)

Sample Debug log when loaded

![image](https://github.com/user-attachments/assets/02eb35d8-8584-46de-83c6-b535d23976b9)

Start the Debugger, goto plugins -> Click "Start MCP Server"

Connect to it with your prefered MCP Client on port 50300 via SSE.

## Applying Fixes for CI/Builds
This repository includes helper files to apply necessary modifications for building on a clean environment (like GitHub Actions) or reapplying changes after pulling updates from the original developer.

- **`apply_file_changes.py`**: A Python script that automatically patches the project files. Run `python apply_file_changes.py` to apply all fixes idempotently.
- **`File_Change_AI_Prompt.txt`**: A detailed guide for an AI assistant (or a human developer) outlining the same set of changes, explaining what to modify, where, and why.

These helpers configure the project for both x86 and x64 builds, set up CI workflows, and resolve compilation issues without needing a full Visual Studio installation.

## Sample Commands
I’ve validated several commands already and they are working wonders. I’m especially excited to be using this system to explore how AI-assisted reverse engineering could streamline security workflows.
Once the MCP server is running (via the plugin menu in x64dbg), you can issue commands like:
```