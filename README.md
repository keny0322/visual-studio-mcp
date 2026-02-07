# Visual Studio MCP Bridge

A **Model Context Protocol (MCP)** server that connects AI coding assistants (like Claude) to **Visual Studio 2022/2026** via COM automation.

This bridge enables AI agents to:
- 📖 Read build output, debug logs, and error lists
- 🔨 Build solutions and check for errors
- 🐛 Start/stop debugging sessions
- 📍 Inspect call stacks at breakpoints

Perfect for **autonomous AI-driven development workflows** where the agent can build, test, and debug without human intervention.

---

## 🚀 Quick Start

### Prerequisites
- **Windows 10/11**
- **.NET 8.0 SDK** or higher
- **Visual Studio 2022 or 2026** (must be running)
- **Claude Desktop** or another MCP-compatible client

### Installation

1. **Clone and build:**
   ```powershell
   git clone https://github.com/YOUR_USERNAME/vs-mcp-bridge.git
   cd vs-mcp-bridge
   dotnet publish -c Release -f net8.0 -r win-x64 --self-contained false
   ```

2. **Register with Claude:**
   ```powershell
   claude mcp add vs-bridge -- "<full-path>\bin\Release\net8.0\win-x64\publish\VisualStudioMcpServer.exe"
   ```

3. **Open Visual Studio** with any solution, then start using the tools!

---

## 🛠️ Available Tools

### Information Tools
| Tool | Description |
|------|-------------|
| `GetSolutionInfo` | Returns current solution path and list of projects |
| `ListOutputPanes` | Lists all Output Window panes (Build, Debug, etc.) |
| `ReadOutputPane` | Reads content from a specific pane (with truncation) |
| `GetErrorList` | Returns all errors and warnings from the Error List |

### Build & Debug Tools
| Tool | Description |
|------|-------------|
| `BuildSolution` | Builds the solution and returns success/error count |
| `StartDebugging` | Starts debugging (F5) |
| `StartWithoutDebugging` | Runs without debugger (Ctrl+F5) |
| `StopDebugging` | Stops the current debug session |
| `GetDebuggerState` | Returns current state (design/running/breakpoint) |
| `GetCallStack` | Returns call stack when stopped at a breakpoint |

---

## 🤖 Autonomous Testing Workflow

Claude can now run this autonomous development loop:

```
1. BuildSolution → Check for compile errors
2. If errors: GetErrorList → Analyze and fix code → Rebuild
3. If success: StartDebugging
4. GetDebuggerState → Monitor execution
5. If at breakpoint: GetCallStack → Analyze the issue
6. StopDebugging → Make fixes → Repeat
```

---

## ⚙️ Technical Details

### COM Message Filter
Visual Studio is a single-threaded COM server. When VS is busy (building, debugging), it rejects COM calls. This bridge implements `IOleMessageFilter` to automatically retry rejected calls.

### .NET Core Compatibility  
Since `Marshal.GetActiveObject` was removed in .NET Core, this project uses P/Invoke to `oleaut32.dll` and `ole32.dll` for COM object retrieval.

### STAThread Requirement
COM automation requires a Single-Threaded Apartment (STA). The main thread is marked with `[STAThread]` to ensure proper COM interop.

---

## 📋 Tool Parameters

### ReadOutputPane
```
paneName: string   - Name (or partial name) of the pane to read
maxLines: int      - Max lines to return from end (default: 50, 0 = all)
```

### BuildSolution
```
waitForBuild: bool - Wait for completion (default: true)
format: string     - Output format: "text" or "json"
```

### GetErrorList / GetSolutionInfo / GetDebuggerState / GetCallStack
```
format: string     - Output format: "text" or "json"
```

---

## 🔧 Troubleshooting

| Issue | Solution |
|-------|----------|
| "No VS instance found" | Ensure Visual Studio is running with a solution open |
| Tools not appearing | Restart Claude after re-registering the MCP |
| COM errors | Make sure VS isn't in a modal dialog (e.g., unsaved changes prompt) |

---

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

---

## 🤝 Contributing

Contributions welcome! Feel free to open issues or PRs.

### Ideas for future tools:
- `SetBreakpoint` - Add/remove breakpoints programmatically
- `EvaluateExpression` - Evaluate expressions in debugger context
- `OpenFile` - Open specific files in VS editor
- `NavigateToError` - Jump to error location
