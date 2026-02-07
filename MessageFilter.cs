using System.Runtime.InteropServices;

namespace VisualStudioMcpServer;

/// <summary>
/// COM Message Filter to handle Visual Studio's busy state.
/// VS is a single-threaded COM server and will reject calls with RPC_E_CALL_REJECTED when busy.
/// This filter implements retry logic to gracefully handle those cases.
/// </summary>
[ComImport]
[Guid("00000016-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IOleMessageFilter
{
    [PreserveSig]
    int HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo);

    [PreserveSig]
    int RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType);

    [PreserveSig]
    int MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType);
}

public class MessageFilter : IOleMessageFilter
{
    private const int SERVERCALL_ISHANDLED = 0;
    private const int PENDINGMSG_WAITDEFPROCESS = 2;
    private const int SERVERCALL_RETRYLATER = 2;

    [DllImport("Ole32.dll")]
    private static extern int CoRegisterMessageFilter(IOleMessageFilter? newFilter, out IOleMessageFilter? oldFilter);

    /// <summary>
    /// Registers the message filter to handle COM retry logic.
    /// Must be called from an STA thread before any COM operations.
    /// </summary>
    public static void Register()
    {
        IOleMessageFilter newFilter = new MessageFilter();
        CoRegisterMessageFilter(newFilter, out _);
    }

    /// <summary>
    /// Unregisters the message filter. Call on application exit.
    /// </summary>
    public static void Revoke()
    {
        CoRegisterMessageFilter(null, out _);
    }

    int IOleMessageFilter.HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo)
    {
        return SERVERCALL_ISHANDLED;
    }

    int IOleMessageFilter.RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType)
    {
        // If server is busy (SERVERCALL_RETRYLATER), retry after 99ms
        if (dwRejectType == SERVERCALL_RETRYLATER)
        {
            return 99;
        }
        // Otherwise, cancel the call
        return -1;
    }

    int IOleMessageFilter.MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType)
    {
        return PENDINGMSG_WAITDEFPROCESS;
    }
}
