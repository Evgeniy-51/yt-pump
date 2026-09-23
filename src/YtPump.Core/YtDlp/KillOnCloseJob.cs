using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace YtPump.Core.YtDlp;

internal sealed class KillOnCloseJob : IDisposable
{
    private const int ExtendedLimitInfoClass = 9;
    private const uint LimitKillOnJobClose = 0x2000;

    private readonly SafeJobHandle _handle;

    private KillOnCloseJob(SafeJobHandle handle) => _handle = handle;

    public static KillOnCloseJob? TryAssign(Process process)
    {
        var raw = Native.CreateJobObject(IntPtr.Zero, null);
        if (raw == IntPtr.Zero)
        {
            return null;
        }

        var job = new KillOnCloseJob(new SafeJobHandle(raw));
        if (!job.EnableKillOnClose())
        {
            job.Dispose();
            return null;
        }

        try
        {
            if (!Native.AssignProcessToJobObject(job._handle, process.Handle))
            {
                job.Dispose();
                return null;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            job.Dispose();
            return null;
        }

        return job;
    }

    public void Dispose() => _handle.Dispose();

    private bool EnableKillOnClose()
    {
        var info = new JobObjectExtendedLimitInformation
        {
            BasicLimitInformation = new JobObjectBasicLimitInformation
            {
                LimitFlags = LimitKillOnJobClose
            }
        };

        var size = Marshal.SizeOf<JobObjectExtendedLimitInformation>();
        var ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(info, ptr, fDeleteOld: false);
            return Native.SetInformationJobObject(_handle, ExtendedLimitInfoClass, ptr, (uint)size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    private sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeJobHandle(IntPtr handle) : base(ownsHandle: true) => SetHandle(handle);

        protected override bool ReleaseHandle() => Native.CloseHandle(handle);
    }

    private static class Native
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetInformationJobObject(
            SafeHandle hJob,
            int jobObjectInfoClass,
            IntPtr lpJobObjectInfo,
            uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AssignProcessToJobObject(SafeHandle hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectBasicLimitInformation
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public nuint MinimumWorkingSetSize;
        public nuint MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public nuint Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectExtendedLimitInformation
    {
        public JobObjectBasicLimitInformation BasicLimitInformation;
        public IoCounters IoInfo;
        public nuint ProcessMemoryLimit;
        public nuint JobMemoryLimit;
        public nuint PeakProcessMemoryUsed;
        public nuint PeakJobMemoryUsed;
    }
}
