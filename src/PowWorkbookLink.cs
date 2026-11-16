using System;using System.IO;using System.Security;using System.Threading.Tasks;
namespace BeamScheduleFormatter {
static class PowWorkbookLink {
 internal static async Task<string> Start(Func<Task> capture,Func<Task> connect){string warning="";try{await capture();}catch(IOException ex){warning=ex.Message;}catch(UnauthorizedAccessException ex){warning=ex.Message;}catch(SecurityException ex){warning=ex.Message;}await connect();return warning;}
}
}
