# Contributing to Visual Studio MCP Server

First off, thank you for considering contributing to this project! It's people like you that make the open-source community such an amazing place to learn, inspire, and create.

This project aims to bridge the gap between AI agents (like Claude) and Visual Studio 2022/2026, enabling autonomous development workflows.

## 🛠️ How Can I Contribute?

### 1. New Tools & Features
We are actively looking for contributions to expand the MCP toolset. Here are specific tools we need implemented (as mentioned in our roadmap):

* **`SetBreakpoint`**: Implement logic to add/remove breakpoints programmatically via the COM interface.
* **`EvaluateExpression`**: Create a tool to evaluate expressions in the current debugger context.
* **`OpenFile`**: Allow the agent to open specific files in the VS editor.
* **`NavigateToError`**: Implement functionality to jump the cursor to specific error locations.

### 2. Reporting Bugs
* Ensure the bug was not already reported by searching on GitHub under [Issues](https://github.com/alon-mini/vs-mcp/issues).
* If you're unable to find an open issue addressing the problem, open a new one. Be sure to include a **title and clear description**, as well as the version of Visual Studio you are using (2022 or 2026).

## 💻 Development Setup

1. **Prerequisites:**
    * Windows 10/11
    * .NET 8.0 SDK or higher
    * Visual Studio 2022 or 2026 (Running instance required for testing).

2. **Build the Project:**
    ```bash
    git clone https://github.com/alon-mini/vs-mcp.git
    cd vs-mcp
    dotnet publish -c Release -f net8.0 -r win-x64 --self-contained false
    ```

3. **Testing Local Changes:**
    You can test your changes by registering your local build with Claude Desktop:
    ```bash
    claude mcp add vs-bridge-dev -- "<full-path>\bin\Release\net8.0\win-x64\publish\VisualStudioMcpServer.exe"
    ```

## 📐 Style Guidelines
* **Language:** C# (.NET 8).
* **COM Interop:** Ensure all COM interactions are handled on the main thread or properly marshaled, as Visual Studio is a single-threaded COM server.
* **Error Handling:** All MCP tools should return clear error messages if the Visual Studio instance is busy (e.g., during a build or modal dialog).

## 🔄 Pull Request Process
* Fork the repo and create your branch from `main`.
* If you've added code that should be tested, add tests.
* Ensure your code builds without warnings.
* Issue that Pull Request!

## 📜 Code of Conduct
This project adopts the [Contributor Covenant Code of Conduct](https://www.contributor-covenant.org/). By participating, you are expected to uphold this code.

