using System;
namespace BeamScheduleFormatter {
sealed class ReportPaperScope:IDisposable {
 [ThreadStatic]static int depth;internal static bool Active{get{return depth>0;}}internal static readonly DesktopPalette Palette=new DesktopPalette(false);bool disposed;
 internal ReportPaperScope(){depth++;}public void Dispose(){if(disposed)return;disposed=true;depth--;}
}
}
