using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Diagnostics.Process;
using static System.IntPtr;
using static System.Runtime.InteropServices.Marshal;

namespace RVis.Client
{
  // based on https://stackoverflow.com/a/37034966

  public static class NativeMethods
  {
    public static bool TerminateOnAppExit(Process process)
    {
      if (_jobHandle == Zero) return false;

      var success = AssignProcessToJobObject(_jobHandle, process.Handle);

      return success || process.HasExited;
    }

    static NativeMethods()
    {
      var jobName = nameof(NativeMethods) + "CPT" + Environment.ProcessId;
      var jobHandle = CreateJobObject(Zero, jobName);

      var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION
      {
        LimitFlags = JOBOBJECTLIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
      };

      var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
      {
        BasicLimitInformation = info
      };

      var length = SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
      var extendedInfoPtr = AllocHGlobal(length);
      try
      {
        StructureToPtr(extendedInfo, extendedInfoPtr, false);

        var success = SetInformationJobObject(
          jobHandle,
          JobObjectInfoType.ExtendedLimitInformation,
          extendedInfoPtr,
          (uint)length
          );

        if (success) _jobHandle = jobHandle;
      }
      finally
      {
        FreeHGlobal(extendedInfoPtr);
      }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string name);

    [DllImport("kernel32.dll")]
    static extern bool SetInformationJobObject(
      IntPtr job,
      JobObjectInfoType infoType,
      IntPtr lpJobObjectInfo,
      uint cbJobObjectInfoLength
      );

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    private static readonly IntPtr _jobHandle;
  }

  public enum JobObjectInfoType
  {
    AssociateCompletionPortInformation = 7,
    BasicLimitInformation = 2,
    BasicUIRestrictions = 4,
    EndOfJobTimeInformation = 6,
    ExtendedLimitInformation = 9,
    SecurityLimitInformation = 5,
    GroupInformation = 11
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
  {
    public Int64 PerProcessUserTimeLimit;
    public Int64 PerJobUserTimeLimit;
    public JOBOBJECTLIMIT LimitFlags;
    public UIntPtr MinimumWorkingSetSize;
    public UIntPtr MaximumWorkingSetSize;
    public UInt32 ActiveProcessLimit;
    public Int64 Affinity;
    public UInt32 PriorityClass;
    public UInt32 SchedulingClass;
  }

  [Flags]
  public enum JOBOBJECTLIMIT : uint
  {
    JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct IO_COUNTERS
  {
    public UInt64 ReadOperationCount;
    public UInt64 WriteOperationCount;
    public UInt64 OtherOperationCount;
    public UInt64 ReadTransferCount;
    public UInt64 WriteTransferCount;
    public UInt64 OtherTransferCount;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
  {
    public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
    public IO_COUNTERS IoInfo;
    public UIntPtr ProcessMemoryLimit;
    public UIntPtr JobMemoryLimit;
    public UIntPtr PeakProcessMemoryUsed;
    public UIntPtr PeakJobMemoryUsed;
  }
}