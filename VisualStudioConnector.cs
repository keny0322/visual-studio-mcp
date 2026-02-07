using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;

namespace VisualStudioMcpServer;

/// <summary>
/// Handles connection to Visual Studio via COM automation.
/// Supports VS2026 (DTE 18.0) and falls back to generic DTE.
/// </summary>
public static class VisualStudioConnector
{
    private static DTE2? _cachedDte;

    // P/Invoke declarations for GetActiveObject (removed in .NET Core)
    [DllImport("oleaut32.dll", PreserveSig = false)]
    private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

    [DllImport("ole32.dll")]
    private static extern int CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid pclsid);

    /// <summary>
    /// .NET Core compatible implementation of Marshal.GetActiveObject
    /// </summary>
    private static object GetActiveObject(string progId)
    {
        int hr = CLSIDFromProgID(progId, out Guid clsid);
        if (hr < 0)
            Marshal.ThrowExceptionForHR(hr);

        GetActiveObject(ref clsid, IntPtr.Zero, out object obj);
        return obj;
    }

    /// <summary>
    /// Gets the active Visual Studio DTE instance.
    /// Tries generic ProgID first, then VS2026-specific.
    /// </summary>
    public static DTE2? GetActiveVS()
    {
        // Return cached instance if still valid
        if (_cachedDte != null)
        {
            try
            {
                // Test if still connected by accessing a property
                _ = _cachedDte.Version;
                return _cachedDte;
            }
            catch
            {
                _cachedDte = null;
            }
        }

        // Try generic ProgID first
        try
        {
            _cachedDte = (DTE2)GetActiveObject("VisualStudio.DTE");
            return _cachedDte;
        }
        catch
        {
            // Try VS2026 specific ProgID
            try
            {
                _cachedDte = (DTE2)GetActiveObject("VisualStudio.DTE.18.0");
                return _cachedDte;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Gets all output window pane names.
    /// </summary>
    public static List<string> GetOutputPaneNames()
    {
        var dte = GetActiveVS();
        if (dte == null) return new List<string>();

        var names = new List<string>();
        var outputWindow = dte.ToolWindows.OutputWindow;

        foreach (OutputWindowPane pane in outputWindow.OutputWindowPanes)
        {
            names.Add(pane.Name);
        }

        return names;
    }

    /// <summary>
    /// Reads content from a specific output pane.
    /// </summary>
    /// <param name="paneName">Name (or partial name) of the pane</param>
    /// <param name="maxLines">Maximum number of lines to return (from the end). 0 = all lines.</param>
    public static string? ReadOutputPane(string paneName, int maxLines = 50)
    {
        var dte = GetActiveVS();
        if (dte == null) return null;

        var outputWindow = dte.ToolWindows.OutputWindow;

        OutputWindowPane? matchingPane = null;
        foreach (OutputWindowPane pane in outputWindow.OutputWindowPanes)
        {
            if (pane.Name.Contains(paneName, StringComparison.OrdinalIgnoreCase))
            {
                matchingPane = pane;
                break;
            }
        }

        if (matchingPane == null) return null;

        var textDoc = matchingPane.TextDocument;
        var startPoint = textDoc.StartPoint.CreateEditPoint();
        var fullText = startPoint.GetText(textDoc.EndPoint);

        if (maxLines <= 0) return fullText;

        // Truncate to last N lines
        var lines = fullText.Split('\n');
        if (lines.Length <= maxLines) return fullText;

        var truncatedLines = lines.Skip(lines.Length - maxLines).ToArray();
        return $"[Truncated to last {maxLines} lines]\n" + string.Join('\n', truncatedLines);
    }

    /// <summary>
    /// Gets all items from the Error List.
    /// </summary>
    public static List<ErrorItem> GetErrorListItems()
    {
        var dte = GetActiveVS();
        if (dte == null) return new List<ErrorItem>();

        var items = new List<ErrorItem>();
        var errorList = dte.ToolWindows.ErrorList;
        var errorItems = errorList.ErrorItems;

        for (int i = 1; i <= errorItems.Count; i++)
        {
            var item = errorItems.Item(i);
            items.Add(new ErrorItem
            {
                Severity = GetSeverityString(item),
                Description = item.Description,
                FileName = item.FileName,
                Line = item.Line,
                Column = item.Column,
                Project = item.Project
            });
        }

        return items;
    }

    private static string GetSeverityString(EnvDTE80.ErrorItem item)
    {
        // ErrorItem doesn't have a direct Severity property in all versions
        // We infer from the error code or default to "Error"
        try
        {
            var desc = item.Description?.ToLower() ?? "";
            if (desc.Contains("warning")) return "Warning";
            if (desc.Contains("message") || desc.Contains("info")) return "Message";
        }
        catch { }
        return "Error";
    }

    /// <summary>
    /// Gets information about the currently open solution.
    /// </summary>
    public static SolutionInfo? GetSolutionInfo()
    {
        var dte = GetActiveVS();
        if (dte?.Solution == null || string.IsNullOrEmpty(dte.Solution.FullName))
            return null;

        var info = new SolutionInfo
        {
            SolutionPath = dte.Solution.FullName,
            SolutionName = Path.GetFileName(dte.Solution.FullName),
            Projects = new List<string>()
        };

        foreach (Project project in dte.Solution.Projects)
        {
            if (!string.IsNullOrEmpty(project.Name))
            {
                info.Projects.Add(project.Name);
            }
        }

        return info;
    }

    // ==================== BUILD & DEBUG CONTROL ====================

    /// <summary>
    /// Builds the solution.
    /// </summary>
    /// <param name="waitForBuild">If true, waits for build to complete</param>
    /// <returns>Build result info</returns>
    public static BuildResult BuildSolution(bool waitForBuild = true)
    {
        var dte = GetActiveVS();
        if (dte?.Solution == null)
            return new BuildResult { Success = false, Message = "No solution is open" };

        var solutionBuild = dte.Solution.SolutionBuild;

        // Start the build
        solutionBuild.Build(waitForBuild);

        if (waitForBuild)
        {
            return new BuildResult
            {
                Success = solutionBuild.LastBuildInfo == 0,
                ErrorCount = solutionBuild.LastBuildInfo,
                Message = solutionBuild.LastBuildInfo == 0 
                    ? "Build succeeded" 
                    : $"Build failed with {solutionBuild.LastBuildInfo} error(s)"
            };
        }

        return new BuildResult { Success = true, Message = "Build started" };
    }

    /// <summary>
    /// Gets the current debugger state.
    /// </summary>
    public static DebuggerState GetDebuggerState()
    {
        var dte = GetActiveVS();
        if (dte == null)
            return new DebuggerState { Mode = "Unknown", Message = "No VS instance found" };

        var mode = dte.Debugger.CurrentMode;
        var state = new DebuggerState
        {
            Mode = mode.ToString(),
            IsDebugging = mode != dbgDebugMode.dbgDesignMode,
            IsRunning = mode == dbgDebugMode.dbgRunMode,
            IsAtBreakpoint = mode == dbgDebugMode.dbgBreakMode
        };

        if (state.IsAtBreakpoint && dte.Debugger.CurrentStackFrame != null)
        {
            try
            {
                dynamic currentFrame = dte.Debugger.CurrentStackFrame;
                state.CurrentFunction = currentFrame.FunctionName;
                state.CurrentFile = Path.GetFileName((string)(currentFrame.File ?? ""));
                state.CurrentLine = (int)(currentFrame.LineCharOffset ?? 0);
            }
            catch { }
        }

        state.Message = state.Mode switch
        {
            "dbgDesignMode" => "Not debugging (Design mode)",
            "dbgRunMode" => "Running (no breakpoint)",
            "dbgBreakMode" => $"At breakpoint: {state.CurrentFunction} ({state.CurrentFile}:{state.CurrentLine})",
            _ => state.Mode
        };

        return state;
    }

    /// <summary>
    /// Starts debugging (F5).
    /// </summary>
    public static string StartDebugging()
    {
        var dte = GetActiveVS();
        if (dte == null) return "Error: No VS instance found";

        var mode = dte.Debugger.CurrentMode;
        if (mode == dbgDebugMode.dbgRunMode)
            return "Already running";
        if (mode == dbgDebugMode.dbgBreakMode)
        {
            dte.Debugger.Go(false); // Continue from breakpoint
            return "Continued from breakpoint";
        }

        dte.Debugger.Go(false);
        return "Started debugging";
    }

    /// <summary>
    /// Starts without debugging (Ctrl+F5).
    /// </summary>
    public static string StartWithoutDebugging()
    {
        var dte = GetActiveVS();
        if (dte == null) return "Error: No VS instance found";

        try
        {
            dte.ExecuteCommand("Debug.StartWithoutDebugging");
            return "Started without debugging";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Stops debugging.
    /// </summary>
    public static string StopDebugging()
    {
        var dte = GetActiveVS();
        if (dte == null) return "Error: No VS instance found";

        var mode = dte.Debugger.CurrentMode;
        if (mode == dbgDebugMode.dbgDesignMode)
            return "Not currently debugging";

        dte.Debugger.Stop(false);
        return "Stopped debugging";
    }

    /// <summary>
    /// Gets the call stack (when at a breakpoint).
    /// </summary>
    public static List<StackFrameInfo> GetCallStack()
    {
        var dte = GetActiveVS();
        var frames = new List<StackFrameInfo>();

        if (dte?.Debugger?.CurrentThread?.StackFrames == null)
            return frames;

        foreach (dynamic frame in dte.Debugger.CurrentThread.StackFrames)
        {
            try
            {
                frames.Add(new StackFrameInfo
                {
                    FunctionName = frame.FunctionName ?? "",
                    FileName = Path.GetFileName((string)(frame.File ?? "")),
                    LineNumber = (int)(frame.LineCharOffset ?? 0),
                    Language = frame.Language ?? ""
                });
            }
            catch { /* Some frames may not have file info */ }
        }

        return frames;
    }
}

public class BuildResult
{
    public bool Success { get; set; }
    public int ErrorCount { get; set; }
    public string Message { get; set; } = "";
}

public class DebuggerState
{
    public string Mode { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsDebugging { get; set; }
    public bool IsRunning { get; set; }
    public bool IsAtBreakpoint { get; set; }
    public string? CurrentFunction { get; set; }
    public string? CurrentFile { get; set; }
    public int CurrentLine { get; set; }
}

public class StackFrameInfo
{
    public string FunctionName { get; set; } = "";
    public string FileName { get; set; } = "";
    public int LineNumber { get; set; }
    public string Language { get; set; } = "";
}

public class ErrorItem
{
    public string Severity { get; set; } = "Error";
    public string Description { get; set; } = "";
    public string FileName { get; set; } = "";
    public int Line { get; set; }
    public int Column { get; set; }
    public string Project { get; set; } = "";

    public override string ToString()
    {
        return $"[{Severity}] {Description} ({Path.GetFileName(FileName)}:{Line})";
    }
}

public class SolutionInfo
{
    public string SolutionPath { get; set; } = "";
    public string SolutionName { get; set; } = "";
    public List<string> Projects { get; set; } = new();
}
