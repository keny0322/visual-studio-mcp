using ModelContextProtocol.Server;
using Newtonsoft.Json;
using System.ComponentModel;

namespace VisualStudioMcpServer.Tools;

/// <summary>
/// MCP Tool: Builds the current Visual Studio solution.
/// </summary>
[McpServerToolType]
public static class BuildSolutionTool
{
    [McpServerTool]
    [Description("Builds the current Visual Studio solution. Returns build result with error count.")]
    public static string BuildSolution(
        [Description("If true (default), waits for build to complete before returning. If false, starts build and returns immediately.")]
        bool waitForBuild = true,

        [Description("Output format: 'text' for human-readable or 'json' for structured data. Defaults to 'text'.")]
        string format = "text")
    {
        var result = VisualStudioConnector.BuildSolution(waitForBuild);

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }

        return result.Message;
    }
}
