using System;using System.Linq;using System.Globalization;using System.Runtime.CompilerServices;using System.Text.RegularExpressions;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Supported reinforcing bar diameters (owner, 18 November 2026: protect every calculation against a diameter that
// is not in the supported list). Same list as the nominal masses in EstimatorEngine.UnitMass.
static class RebarDiameters {
 internal static readonly int[] Supported={10,12,16,20,25,28,32,36,40,50};
 internal static string List{get{return string.Join(", ",Supported)+" mm";}}
 internal static bool IsSupported(double diameter){foreach(int d in Supported)if(Math.Abs(diameter-d)<1e-9)return true;return false;}
 internal static string Message(string value){return "Bar diameter "+value+" mm is not a supported rebar size. Use one of: "+List+".";}
}
static class RebarDiameterText {
 internal static string Raw(object value){return Regex.Replace(Convert.ToString(value,CultureInfo.InvariantCulture)??"","[Øø⌀]|\\s*mm\\s*", "",RegexOptions.IgnoreCase).Trim();}
 internal static string Format(object value){string raw=Raw(value);double diameter;return double.TryParse(raw,NumberStyles.Float,CultureInfo.InvariantCulture,out diameter)&&diameter>0?"Ø"+diameter.ToString("0.###",CultureInfo.InvariantCulture)+" mm":Convert.ToString(value,CultureInfo.InvariantCulture);}
 internal static bool IsColumn(string[] headers,int index){string name=headers[index]??"";if(name.Trim().Equals("Dia",StringComparison.OrdinalIgnoreCase)||name.Contains("Ø"))return true;if(!Regex.IsMatch(name,@"\b(dia|diameter)\b",RegexOptions.IgnoreCase))return false;return headers.Any(h=>Regex.IsMatch(h??"",@"rebar|reinforc|bar id|cover|bars per|bars /|bar count|tie diameter|main diameter|cut steel|net weight",RegexOptions.IgnoreCase));}
}
static class RebarDiameterDisplay {
 static readonly ConditionalWeakTable<DataGridView,object> applied=new ConditionalWeakTable<DataGridView,object>();
 internal static void Attach(DataGridView grid){object seen;if(applied.TryGetValue(grid,out seen))return;applied.Add(grid,new object());Func<int,bool> diameter=column=>column>=0&&column<grid.ColumnCount&&RebarDiameterText.IsColumn(grid.Columns.Cast<DataGridViewColumn>().Select(c=>c.HeaderText).ToArray(),column);
 grid.CellFormatting+=(s,e)=>{if(e.RowIndex<0||!diameter(e.ColumnIndex)||e.Value==null)return;if(grid.IsCurrentCellInEditMode&&grid.CurrentCell==grid.Rows[e.RowIndex].Cells[e.ColumnIndex])return;e.Value=RebarDiameterText.Format(e.Value);e.FormattingApplied=true;};
 grid.CellParsing+=(s,e)=>{if(e.RowIndex<0||!diameter(e.ColumnIndex))return;e.Value=RebarDiameterText.Raw(e.Value);e.ParsingApplied=true;};
 grid.EditingControlShowing+=(s,e)=>{if(grid.CurrentCell==null||!diameter(grid.CurrentCell.ColumnIndex))return;var text=e.Control as TextBox;if(text!=null)text.Text=RebarDiameterText.Raw(text.Text);};
 // Protection (owner, 18 November 2026): a typed diameter that is not a supported rebar size is not accepted.
 ToolTip tip=null;grid.Disposed+=(s,e)=>{if(tip!=null)tip.Dispose();};
 grid.CellValidating+=(s,e)=>{if(e.RowIndex<0||!diameter(e.ColumnIndex)||grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly)return;string raw=RebarDiameterText.Raw(e.FormattedValue);double value;
  if(raw==""||!double.TryParse(raw,NumberStyles.Float,CultureInfo.InvariantCulture,out value)||RebarDiameters.IsSupported(value)){grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText="";return;}
  e.Cancel=true;string message=RebarDiameters.Message(raw);grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText=message;
  try{if(tip==null)tip=new ToolTip{IsBalloon=false,ToolTipIcon=ToolTipIcon.Warning,ToolTipTitle="Unsupported bar diameter"};var rect=grid.GetCellDisplayRectangle(e.ColumnIndex,e.RowIndex,false);tip.Show(message,grid,rect.Left,rect.Bottom+2,5000);}catch(Exception){}};
 grid.CellEndEdit+=(s,e)=>{if(e.RowIndex<0||e.RowIndex>=grid.RowCount||!diameter(e.ColumnIndex))return;var cell=grid.Rows[e.RowIndex].Cells[e.ColumnIndex];double value;string raw=RebarDiameterText.Raw(cell.Value);if(raw==""||!double.TryParse(raw,NumberStyles.Float,CultureInfo.InvariantCulture,out value)||RebarDiameters.IsSupported(value))cell.ErrorText="";if(tip!=null)tip.Hide(grid);};
 }
}
}
