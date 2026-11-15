using System;using System.Linq;using System.Globalization;using System.Runtime.CompilerServices;using System.Text.RegularExpressions;using System.Windows.Forms;
namespace BeamScheduleFormatter {
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
 }
}
}
