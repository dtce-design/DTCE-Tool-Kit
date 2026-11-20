using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Runtime.CompilerServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Excel-like formulas in the Program of Works input cells (owner, 20 November 2026: "sa mga input sa POW allow maginput na parang
   calculator like excel =0.6+0.3 tapos ang lalabas e yung total, naka save pa din yung formula sa cell"). A cell typed with "="
   shows the result and keeps the formula: editing the cell shows the formula again, the tooltip shows it, and the project file
   stores it next to the value (attribute f<n>). Supported: + - * / ^, parentheses, decimals; "x" also multiplies. */
static class GridFormulas {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 sealed class Box {public string Text;}
 static readonly ConditionalWeakTable<DataGridViewCell,Box> formulas=new ConditionalWeakTable<DataGridViewCell,Box>();
 static readonly ConditionalWeakTable<DataGridView,Box> attached=new ConditionalWeakTable<DataGridView,Box>();
 internal static readonly Color FormulaColor=Color.FromArgb(20,90,160);
 public static string Formula(DataGridViewCell cell){Box b;return cell!=null&&formulas.TryGetValue(cell,out b)?b.Text:null;}
 public static void Set(DataGridViewCell cell,string formula){if(cell==null)return;formulas.Remove(cell);if(string.IsNullOrWhiteSpace(formula)){cell.Style.ForeColor=Color.Empty;return;}formulas.Add(cell,new Box{Text=formula.Trim()});cell.Style.ForeColor=FormulaColor;}
 public static string Format(double v){return Math.Round(v,10).ToString("0.##########",Inv);}
 /* every editable grid under the control, now and when added later */
 public static void AttachAll(Control root){if(root==null)return;var grid=root as DataGridView;if(grid!=null)Attach(grid);foreach(Control c in root.Controls)AttachAll(c);root.ControlAdded+=(s,e)=>AttachAll(e.Control);}
 public static void Attach(DataGridView g){Box b;if(g==null||attached.TryGetValue(g,out b))return;attached.Add(g,new Box());
  g.CellParsing+=(s,e)=>{string text=e.Value as string;if(text==null||e.RowIndex<0||e.ColumnIndex<0)return;var cell=g[e.ColumnIndex,e.RowIndex];text=text.Trim();
   if(!text.StartsWith("=")){Set(cell,null);return;}double v;string error;if(!TryEvaluate(text.Substring(1),out v,out error)){cell.ErrorText="Formula: "+error;return;}cell.ErrorText="";Set(cell,text);
   if(e.DesiredType==typeof(double))e.Value=v;else if(e.DesiredType==typeof(decimal))e.Value=(decimal)v;else if(e.DesiredType==typeof(int))e.Value=(int)Math.Round(v);else e.Value=Format(v);e.ParsingApplied=true;};
  /* also a formula committed or pasted without parsing (some grids, clipboard paste): evaluate it once the value is in */
  bool busy=false;g.CellValueChanged+=(s,e)=>{if(busy||e.RowIndex<0||e.ColumnIndex<0)return;var cell=g[e.ColumnIndex,e.RowIndex];string text=Convert.ToString(cell.Value,Inv);if(!text.Trim().StartsWith("=")){/* a different plain value replaces the formula */string old=Formula(cell);double ov;string oe;if(old!=null&&!(TryEvaluate(old.Substring(1),out ov,out oe)&&Format(ov)==text.Trim()))Set(cell,null);return;}double v;string error;text=text.Trim();
   if(!TryEvaluate(text.Substring(1),out v,out error)){cell.ErrorText="Formula: "+error;return;}cell.ErrorText="";busy=true;try{Set(cell,text);cell.Value=Format(v);}finally{busy=false;}};
  g.EditingControlShowing+=(s,e)=>{var cell=g.CurrentCell;string f=Formula(cell);var box=e.Control as TextBox;if(f!=null&&box!=null){box.Text=f;box.SelectionStart=f.Length;}};
  g.CellToolTipTextNeeded+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex<0)return;string f=Formula(g[e.ColumnIndex,e.RowIndex]);if(f!=null)e.ToolTipText="Formula "+f;};}
 /* arithmetic: expression = term {(+|-) term}; term = factor {(*|/|x) factor}; factor = unary [^ factor]; unary = [-|+] primary */
 public static bool TryEvaluate(string text,out double value,out string error){value=0;error="";try{var p=new Parser{S=(text??"").Replace(" ","").Replace(",",""),I=0};if(p.S.Length==0)throw new FormatException("empty formula");value=p.Expression();if(p.I<p.S.Length)throw new FormatException("unexpected '"+p.S[p.I]+"'");if(double.IsNaN(value)||double.IsInfinity(value))throw new FormatException("the result is not a number");return true;}catch(Exception ex){error=ex is DivideByZeroException?"division by zero":ex.Message;return false;}}
 sealed class Parser {public string S;public int I;
  char Peek{get{return I<S.Length?S[I]:'\0';}}
  public double Expression(){double v=Term();while(Peek=='+'||Peek=='-'){char op=S[I++];double r=Term();v=op=='+'?v+r:v-r;}return v;}
  double Term(){double v=Power();while(Peek=='*'||Peek=='/'||Peek=='x'||Peek=='X'){char op=S[I++];double r=Power();if(op=='/'){if(r==0)throw new DivideByZeroException();v/=r;}else v*=r;}return v;}
  double Power(){double v=Unary();if(Peek=='^'){I++;double r=Power();v=Math.Pow(v,r);}return v;}
  double Unary(){if(Peek=='-'){I++;return -Unary();}if(Peek=='+'){I++;return Unary();}return Primary();}
  double Primary(){if(Peek=='('){I++;double v=Expression();if(Peek!=')')throw new FormatException("missing ')'");I++;return v;}int start=I;while(I<S.Length&&(char.IsDigit(S[I])||S[I]=='.'))I++;if(start==I)throw new FormatException(I<S.Length?"unexpected '"+S[I]+"'":"incomplete formula");double num;if(!double.TryParse(S.Substring(start,I-start),NumberStyles.Float,Inv,out num))throw new FormatException("bad number "+S.Substring(start,I-start));return num;}
 }
}
}
