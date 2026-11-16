using System;using System.Globalization;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class ProgramOfWorksForm {
 void ConfigureBoqQuantityDisplay(){boqGrid.CellFormatting+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex!=2||e.Value==null)return;double value;if(double.TryParse(Convert.ToString(e.Value,CultureInfo.InvariantCulture),NumberStyles.Float|NumberStyles.AllowThousands,CultureInfo.InvariantCulture,out value)&&!double.IsNaN(value)&&!double.IsInfinity(value)){e.Value=value.ToString("N2",CultureInfo.InvariantCulture);e.FormattingApplied=true;}};}
}
}
