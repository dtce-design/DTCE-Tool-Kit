using System;using System.Collections.Generic;using System.Linq;
namespace BeamScheduleFormatter {
/* Bill of Quantities -> POW BACKUP (owner, 20 November 2026: "yung backup sa pow nakalink sa boq workspace"): the quantity of every pay
   item exactly as the Bill of Quantities shows it (computed, typed Part A / B, manual), with its BACKUP row, item no. and unit. */
sealed class BoqPowItem{public string Code,Description,Unit;public int Row;public double Quantity;}
partial class ProgramOfWorksForm {
 internal List<BoqPowItem> PowBackupItems(){return SchedulePayQuantities().Select(p=>new BoqPowItem{Code=(p.Key.Code??"").Trim(),Description=p.Key.Description,Unit=p.Key.Unit,Row=p.Key.Row,Quantity=p.Value}).ToList();}
 internal static ProgramOfWorksForm OpenWorkspace{get{var f=Live==null?null:Live.Target as ProgramOfWorksForm;return f==null||f.IsDisposed?null:f;}}
}
/* places Bill of Quantities quantities on the BACKUP rows of a POW workbook (column BC); item rows not in the Bill of Quantities are
   cleared - typed or formula - so that only the linked quantities flow into DETAILED, ABC, SUM and BOQ (Progress Photographs keep the
   workbook's months unless the Bill of Quantities has them) */
static class PowBackupLink {
 public const int CodeColumn=2,QuantityColumn=55;
 static bool IsCode(string s){return s.Length>0&&s.Length<40&&(char.IsDigit(s[0])||(s.Length>1&&char.IsLetter(s[0])&&s[1]=='.'));}
 public sealed class Result{public Dictionary<string,object> Values=new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);public List<BoqPowItem> Unmatched=new List<BoqPowItem>();public int Matched;}
 public static Result Plan(WbBook book,WbCalc calc,IEnumerable<BoqPowItem> items){var res=new Result();var sheet=book.Sheet("BACKUP");if(sheet==null){res.Unmatched.AddRange(items);return res;}
  var rows=new Dictionary<string,List<int>>(StringComparer.OrdinalIgnoreCase);
  for(int r=1;r<=sheet.MaxRow;r++){var codeCell=sheet.Get(r,CodeColumn);if(codeCell==null)continue;var v=calc.CellValue(sheet,codeCell) as string;if(v==null)continue;v=v.Trim();if(!IsCode(v))continue;var q=sheet.Get(r,QuantityColumn);if(q!=null&&(q.ArrayMaster!=null||q.IsArrayMaster))continue;/* formula quantities (the template's own takeoff) are replaced too (owner, 23 November 2026) */List<int> list;if(!rows.TryGetValue(v,out list))rows[v]=list=new List<int>();list.Add(r);}
  var wanted=new Dictionary<int,double>();
  foreach(var item in items){List<int> list;if(!rows.TryGetValue(item.Code,out list)||list.Count==0){res.Unmatched.Add(item);continue;}int row=list.Contains(item.Row)?item.Row:list.OrderBy(r=>Math.Abs(r-item.Row)).First();double prev;wanted[row]=(wanted.TryGetValue(row,out prev)?prev:0)+item.Quantity;res.Matched++;}
  /* Progress Photographs keep the workbook quantity unless the Bill of Quantities has one (owner, 23 November 2026) */
  Func<int,bool> photo=r=>{for(int c=CodeColumn+1;c<=CodeColumn+8;c++){var d=sheet.Get(r,c);if(d==null)continue;var t=calc.CellValue(sheet,d) as string;if(t!=null&&BackupBillItem.WorkbookItem(t))return true;}return false;};
  foreach(var list in rows.Values)foreach(var r in list){double q;if(wanted.TryGetValue(r,out q))res.Values["BACKUP!"+WbBook.ColumnName(QuantityColumn)+r]=q;else if(!photo(r))res.Values["BACKUP!"+WbBook.ColumnName(QuantityColumn)+r]=null;}
  return res;}
 public static string Signature(IEnumerable<BoqPowItem> items){return string.Join("|",items.OrderBy(i=>i.Row).ThenBy(i=>i.Code).Select(i=>i.Code+"@"+i.Row+"="+i.Quantity.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));}
}
}
