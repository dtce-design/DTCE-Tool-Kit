using System;using System.Runtime.InteropServices;using System.Threading;
namespace BeamScheduleFormatter {
// Owner (19 November 2026, "excel sync paused" / "hindi nagwowork yung map backup items"): since v1.29 the linked workbook opens in
// the owner's own, visible Excel with its macros and events on. While Excel is busy - a cell is being edited, a dialog or the
// macro security notice is open, or a workbook macro is running - it rejects calls from the Tool Kit, and the sync stopped with
// "Excel sync paused". The Excel thread now registers an OLE message filter that waits and retries rejected calls, and a call
// refused because Excel is in edit mode is tried again for a while before the owner is asked to finish the edit in Excel.
[ComImport,Guid("00000016-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IOleMessageFilter {[PreserveSig]int HandleInComingCall(int callType,IntPtr caller,int tickCount,IntPtr interfaceInfo);[PreserveSig]int RetryRejectedCall(IntPtr callee,int tickCount,int rejectType);[PreserveSig]int MessagePending(IntPtr callee,int tickCount,int pendingType);}
sealed class ExcelBusyFilter:IOleMessageFilter {
 internal const int RetryMilliseconds=60000;
 [DllImport("ole32.dll")]static extern int CoRegisterMessageFilter(IOleMessageFilter filter,out IOleMessageFilter old);
 /* registers the filter on the calling STA thread (the Excel worker thread); harmless where COM is not available */
 internal static void Register(){try{IOleMessageFilter old;CoRegisterMessageFilter(new ExcelBusyFilter(),out old);}catch(Exception){}}
 public int HandleInComingCall(int callType,IntPtr caller,int tickCount,IntPtr interfaceInfo){return 0;}
 /* SERVERCALL_RETRYLATER (2): try again after 250 ms, for up to a minute; SERVERCALL_REJECTED: give up */
 public int RetryRejectedCall(IntPtr callee,int tickCount,int rejectType){return rejectType==2&&tickCount<RetryMilliseconds?250:-1;}
 public int MessagePending(IntPtr callee,int tickCount,int pendingType){return 2;}
 internal const int CallRejected=unchecked((int)0x80010001),ExcelBusy=unchecked((int)0x800AC472),RetryLater=unchecked((int)0x8001010A);
 internal static bool IsBusy(Exception error){for(var e=error;e!=null;e=e.InnerException){var com=e as COMException;if(com!=null&&(com.ErrorCode==CallRejected||com.ErrorCode==ExcelBusy||com.ErrorCode==RetryLater))return true;}return false;}
 /* runs work again while Excel answers "busy" (edit mode, an open dialog), up to about 30 seconds */
 internal static T Retry<T>(Func<T> work){for(int attempt=0;;attempt++){try{return work();}catch(Exception e){if(!IsBusy(e)||attempt>=60)throw;Thread.Sleep(500);}}}
 internal const string BusyMessage="Excel is busy and did not accept the update: finish or cancel the cell you are editing (Enter or Esc) and close any open Excel dialog or security notice, then use Sync to Excel or Map BACKUP items again.";
}
}
