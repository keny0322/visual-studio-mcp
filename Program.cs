using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using VisualStudioMcpServer;
using VisualStudioMcpServer.Tools;

/// <summary>
/// Visual Studio MCP Bridge - Connects Claude Code to Visual Studio 2026 via COM automation.
/// 
/// Usage:
///   claude mcp add vs-bridge -- "path/to/VisualStudioMcpServer.exe"
/// 
/// Available Tools:
///   - list_output_panes: Lists all Output Window panes
///   - read_output_pane: Reads content from a specific pane (with truncation)
///   - get_error_list: Gets all errors/warnings from Error List
///   - get_solution_info: Gets current solution path and projects
/// </summary>
class Program
{
    // We need STA thread for COM automation, but MCP SDK uses async.
    // Solution: Run COM operations from main STA thread, use synchronous wrappers.
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            // Register COM message filter for handling VS busy states
            MessageFilter.Register();

            // Run the async server on the STA thread
            RunServerAsync(args).GetAwaiter().GetResult();

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
        finally
        {
            // Always revoke the message filter on exit
            MessageFilter.Revoke();
        }
    }

    static async Task RunServerAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly(typeof(ListOutputPanesTool).Assembly);

        var app = builder.Build();

        await app.RunAsync();
    }
}
