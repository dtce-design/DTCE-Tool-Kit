using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* The Bill of Quantities calculation report laid out like the design-sheet reports (owner, 24 November 2026: "masmaayos na presentation
   ng boq report"; "palitan yung presentation ng calculation ng BOQ REPORT"; "lahat irestructure mo, hindi lang rebar table").
   Structure: REPORT and CONTENTS tables; the Bill of Quantities; then one numbered section per work with
     k.1  Quantities - material / work, quantity, unit and where its calculation is (k.n);
     k.n  <material> - calculation as a calculation sheet (no grid): numbered lines, the members in bold, the calculation with its
          values under them, the result on the right, the total under a rule; identical calculations are one line ("40 × 4.5");
   the reinforcing steel record the same way, per member; the measurement inputs as column tables (Appendix A).
   Nothing is dropped: every section, note and table of the report is placed in order. */
static class QuantityReportLayout {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static readonly Color TotalTint=Color.FromArgb(232,242,236);
 static bool IsTotal(string[] row){return row.Any(c=>c!=null&&(c.StartsWith("TOTAL",StringComparison.OrdinalIgnoreCase)||c.IndexOf("TO BUY",StringComparison.OrdinalIgnoreCase)>=0||c=="Member total"));}
 static Color? Tint(string[] row){return IsTotal(row)?(Color?)TotalTint:null;}
 /* a column's share of the line: text columns wide, numbers and units narrow */
 static float Weight(string header){string h=(header??"").ToLowerInvariant();
  if(h.StartsWith("how the bar"))return 3f;if(h.StartsWith("net weight"))return 2.4f;if(h.StartsWith("laps"))return 1f;
  if(h.Contains("formula"))return 3f;if(h=="pieces"||h=="bar"||h.StartsWith("cut")||h=="kg/m"||h.EndsWith("(kg)"))return .75f;
  if(h.Contains("calculation")||h.Contains("basis")||h.Contains("inputs")||h.Contains("reused by")||h.Contains("opened for"))return 3.6f;
  if(h.Contains("description")||h.Contains("source")||h.Contains("material")||h.Contains("member")||h.Contains("input issue")||h.Contains("commercial"))return 2.6f;
  if(h=="unit"||h=="no."||h=="id"||h.Contains("stocks")||h.Contains("whole bars"))return .7f;
  if(h.Contains("pay item"))return 1f;
  if(h.Contains("quantity")||h.Contains("kg")||h.Contains("final offcut"))return .9f;return 1.2f;}
 static void Plain(RichReport rep,EngineeringReportBlock r){if(r.Rows.Count==0)return;
  var rows=r.Rows.Select(row=>row.Length>1&&row[0]!=null&&row[0].StartsWith("▸ ")&&row.Skip(1).All(string.IsNullOrEmpty)?new[]{Name(row[0].Substring(2))}:row.Select(Clean).ToArray());
  rep.Table(r.Headers,rows,r.Headers.Select(Weight).ToArray(),Tint);}
 static string BreakdownName(EngineeringReportBlock r){const string tail=" — source breakdown";return r.Kind!="table"&&r.Text!=null&&r.Text.EndsWith(tail)?r.Text.Substring(0,r.Text.Length-tail.Length):null;}
 static bool IsQuantities(EngineeringReportBlock r){return r.Kind=="table"&&r.Headers!=null&&r.Headers.Length>=3&&r.Headers[0]=="Material / work";}
 static bool IsSources(EngineeringReportBlock r){return r.Kind=="table"&&r.Headers!=null&&r.Headers.Length==4&&r.Headers[0]=="Source / member"&&r.Headers[3].StartsWith("Calculation");}
 static bool IsInputs(EngineeringReportBlock r){return r.Kind=="table"&&r.Headers!=null&&r.Headers.Length==3&&r.Headers[0]=="ID"&&r.Headers[2]=="Inputs";}
 /* "sum of 40 sources: 4.5 + 4.5 + … = 180 m3 (each source …)" -> "Σ 40 lines, 2.2 = 180 m3" when the lines are in this report */
 static string Basis(string basis,string where){if(basis==null)return basis;var m=Regex.Match(basis,@"^sum of (\d+) sources?:.*?= ([^=(]+?)\s*(\(each source.*\))?$");return m.Success?"Σ "+m.Groups[1].Value+" source lines"+(string.IsNullOrEmpty(where)?"":" in "+where)+" = "+m.Groups[2].Value.Trim():basis;}
 /* source table -> No. | Source / member | Calculation | Result; sources with the same calculation are one line (the members listed once,
    "40 × 4.5 = 180 m3"), ending with the TOTAL row */
 static string Name(string source){return Clean(source).Replace(" | "," / ");}
 internal static string Clean(string text){return text==null?null:Regex.Replace(text,@"\s*\|\s*Manual\s*\|\s*Manual beam schedules","");}
 /* "Footing F-001 / F, Footing F-002 / F, …" -> "Footing F-001 to F-060 / F": the common first word and last part written once,
    runs of 3 or more consecutive numbers as a range */
 internal static string Members(List<string> names){if(names.Count<2)return string.Join(", ",names);string head="",tail="";
  int seg=names[0].IndexOf(" / ");if(seg>0){string first=names[0].Substring(0,seg+3);if(names.All(x=>x.StartsWith(first)&&x.Length>first.Length)){head=first.Substring(0,seg)+": ";names=names.Select(x=>x.Substring(first.Length)).ToList();}}
  var w=names[0].Split(' ');if(w.Length>1&&names.All(x=>x.StartsWith(w[0]+" "))){head+=w[0]+" ";names=names.Select(x=>x.Substring(w[0].Length+1)).ToList();}
  int cut=names[0].LastIndexOf(" / ");if(cut>0){string t=names[0].Substring(cut);if(names.All(x=>x.EndsWith(t)&&x.Length>t.Length)){tail=t;names=names.Select(x=>x.Substring(0,x.Length-t.Length)).ToList();}}
  {int sp=names[0].LastIndexOf(' ');if(sp>0){string lw=names[0].Substring(sp);if(lw.Length>1&&names.All(x=>x.EndsWith(lw)&&x.Length>lw.Length&&x.IndexOf(" / ")<0)){tail=lw+tail;names=names.Select(x=>x.Substring(0,x.Length-lw.Length)).ToList();}}}
  /* "B-2 / Level 1, B-3 / Level 2 …" (same calculation) -> "B-1 to B-40 (Level 0, Level 1, Level 2, Level 3)" */
  if(names.All(x=>x.Contains(" / "))){var ids=names.Select(x=>x.Substring(0,x.IndexOf(" / "))).ToList();var rest=names.Select(x=>x.Substring(x.IndexOf(" / ")+3)).Distinct().ToList();
   if(rest.Count<names.Count&&ids.Distinct().Count()==ids.Count)return head+Ranges(ids.OrderBy(x=>Regex.Replace(x,@"\d+",d=>d.Value.PadLeft(9,'0'))).ToList())+tail+"  ("+string.Join(", ",rest.OrderBy(x=>x))+")";}
  return head+Ranges(names)+tail;}
 static string Ranges(List<string> names){var parts=new List<string>();int i=0;while(i<names.Count){var m=Regex.Match(names[i],@"^(.*?)(\d+)$");int j=i;
   if(m.Success){long a=long.Parse(m.Groups[2].Value,Ci);while(j+1<names.Count){var m2=Regex.Match(names[j+1],@"^(.*?)(\d+)$");if(!m2.Success||m2.Groups[1].Value!=m.Groups[1].Value||long.Parse(m2.Groups[2].Value,Ci)!=a+(j+1-i))break;j++;}}
   parts.Add(j-i>=2?names[i]+" to "+names[j]:names[i]);if(j-i==1)parts.Add(names[j]);i=j+1;}
  return string.Join(", ",parts);}
 /* source lines as a calculation sheet: "1  Beam B-1 to B-40 / Level … (40 sources, same calculation)" in bold, the calculation with its
    values under it, the result on the right ("40 × 4.5 = 180 m3"), then the Total under a rule */
 static void Sources(RichReport rep,EngineeringReportBlock r){if(r.Rows.Count==0)return;int n=0;double sum=0;bool numeric=true;string unit=null;
  foreach(var row in r.Rows){double q;if(double.TryParse(row[1],NumberStyles.Float,Ci,out q))sum+=q;else numeric=false;if(unit==null)unit=row[2];else if(unit!=row[2])numeric=false;}
  foreach(var g in r.Rows.GroupBy(row=>(row[3]??"")+"\u0001"+row[1]+"\u0001"+row[2])){var first=g.First();int count=g.Count();string calc=string.IsNullOrWhiteSpace(first[3])?"entered value":first[3];
   double q;string result=count>1&&double.TryParse(first[1],NumberStyles.Float,Ci,out q)?count.ToString(Ci)+" × "+first[1]+" = "+(q*count).ToString("0.###",Ci)+" "+first[2]:first[1]+" "+first[2];
   string names=Members(g.Select(row=>Name(row[0])).ToList());rep.Calc((++n).ToString(Ci),count>1?names+"  ("+count.ToString(Ci)+" sources, each)":names,calc,result);}
  if(numeric&&n>1)rep.Calc("","","Total (lines 1 to "+n.ToString(Ci)+")",sum.ToString("0.###",Ci)+" "+unit,true);}
 /* the reinforcing steel record (Bar | How the bar is cut | Net weight | Laps + offcut | Purchase, member rows "▸ …") as a calculation sheet */
 static bool IsRebarRecord(EngineeringReportBlock r){return r.Kind=="table"&&r.Headers!=null&&r.Headers.Length==5&&r.Headers[0]=="Bar"&&r.Headers[1].StartsWith("How the bar");}
 static void RebarRecord(RichReport rep,EngineeringReportBlock r){
  /* members with the same bars are written once ("B-1 to B-40 (40 members, each)") with the member total × count */
  var members=new List<KeyValuePair<string,List<string[]>>>();string[] total=null;
  foreach(var row in r.Rows){if(row[0]!=null&&row[0].StartsWith("▸ "))members.Add(new KeyValuePair<string,List<string[]>>(Name(row[0].Substring(2)),new List<string[]>()));else if(row[0]=="TOTAL")total=row;else if(members.Count>0)members[members.Count-1].Value.Add(row);}
  foreach(var g in members.GroupBy(m=>string.Join("\u0002",m.Value.Select(x=>string.Join("\u0001",x))))){var list=g.ToList();int count=list.Count;var bars=list[0].Value;
   rep.Paragraph(count>1?Members(list.Select(m=>m.Key).ToList())+"  ("+count.ToString(Ci)+" members, each)":list[0].Key,false,true);int n=0;double each=0;
   foreach(var row in bars){if(row[2]=="Member total")continue;var lo=(row[3]??"").Split(new[]{" + "},StringSplitOptions.None);string extra=lo.Length==2?" + laps "+lo[0]+" + offcut "+lo[1]+" kg":"";double kg;if(double.TryParse(row[4],NumberStyles.Float,Ci,out kg))each+=kg;
    rep.Calc((++n).ToString(Ci),row[0]+":  "+row[1],row[2]+extra,row[4]+" kg");}
   if(n>1||count>1)rep.Calc("","",count>1?"Member total × "+count.ToString(Ci):"Member total",(count>1?count.ToString(Ci)+" × "+each.ToString("0.000",Ci)+" = ":"")+(each*count).ToString("0.000",Ci)+" kg",true);}
  if(total!=null)rep.Calc("","","TOTAL reinforcing steel to buy",total[4]+" kg",true);}
 /* "Width m = 4; Height m = 3" per row -> one column per input when the rows share up to 11 inputs, else one input per line */
 static void Inputs(RichReport rep,EngineeringReportBlock r){if(r.Rows.Count==0)return;
  var parsed=r.Rows.Select(row=>(row[2]??"").Split(new[]{"; "},StringSplitOptions.RemoveEmptyEntries).Select(p=>{int i=p.IndexOf(" = ");return i<0?new[]{"Input",p}:new[]{p.Substring(0,i),p.Substring(i+3)};}).ToList()).ToList();
  var keys=parsed.SelectMany(p=>p.Select(kv=>kv[0])).Distinct().ToList();
  if(keys.Count>0&&keys.Count<=11){var headers=new[]{"ID","Description"}.Concat(keys).ToArray();
   rep.Table(headers,r.Rows.Select((row,k)=>new[]{row[0],row[1]}.Concat(keys.Select(key=>{var kv=parsed[k].FirstOrDefault(x=>x[0]==key);return kv==null?"":kv[1];})).ToArray()),new[]{.8f,1.6f}.Concat(keys.Select(key=>1f)).ToArray(),Tint);}
  else rep.Table(new[]{"ID","Description","Inputs"},r.Rows.Select((row,k)=>new[]{row[0],row[1],string.Join("\n",parsed[k].Select(kv=>kv[0]+" = "+kv[1]))}),new[]{.8f,1.6f,4.4f},Tint);}
 static string Title(EngineeringReport report){string sub=Regex.Replace(report.Subtitle??"",@"\s*\(.*\)\s*$","").Replace(" · "," - ");return string.IsNullOrWhiteSpace(sub)?report.Title:sub;}
 internal static CalculationDocument Build(EngineeringReport report){var rep=new RichReport(Title(report));rep.Compact=true;
  var cover=new List<string[]>();if(!string.IsNullOrWhiteSpace(report.Project))cover.Add(new[]{"Project",report.Project});if(!string.IsNullOrWhiteSpace(report.Location))cover.Add(new[]{"Location",report.Location});if(!string.IsNullOrWhiteSpace(report.Office))cover.Add(new[]{"Implementing office",report.Office});
  cover.Add(new[]{"Report",report.Title+" - "+report.Subtitle});cover.Add(new[]{"Status",report.Status});cover.Add(new[]{"Prepared",report.Generated.ToString("dd MMMM yyyy, HH:mm")});foreach(var h in report.Highlights)cover.Add(new[]{h.Key,h.Value});
  rep.Heading("REPORT").KeyValues(cover);
  var all=report.Sections.Where(sx=>sx.Records.Count>0).ToList();
  var main=all.Where(sx=>!sx.Records.All(IsInputs)).ToList();var appendix=all.Where(sx=>sx.Records.All(IsInputs)).ToList();
  rep.Heading("CONTENTS");var contents=main.Select((sx,k)=>new[]{(k+1).ToString(Ci),sx.Title}).ToList();if(appendix.Count>0)contents.Add(new[]{"A","Measurement inputs ("+string.Join(", ",appendix.Select(sx=>sx.Title.Replace(" — measurement inputs","")))+")"});
  rep.Table(new[]{"No.","Section"},contents,new[]{.5f,5.3f});
  rep.Paragraph("How to read this report: each work section lists its quantities first (k.1), then the calculation of each quantity (k.2, k.3 …) as numbered lines - source / member, the calculation with its values, and the result - ending with the TOTAL that is carried to k.1 and to the Bill of Quantities. Source lines explain a total; they are not additional quantities. The measured inputs are in Appendix A.",true);
  for(int k=0;k<main.Count;k++){var sx=main[k];string no=(k+1).ToString(Ci);rep.Heading(no+"  "+sx.Title.ToUpperInvariant());
   /* number the calculations of this section first, so the quantities table can point to them */
   var where=new Dictionary<string,string>();int sub=sx.Records.Any(IsQuantities)?1:0;foreach(var r in sx.Records){var name=BreakdownName(r);if(name!=null&&!where.ContainsKey(name))where[name]=no+"."+(++sub).ToString(Ci);}
   foreach(var r in sx.Records){
    if(IsQuantities(r)){if(r.Rows.Count==0)continue;rep.Paragraph(no+".1  Quantities",false,true);int bi=Array.IndexOf(r.Headers,"Basis");
     var rows=r.Rows.Select(row=>{var copy=(string[])row.Clone();string w;if(bi>=0){where.TryGetValue(row[0]??"",out w);copy[bi]=Basis(copy[bi],w);}return copy;});rep.Table(r.Headers,rows,r.Headers.Select(Weight).ToArray(),Tint);}
    else if(BreakdownName(r)!=null){var name=BreakdownName(r);rep.Paragraph(where[name]+"  "+name+" - calculation",false,true);}
    else if(IsSources(r))Sources(rep,r);
    else if(IsInputs(r))Inputs(rep,r);
    else if(IsRebarRecord(r))RebarRecord(rep,r);
    else if(r.Kind=="table"&&r.Headers!=null&&r.Rows!=null)Plain(rep,r);
    else if(r.Kind=="note")rep.Paragraph(r.Text,true);
    else if(r.Kind=="equation")rep.Paragraph(r.Text,false,true);
    else if(!string.IsNullOrWhiteSpace(r.Text)){bool title=r.Text.Length<70&&r.Text==r.Text.ToUpperInvariant();rep.Paragraph(r.Text,!title,title);}}}
  if(appendix.Count>0){rep.Heading("A  MEASUREMENT INPUTS");int a=0;foreach(var sx in appendix){rep.Paragraph("A."+(++a).ToString(Ci)+"  "+sx.Title.Replace(" — measurement inputs",""),false,true);foreach(var r in sx.Records)Inputs(rep,r);}}
  if(main.Count==0&&appendix.Count==0)rep.Paragraph("No measured quantities have been entered for this scope.",true);
  rep.Signatures("Prepared by","Checked by");return rep.Build();}
}
}
