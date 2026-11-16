using System;using System.Collections.Generic;
namespace BeamScheduleFormatter {
static class ProjectReportHeader {
 internal static List<string> Lines(ProjectInformation project,double width){var result=EngineeringQuantityPdf.Wrap(project.NameLine,width,8);result.AddRange(EngineeringQuantityPdf.Wrap(project.LocationLine,width,8));return result;}
 internal static float PreviewBodyTop(ProjectInformation project){return Math.Max(125,68+Lines(project,475).Count*10+14);}
 internal static bool UseFlowingPreview(ProjectInformation project){return Lines(project,475).Count>40;}
 internal static int PreviewRows(ProjectInformation project){return UseFlowingPreview(project)?54:Math.Max(1,Math.Min(54,(int)((795-PreviewBodyTop(project))/12)));}
}
}
