using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* STAAD load combinations for the superstructure (owner, 25 November 2026: "create load combinations for superstructure including
   orthogonal effect for staad input"; "LOAD ComBo No. Start 1000, 2000, 3000, 4000, 5000, 6000 per group"; "use standard naming same as
   input on staad"; "for Example Load Case 1 EQ+X Seismic-H").
   The primary load cases are pasted as in the STAAD input ("LOAD 1 LOADTYPE SEISMIC-H TITLE EQ+X") or the load case list ("1 EQ+X
   Seismic-H"); their type and title give D, L, Lr, W, Eh (X / Z, with or without a sign) and Ev. Combinations are titled with those
   STAAD titles ("1.2DL+1.2SDL+1.6LL+0.5LR").
     1000  LRFD gravity    NSCP 2015 203.3.1: 1.4D; 1.2D + 1.6L + 0.5Lr; 1.2D + 1.6Lr + f1L
     2000  LRFD wind       1.2D + 1.6Lr + 0.5W; 1.2D + 1.0W + f1L + 0.5Lr; 0.9D + 1.0W
     3000  LRFD seismic    1.2D + f1L + 1.0E; 0.9D + 1.0E, E = rho Eh + Ev (208.5.1.1), Ev = 0.5 Ca I D or the Seismic-V case
     4000  ASD gravity     203.4.1: D; D + L; D + Lr; D + 0.75L + 0.75Lr
     5000  ASD wind        D + 0.6W; D + 0.75L + 0.75(0.6W) + 0.75Lr; 0.6D + 0.6W
     6000  ASD seismic     D + E/1.4; D + 0.75L + 0.75(E/1.4); 0.6D + E/1.4
   Orthogonal effects: every seismic combination is written with 100 % of one horizontal direction plus 30 % of the perpendicular one,
   in every sign (8 cases); separate + / - load cases (EQ+X, EQ-X ...) are used with positive factors. */
/* Repeat: the terms of a REPEAT LOAD case (the orthogonal seismic cases of structure class C) */
sealed class StaadPrimary {public int No;public string Title,Kind,Axis="";public int Sign;public List<KeyValuePair<StaadPrimary,double>> Repeat;}
sealed class StaadCombo {public int No;public string Title,Group;public List<KeyValuePair<StaadPrimary,double>> Terms=new List<KeyValuePair<StaadPrimary,double>>();}
/* Structure: "A" regular and symmetrical (EQ +X, -X, +Z, -Z each alone), "B" regular and unsymmetrical (EQ +X +E ... with accidental
   torsion, each alone), "C" irregular and unsymmetrical (orthogonal effects: eight REPEAT LOAD cases 1.0(EQ +X) + 0.3(EQ +Z) ..., Load Type
   Seismic, built from the EQ cases of Load Type None) - the owner's STAAD notes. F1One: titles of the live cases with f1 = 1.0 (owner:
   "hiwalay yung LL1 at LL2 yung f1 niya"); other live cases f1 = 0.5. */
sealed class StaadCombinationSettings {public double Rho=1.0,Ca=0.44,I=1.0;public bool IncludeEv=true,Lrfd=true,Asd=true;public string Structure="C";
 /* drift checks (owner, 25 November 2026: "also for drift check wind and seismic"): 7000 wind drift D + 0.5L + kW (k = DriftWind, 0.6 =
    service-level wind of the NSCP 2015 ultimate wind speeds), 8000 seismic drift 1.0 Eh of every seismic case (Section 208.6.4: the
    design-level displacement of the design seismic forces, then dM = 0.7 R dS against 0.025h / 0.020h) */public bool Drift=true;public double DriftWind=0.6;public HashSet<string> F1One=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 public double F1(StaadPrimary p){return F1One.Contains(p.Title)||F1One.Contains(p.Title.Replace(" ",""))?1.0:0.5;}}
static class StaadCombinations {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 internal static string F(double v){return Math.Round(v,3).ToString("0.###",Ci);}
 /* one primary load case per line: STAAD input ("LOAD 5 LOADTYPE SEISMIC-H TITLE EQ+X") or the load case list ("5 EQ+X Seismic-H",
    "Load Case 5: EQ+X Seismic-H"); combinations ("LOAD COMB ...") and lines without a number are skipped */
 internal static List<StaadPrimary> Parse(string text){var list=new List<StaadPrimary>();
  foreach(var raw in (text??"").Split('\n')){string line=raw.Trim();if(line==""||line.StartsWith("*"))continue;if(Regex.IsMatch(line,@"\bCOMB",RegexOptions.IgnoreCase))continue;
   var m=Regex.Match(line,@"^(?:\d+\.\s+)?(?:LOAD\s+(?:CASE\s+)?)?(\d+)\s*[:.]?\s*(.*)$",RegexOptions.IgnoreCase);if(!m.Success)continue;int no=int.Parse(m.Groups[1].Value,Ci);string rest=m.Groups[2].Value.Trim();
   string type="";var t=Regex.Match(rest,@"\bLOADTYPE\s+(ROOF\s+LIVE|SEISMIC-H|SEISMIC-V|SEISMIC|DEAD|LIVE|WIND|[A-Z\-]+)",RegexOptions.IgnoreCase);
   if(t.Success){type=t.Groups[1].Value.ToUpperInvariant();rest=rest.Remove(t.Index,t.Length);}
   else{var t2=Regex.Match(rest,@"(?:^|\s)(ROOF\s+LIVE|SEISMIC-H|SEISMIC-V|SEISMIC|DEAD|LIVE|WIND)\s*$",RegexOptions.IgnoreCase);if(t2.Success){type=t2.Groups[1].Value.ToUpperInvariant();rest=rest.Remove(t2.Index,t2.Length);}}
   string title=Regex.Replace(rest,@"\bTITLE\b","",RegexOptions.IgnoreCase).Trim();if(title=="")title="LOAD"+no.ToString(Ci);
   var p=new StaadPrimary{No=no,Title=Regex.Replace(title,@"\s+"," ")};p.Kind=Kind(type,p.Title);
   string u=p.Title.ToUpperInvariant();
   if(p.Kind=="E"){var a=Regex.Match(u,@"([+\-]?)\s*([XZ])(?![A-Z])");if(a.Success){p.Axis=a.Groups[2].Value;p.Sign=a.Groups[1].Value=="+"?1:a.Groups[1].Value=="-"?-1:0;}}
   if(list.Any(q=>q.No==no))continue;list.Add(p);}
  return list;}
 static string Kind(string type,string title){string u=title.ToUpperInvariant();var words=Regex.Split(u,"[^A-Z0-9]+").Where(w=>w!="").ToList();
  if(type=="DEAD")return "D";if(type=="LIVE")return words.Any(w=>w=="LR"||w=="LLR"||w=="RLL"||w=="ROOF")?"Lr":"L";if(type.StartsWith("ROOF"))return "Lr";if(type=="WIND")return "W";if(type=="SEISMIC-V")return "Ev";if(type.StartsWith("SEISMIC"))return "E";
  /* no LOADTYPE: from the title */
  if(Regex.IsMatch(u,@"^(EQ|E|EX|EZ|SEISMIC|EARTHQUAKE|RSA|SPEC)")||words.Any(w=>w=="EQ"||w=="EQX"||w=="EQZ"||w=="SEISMIC"||w=="EARTHQUAKE"))return words.Any(w=>w=="EV"||w=="EQV"||w=="VERTICAL")?"Ev":"E";
  if(Regex.IsMatch(u,@"^(W|WL|WIND|WW|LW|SW)[\s+\-]*[XZ+\-]")||words.Any(w=>w=="WIND"||w=="WL"))return "W";
  if(words.Any(w=>w=="LR"||w=="LLR"||w=="RLL"||w=="ROOF"))return "Lr";if(words.Any(w=>w=="LL"||w=="LIVE"||w=="L"))return "L";
  if(words.Any(w=>w=="DL"||w=="DEAD"||w=="SDL"||w=="SIDL"||w=="SW"||w=="SELF"||w=="SELFWEIGHT"||w=="D"||w=="FINISHES"||w=="PARTITION"||w=="PARTITIONS"))return "D";
  return "Other";}
 /* the horizontal seismic pairs of the orthogonal rule: (main, factor, other, factor) */
 static List<List<KeyValuePair<StaadPrimary,double>>> Orthogonal(List<StaadPrimary> e){var list=new List<List<KeyValuePair<StaadPrimary,double>>>();
  var xs=e.Where(p=>p.Axis=="X").ToList();var zs=e.Where(p=>p.Axis=="Z").ToList();var none=e.Where(p=>p.Axis=="").ToList();
  Func<List<StaadPrimary>,List<KeyValuePair<StaadPrimary,double>>> signed=g=>{var r=new List<KeyValuePair<StaadPrimary,double>>();if(g.Any(p=>p.Sign!=0))foreach(var p in g)r.Add(new KeyValuePair<StaadPrimary,double>(p,1));else foreach(var p in g){r.Add(new KeyValuePair<StaadPrimary,double>(p,1));r.Add(new KeyValuePair<StaadPrimary,double>(p,-1));}return r;};
  var sx=signed(xs);var sz=signed(zs);
  if(sx.Count>0&&sz.Count>0){foreach(var a in sx)foreach(var b in sz)list.Add(new List<KeyValuePair<StaadPrimary,double>>{new KeyValuePair<StaadPrimary,double>(a.Key,a.Value),new KeyValuePair<StaadPrimary,double>(b.Key,0.3*b.Value)});
   /* the main direction first, as in the STAAD notes: 1.0(EQ +Z) + 0.3(EQ +X) */foreach(var b in sz)foreach(var a in sx)list.Add(new List<KeyValuePair<StaadPrimary,double>>{new KeyValuePair<StaadPrimary,double>(b.Key,b.Value),new KeyValuePair<StaadPrimary,double>(a.Key,0.3*a.Value)});}
  else foreach(var a in sx.Concat(sz))list.Add(new List<KeyValuePair<StaadPrimary,double>>{a});
  foreach(var a in signed(none))list.Add(new List<KeyValuePair<StaadPrimary,double>>{a});
  return list;}
 static string F1d(double v){return v.ToString("0.0##",Ci);}
 /* the REPEAT LOAD cases made for the orthogonal effects (class C) */
 internal static List<StaadPrimary> RepeatCases(List<StaadCombo> combos){return combos.SelectMany(c=>c.Terms.Select(t=>t.Key)).Where(p=>p.Repeat!=null).Distinct().OrderBy(p=>p.No).ToList();}
 internal static List<StaadCombo> Build(List<StaadPrimary> cases,StaadCombinationSettings s){
  var d=cases.Where(p=>p.Kind=="D").ToList();var l=cases.Where(p=>p.Kind=="L").ToList();var lr=cases.Where(p=>p.Kind=="Lr").ToList();var w=cases.Where(p=>p.Kind=="W").ToList();var ev=cases.Where(p=>p.Kind=="Ev").ToList();
  if(d.Count==0)throw new Exception("No dead load case found: give the dead load cases LOADTYPE DEAD (or titles DL, SDL, SW ...).");
  var eCases=cases.Where(p=>p.Kind=="E").ToList();var eh=new List<List<KeyValuePair<StaadPrimary,double>>>();var list=new List<StaadCombo>();
  if(s.Structure=="C"&&eCases.Any(p=>p.Axis=="X")&&eCases.Any(p=>p.Axis=="Z")){int no=cases.Count==0?0:cases.Max(p=>p.No);
   foreach(var set in Orthogonal(eCases)){var r=new StaadPrimary{No=++no,Kind="E",Repeat=set,Title=string.Join("",set.Select((t,k)=>(t.Value<0?"-":k>0?"+":"")+F1d(Math.Abs(t.Value))+"("+t.Key.Title+")"))};eh.Add(new List<KeyValuePair<StaadPrimary,double>>{new KeyValuePair<StaadPrimary,double>(r,1)});}}
  else foreach(var p in eCases){if(p.Sign!=0||eCases.Any(q=>q.Sign!=0))eh.Add(new List<KeyValuePair<StaadPrimary,double>>{new KeyValuePair<StaadPrimary,double>(p,1)});else{eh.Add(new List<KeyValuePair<StaadPrimary,double>>{new KeyValuePair<StaadPrimary,double>(p,1)});eh.Add(new List<KeyValuePair<StaadPrimary,double>>{new KeyValuePair<StaadPrimary,double>(p,-1)});}}
  double ev0=s.IncludeEv&&ev.Count==0?0.5*s.Ca*s.I:0;/* Ev = 0.5 Ca I D added to the dead load factor when there is no Seismic-V case */
  int[] next={1000,2000,3000,4000,5000,6000,7000,8000};
  Action<int,string,Action<StaadCombo>> add=(g,group,fill)=>{var c=new StaadCombo{Group=group};fill(c);var terms=c.Terms.GroupBy(t=>t.Key).Select(x=>new KeyValuePair<StaadPrimary,double>(x.Key,x.Sum(y=>y.Value))).Where(t=>Math.Abs(t.Value)>1e-9).ToList();c.Terms=terms;if(terms.Count==0)return;
   string key=string.Join(" ",terms.OrderBy(t=>t.Key.No).Select(t=>t.Key.No+":"+F(t.Value)));if(list.Any(o=>o.Group==group&&string.Join(" ",o.Terms.OrderBy(t=>t.Key.No).Select(t=>t.Key.No+":"+F(t.Value)))==key))return;
   c.No=++next[g];c.Title=Title(terms);list.Add(c);};
  Action<StaadCombo,IEnumerable<StaadPrimary>,double> put=(c,g,f)=>{if(Math.Abs(f)<1e-12)return;foreach(var p in g)c.Terms.Add(new KeyValuePair<StaadPrimary,double>(p,f));};
  Action<StaadCombo,double> putL=(c,k)=>{foreach(var p in l)c.Terms.Add(new KeyValuePair<StaadPrimary,double>(p,k*s.F1(p)));};
  Action<StaadCombo,List<KeyValuePair<StaadPrimary,double>>,double> putE=(c,e,f)=>{foreach(var t in e)c.Terms.Add(new KeyValuePair<StaadPrimary,double>(t.Key,f*s.Rho*t.Value));};
  if(s.Lrfd){
   add(0,"LRFD gravity",c=>put(c,d,1.4));
   add(0,"LRFD gravity",c=>{put(c,d,1.2);put(c,l,1.6);put(c,lr,0.5);});
   if(lr.Count>0)add(0,"LRFD gravity",c=>{put(c,d,1.2);put(c,lr,1.6);putL(c,1.0);});
   foreach(var wi in w){var one=new[]{wi};
    add(1,"LRFD wind",c=>{put(c,d,1.2);put(c,lr,1.6);put(c,one,0.5);});
    add(1,"LRFD wind",c=>{put(c,d,1.2);put(c,one,1.0);putL(c,1.0);put(c,lr,0.5);});
    add(1,"LRFD wind",c=>{put(c,d,0.9);put(c,one,1.0);});}
   foreach(var e in eh){add(2,"LRFD seismic",c=>{put(c,d,1.2+ev0);putL(c,1.0);putE(c,e,1.0);put(c,ev,1.0);});
    add(2,"LRFD seismic",c=>{put(c,d,0.9-ev0);putE(c,e,1.0);put(c,ev,-1.0);});}}
  if(s.Asd){
   add(3,"ASD gravity",c=>put(c,d,1.0));
   if(l.Count>0)add(3,"ASD gravity",c=>{put(c,d,1.0);put(c,l,1.0);});
   if(lr.Count>0)add(3,"ASD gravity",c=>{put(c,d,1.0);put(c,lr,1.0);});
   if(l.Count>0&&lr.Count>0)add(3,"ASD gravity",c=>{put(c,d,1.0);put(c,l,0.75);put(c,lr,0.75);});
   foreach(var wi in w){var one=new[]{wi};
    add(4,"ASD wind",c=>{put(c,d,1.0);put(c,one,0.6);});
    add(4,"ASD wind",c=>{put(c,d,1.0);put(c,l,0.75);put(c,one,0.45);put(c,lr,0.75);});
    add(4,"ASD wind",c=>{put(c,d,0.6);put(c,one,0.6);});}
   foreach(var e in eh){add(5,"ASD seismic",c=>{put(c,d,1.0+ev0/1.4);putE(c,e,1/1.4);put(c,ev,1/1.4);});
    add(5,"ASD seismic",c=>{put(c,d,1.0+0.75*ev0/1.4);put(c,l,0.75);putE(c,e,0.75/1.4);put(c,ev,0.75/1.4);});
    add(5,"ASD seismic",c=>{put(c,d,0.6-ev0/1.4);putE(c,e,1/1.4);put(c,ev,-1/1.4);});}}
  if(s.Drift){foreach(var wi in w){var one=new[]{wi};add(6,"Wind drift",c=>{put(c,d,1.0);put(c,l,0.5);put(c,one,s.DriftWind);});}
   foreach(var e in eh)add(7,"Seismic drift",c=>{foreach(var t in e)c.Terms.Add(new KeyValuePair<StaadPrimary,double>(t.Key,t.Value));});}
  return list;}
 /* "1.2DL+1.2SDL+1.6LL+0.5LR", "0.9DL-1.0EQ+X-0.3EQ+Z" with the STAAD titles */
 static string Title(List<KeyValuePair<StaadPrimary,double>> terms){var b=new StringBuilder();foreach(var t in terms.OrderBy(t=>Order(t.Key.Kind)).ThenBy(t=>t.Key.No)){string f=F(Math.Abs(t.Value));b.Append(t.Value<0?"-":b.Length>0?"+":"").Append(f=="1"?"":f).Append(t.Key.Repeat!=null&&f!="1"?"["+t.Key.Title.Replace(" ","")+"]":t.Key.Title.Replace(" ",""));}return b.ToString();}
 static int Order(string k){return k=="D"?0:k=="L"?1:k=="Lr"?2:k=="W"?3:k=="E"?4:5;}
 internal static string Staad(List<StaadCombo> combos,List<StaadPrimary> cases,StaadCombinationSettings s){var b=new StringBuilder();
  b.Append("* ---- LOAD COMBINATIONS, NSCP 2015 (superstructure) ----");
  b.Append("\n* 1000 LRFD gravity, 2000 LRFD wind, 3000 LRFD seismic, 4000 ASD gravity,");
  b.Append("\n* 5000 ASD wind, 6000 ASD seismic, 7000 wind drift, 8000 seismic drift. f1 = 1.0 for ").Append(cases.Any(p=>p.Kind=="L"&&s.F1(p)==1)?string.Join(", ",cases.Where(p=>p.Kind=="L"&&s.F1(p)==1).Select(p=>p.Title)):"no live case").Append(", 0.5 for the others; rho = ").Append(F(s.Rho)).Append(cases.Any(p=>p.Kind=="Ev")?", Ev = the Seismic-V case":s.IncludeEv?", Ev = 0.5 Ca I D = "+F(0.5*s.Ca*s.I)+" D (in the D factor)":", Ev not included").Append(".");
  if(s.Drift)b.Append("\n* Drift: wind D + 0.5L + ").Append(F(s.DriftWind)).Append("W; seismic 1.0 Eh (dS, Section 208.6.4; dM = 0.7 R dS).");
  b.Append(s.Structure=="C"?"\n* Seismic: irregular and unsymmetrical - orthogonal effects, 100 % of one direction + 30 % of the other.":s.Structure=="B"?"\n* Seismic: regular and unsymmetrical - each EQ case with its accidental torsion (+E / -E).":"\n* Seismic: regular and symmetrical - each EQ direction alone.");
  var rpt=RepeatCases(combos);if(rpt.Count>0){b.Append("\n* ---- ORTHOGONAL SEISMIC LOAD CASES (paste after the primary loads; the EQ cases are Load Type None) ----");
   foreach(var r in rpt){b.Append("\nLOAD ").Append(r.No).Append(" LOADTYPE SEISMIC TITLE ").Append(r.Title).Append("\nREPEAT LOAD\n").Append(string.Join(" ",r.Repeat.Select(t=>t.Key.No.ToString(Ci)+" "+F(t.Value))));}}
  string group="";foreach(var c in combos){if(c.Group!=group){group=c.Group;b.Append("\n* ---- ").Append(group.ToUpperInvariant()).Append(" ----");}
   b.Append("\nLOAD COMBINATION ").Append(c.No).Append(" ").Append(c.Title);
   b.Append("\n").Append(string.Join(" ",c.Terms.OrderBy(t=>t.Key.No).Select(t=>t.Key.No.ToString(Ci)+" "+F(t.Value))));}
  return StaadLines.Wrap(b.ToString());}
 /* the owner's STAAD titles: DL, LL, LLR, EQ+X ..., WL+X ... (Seismic-H load type) */
 internal const string Example="LOAD 1 LOADTYPE DEAD TITLE DL\nLOAD 2 LOADTYPE LIVE TITLE LL1\nLOAD 3 LOADTYPE LIVE TITLE LL2\nLOAD 4 LOADTYPE ROOF LIVE TITLE LLR\nLOAD 5 LOADTYPE SEISMIC-H TITLE EQ+X\nLOAD 6 LOADTYPE SEISMIC-H TITLE EQ-X\nLOAD 7 LOADTYPE SEISMIC-H TITLE EQ+Z\nLOAD 8 LOADTYPE SEISMIC-H TITLE EQ-Z\nLOAD 9 LOADTYPE WIND TITLE WL+X\nLOAD 10 LOADTYPE WIND TITLE WL-X\nLOAD 11 LOADTYPE WIND TITLE WL+Z\nLOAD 12 LOADTYPE WIND TITLE WL-Z";
}
}
