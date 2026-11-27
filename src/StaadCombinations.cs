using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* STAAD load combinations for the superstructure (owner, 25 November 2026: "create load combinations for superstructure including
   orthogonal effect for staad input"; "LOAD ComBo No. Start 1000, 2000, 3000, 4000, 5000, 6000 per group"; "use standard naming same as
   input on staad"; "for Example Load Case 1 EQ+X Seismic-H").
   The primary load cases are pasted as in the STAAD input ("LOAD 1 LOADTYPE SEISMIC-H TITLE EQ+X") or the load case list ("1 EQ+X
   Seismic-H"); their type and title give D, L, Lr, W, Eh (X / Z, with or without a sign) and Ev. Combinations are titled with those
   STAAD titles ("1.2DL+1.2SDL+1.6LL+0.5LR").
     1000  Strength design (203.3.1)         Eq. 203-1 to 203-7
     2000  Alternate basic ASD (203.4.2)     Eq. 203-13 to 203-18 (one-third stress increase for W / E)
     3000  Special seismic (203.5)           Eq. 203-19 1.2D + f1L + 1.0Em, 203-20 0.9D +/- 1.0Em, Em = Omega0 Eh (208-19)
     4000  Basic ASD (203.4.1)               Eq. 203-8 to 203-12 (when chosen)
     5000  Wind drift, 6000 seismic drift
   E = rho Eh + Ev (208-18), Ev = 0.5 Ca I D (or the Seismic-V case) for strength design, zero for ASD unless chosen; ASD uses E/1.4.
   Orthogonal effects: every seismic combination is written with 100 % of one horizontal direction plus 30 % of the perpendicular one,
   in every sign (8 cases); separate + / - load cases (EQ+X, EQ-X ...) are used with positive factors. */
/* Repeat: the terms of a REPEAT LOAD case (the orthogonal seismic cases of structure class C) */
sealed class StaadPrimary {public int No;public string Title,Kind,Axis="";public int Sign;public List<KeyValuePair<StaadPrimary,double>> Repeat;}
sealed class StaadCombo {public int No;public string Title,Group,Eq="";public List<KeyValuePair<StaadPrimary,double>> Terms=new List<KeyValuePair<StaadPrimary,double>>();}
/* Structure: "A" regular and symmetrical (EQ +X, -X, +Z, -Z each alone), "B" regular and unsymmetrical (EQ +X +E ... with accidental
   torsion, each alone), "C" irregular and unsymmetrical (orthogonal effects: eight REPEAT LOAD cases 1.0(EQ +X) + 0.3(EQ +Z) ..., Load Type
   Seismic, built from the EQ cases of Load Type None) - the owner's STAAD notes. F1One: titles of the live cases with f1 = 1.0 (owner:
   "hiwalay yung LL1 at LL2 yung f1 niya"); other live cases f1 = 0.5. */
sealed class StaadCombinationSettings {/* automatic primary loads and their STAAD section (owner, 26 November 2026: "automatic na sana di na ako maglalagay ng primary loads"; "perform analysis change every eq primary case") */public bool AutoPrimary,PerformChange=false;public bool PrintReports=true;public string EqCommand="UBC LOAD";/* wind case text by direction of travel ("+X" ...) from the Wind Load sheet */public Dictionary<string,string> WindText=new Dictionary<string,string>();public double Acc=0.05,Ax=1.0;public double Rho=1.0,Ca=0.44,I=1.0;public bool IncludeEv=true,Lrfd=true,Asd=false,EvInAsd=false;public string Structure="C";
 /* drift checks (owner, 25 November 2026: "also for drift check wind and seismic"): 7000 wind drift D + 0.5L + kW (k = DriftWind, 0.6 =
    service-level wind of the NSCP 2015 ultimate wind speeds), 8000 seismic drift 1.0 Eh of every seismic case (Section 208.6.4: the
    design-level displacement of the design seismic forces, then dM = 0.7 R dS against 0.025h / 0.020h) */public bool Drift=true,DriftGravity=false;public double DriftWind=0.6;
 /* owner (25 November 2026: "diba may basic at alternative tapos may additional loads sa eq?"): 9000 the alternative basic ASD
    combinations (NSCP 2015 203.4.2: D + L + Lr; D + L + 0.6 omega W; D + L + E/1.4; 0.9D + E/1.4 - allowable stresses may then be
    increased by one-third for wind or seismic), 10000 the special seismic combinations (203.5: 1.2D + f1L + 1.0Em; 0.9D + 1.0Em,
    Em = Omega0 Eh, Section 208.5.1.2) */public bool AltAsd=true,Special=true;public double Omega0=2.8;public HashSet<string> F1One=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
  /* NSCP 2015 as printed (owner's pages, 25 November 2026): 1000 = 203.3.1 strength design (203-1 to 203-7), 2000 = 203.4.2 alternate basic
     ASD (203-13 to 203-18, one-third stress increase for W / E), 3000 = 203.5 special seismic (203-19, 203-20, Em = Omega0 Eh, 208-19),
     4000 = 203.4.1 basic ASD (203-8 to 203-12; off unless chosen - the owner uses the alternate set), 5000 wind drift, 6000 seismic drift.
     E = rho Eh + Ev (208-18); Ev = 0.5 Ca I D "for Strength Design, and may be taken as zero for Allowable Stress Design" - so no Ev in
     the ASD sets unless EvInAsd is chosen. */
  int[] next={1000,2000,3000,4000,5000,6000};string eqNow="";
  Action<int,string,Action<StaadCombo>> add=(g,group,fill)=>{var c=new StaadCombo{Group=group,Eq=eqNow};fill(c);var terms=c.Terms.GroupBy(t=>t.Key).Select(x=>new KeyValuePair<StaadPrimary,double>(x.Key,x.Sum(y=>y.Value))).Where(t=>Math.Abs(t.Value)>1e-9).ToList();c.Terms=terms;if(terms.Count==0)return;
   string key=string.Join(" ",terms.OrderBy(t=>t.Key.No).Select(t=>t.Key.No+":"+F(t.Value)));if(list.Any(o=>o.Group==group&&string.Join(" ",o.Terms.OrderBy(t=>t.Key.No).Select(t=>t.Key.No+":"+F(t.Value)))==key))return;
   c.No=++next[g];c.Title=Title(terms);list.Add(c);};
  Action<StaadCombo,IEnumerable<StaadPrimary>,double> put=(c,g,f)=>{if(Math.Abs(f)<1e-12)return;foreach(var p in g)c.Terms.Add(new KeyValuePair<StaadPrimary,double>(p,f));};
  Action<StaadCombo,double> putL=(c,k)=>{foreach(var p in l)c.Terms.Add(new KeyValuePair<StaadPrimary,double>(p,k*s.F1(p)));};
  Action<StaadCombo,List<KeyValuePair<StaadPrimary,double>>,double> putE=(c,e,f)=>{foreach(var t in e)c.Terms.Add(new KeyValuePair<StaadPrimary,double>(t.Key,f*s.Rho*t.Value));};
  double eva=s.EvInAsd?ev0:0;var evAsd=s.EvInAsd?ev:new List<StaadPrimary>();
  const string LRFD="Strength design (203.3.1)",ALT="Alternate basic ASD (203.4.2)",SPECIAL="Special seismic (203.5)",BASIC="Basic ASD (203.4.1)",WD="Wind drift",ED="Seismic drift";
  if(s.Lrfd){
   eqNow="203-1";add(0,LRFD,c=>put(c,d,1.4));
   eqNow="203-2";add(0,LRFD,c=>{put(c,d,1.2);put(c,l,1.6);put(c,lr,0.5);});
   eqNow="203-3";if(lr.Count>0)add(0,LRFD,c=>{put(c,d,1.2);put(c,lr,1.6);putL(c,1.0);});foreach(var wi in w){var one=new[]{wi};add(0,LRFD,c=>{put(c,d,1.2);put(c,lr,1.6);put(c,one,0.5);});}
   eqNow="203-4";foreach(var wi in w){var one=new[]{wi};add(0,LRFD,c=>{put(c,d,1.2);put(c,one,1.0);putL(c,1.0);put(c,lr,0.5);});}
   eqNow="203-5";foreach(var e in eh)add(0,LRFD,c=>{put(c,d,1.2+ev0);putL(c,1.0);putE(c,e,1.0);put(c,ev,1.0);});
   eqNow="203-6";foreach(var wi in w){var one=new[]{wi};add(0,LRFD,c=>{put(c,d,0.9);put(c,one,1.0);});}
   eqNow="203-7";foreach(var e in eh)add(0,LRFD,c=>{put(c,d,0.9-ev0);putE(c,e,1.0);put(c,ev,-1.0);});}
  if(s.AltAsd){
   eqNow="203-13";foreach(var wi in w){var one=new[]{wi};add(1,ALT,c=>{put(c,d,1.0);put(c,l,0.75);put(c,lr,0.75);put(c,one,0.45);});}foreach(var e in eh)add(1,ALT,c=>{put(c,d,1.0+0.75*eva/1.4);put(c,l,0.75);put(c,lr,0.75);putE(c,e,0.75/1.4);put(c,evAsd,0.75/1.4);});
   eqNow="203-14";foreach(var wi in w){var one=new[]{wi};add(1,ALT,c=>{put(c,d,0.6);put(c,one,0.6);});}
   eqNow="203-15";foreach(var e in eh)add(1,ALT,c=>{put(c,d,0.6-eva/1.4);putE(c,e,1/1.4);put(c,evAsd,-1/1.4);});
   eqNow="203-16";add(1,ALT,c=>{put(c,d,1.0);put(c,l,1.0);put(c,lr,1.0);});
   eqNow="203-17";foreach(var wi in w){var one=new[]{wi};add(1,ALT,c=>{put(c,d,1.0);put(c,l,1.0);put(c,one,0.6);});}
   eqNow="203-18";foreach(var e in eh)add(1,ALT,c=>{put(c,d,1.0+eva/1.4);put(c,l,1.0);putE(c,e,1/1.4);put(c,evAsd,1/1.4);});}
  if(s.Special){eqNow="203-19";foreach(var e in eh)add(2,SPECIAL,c=>{put(c,d,1.2);putL(c,1.0);foreach(var t in e)c.Terms.Add(new KeyValuePair<StaadPrimary,double>(t.Key,s.Omega0*t.Value));});
   eqNow="203-20";foreach(var e in eh)add(2,SPECIAL,c=>{put(c,d,0.9);foreach(var t in e)c.Terms.Add(new KeyValuePair<StaadPrimary,double>(t.Key,s.Omega0*t.Value));});}
  if(s.Asd){
   eqNow="203-8";add(3,BASIC,c=>put(c,d,1.0));
   eqNow="203-9";if(l.Count>0)add(3,BASIC,c=>{put(c,d,1.0);put(c,l,1.0);});
   eqNow="203-10";if(lr.Count>0)add(3,BASIC,c=>{put(c,d,1.0);put(c,lr,1.0);});
   eqNow="203-11";if(l.Count>0&&lr.Count>0)add(3,BASIC,c=>{put(c,d,1.0);put(c,l,0.75);put(c,lr,0.75);});
   eqNow="203-12";foreach(var wi in w){var one=new[]{wi};add(3,BASIC,c=>{put(c,d,1.0);put(c,one,0.6);});}foreach(var e in eh)add(3,BASIC,c=>{put(c,d,1.0+eva/1.4);putE(c,e,1/1.4);put(c,evAsd,1/1.4);});}
  if(s.Drift){eqNow="D + 0.5L + "+F(s.DriftWind)+"W";foreach(var wi in w){var one=new[]{wi};add(4,WD,c=>{put(c,d,1.0);put(c,l,0.5);put(c,one,s.DriftWind);});}
   /* with a P-Delta analysis the gravity loads must be in the drift case (owner: "sure ka na eq loads lang?") */eqNow=s.DriftGravity?"D + 0.5L + 1.0 Eh (208.6.4, P-Delta)":"1.0 Eh (208.6.4)";foreach(var e in eh)add(5,ED,c=>{if(s.DriftGravity){put(c,d,1.0);put(c,l,0.5);}foreach(var t in e)c.Terms.Add(new KeyValuePair<StaadPrimary,double>(t.Key,t.Value));});}
  return list;}
 /* "1.2DL+1.2SDL+1.6LL+0.5LR", "0.9DL-1.0EQ+X-0.3EQ+Z" with the STAAD titles */
 static string Title(List<KeyValuePair<StaadPrimary,double>> terms){var b=new StringBuilder();foreach(var t in terms.OrderBy(t=>Order(t.Key.Kind)).ThenBy(t=>t.Key.No)){string f=F(Math.Abs(t.Value));b.Append(t.Value<0?"-":b.Length>0?"+":"").Append(f=="1"?"":f).Append(t.Key.Repeat!=null&&f!="1"?"["+t.Key.Title.Replace(" ","")+"]":Paren(t.Key));}return b.ToString();}
 /* a title with + / - inside is bracketed, so 1.3(EQ+X+E) and +(WL-Z) read as one load */
 /* load numbers as STAAD lists: 1001 TO 1031, single numbers apart */
 static string Ranges(List<int> nos){var parts=new List<string>();for(int i=0;i<nos.Count;){int j=i;while(j+1<nos.Count&&nos[j+1]==nos[j]+1)j++;parts.Add(j>i?nos[i]+" TO "+nos[j]:nos[i].ToString(Ci));i=j+1;}return string.Join(" ",parts);}
 static string Paren(StaadPrimary p){string n=p.Title.Replace(" ","");return p.Repeat==null&&Regex.IsMatch(n,@".[+\-]")?"("+n+")":n;}
 static int Order(string k){return k=="D"?0:k=="L"?1:k=="Lr"?2:k=="W"?3:k=="E"?4:5;}
 /* the primary load cases of the structure class, earthquake cases first: A EQ +X -X +Z -Z; B the same with accidental torsion
    +E / -E (8 cases); C EQ +X -X +Z -Z with Load Type None (the orthogonal REPEAT LOAD cases are seismic); then DL, LL1..n, LLR, WL */
 internal static string AutoPrimary(string structure,int liveCases,bool roofLive,bool wind){return AutoPrimary(structure,liveCases,roofLive,wind,false,true);}
 internal static string AutoPrimary(string structure,int liveCases,bool roofLive,bool wind,bool allWind,bool torsion){return AutoPrimary(structure,liveCases,roofLive,wind,allWind,torsion,false);}
 /* with allWind the design wind load cases of NSCP 2015 Figure 207B.4-8 (directional procedure) follow Case 1: Case 2 WL2+X+T / WL2+X-T ...,
    Case 3 WL3+X+Z ..., Case 4 WL4+X+Z+T ...; without torsion (the code's exemption) Cases 2 and 4 are left out */
 /* with roofWind each Case 1 direction is two cases: WL+X with the roof uplift (+GCpi, Cp branch 1) and WL1B+X with the roof pressure
    of -GCpi / Cp branch 2 (NSCP 2015 Figure 207B.4-1: both GCpi signs and both Cp values) */
 internal static string AutoPrimary(string structure,int liveCases,bool roofLive,bool wind,bool allWind,bool torsion,bool roofWind){var lines=new List<string>();int n=0;
  string type=structure=="C"?"NONE":"SEISMIC-H";
  foreach(var d in new[]{"+X","-X","+Z","-Z"}){if(structure=="B"){lines.Add("LOAD "+(++n)+" LOADTYPE "+type+" TITLE EQ"+d+"+E");lines.Add("LOAD "+(++n)+" LOADTYPE "+type+" TITLE EQ"+d+"-E");}else lines.Add("LOAD "+(++n)+" LOADTYPE "+type+" TITLE EQ"+d);}
  lines.Add("LOAD "+(++n)+" LOADTYPE DEAD TITLE DL");for(int k=1;k<=Math.Max(1,liveCases);k++)lines.Add("LOAD "+(++n)+" LOADTYPE LIVE TITLE "+(liveCases<=1?"LL":"LL"+k));
  if(roofLive)lines.Add("LOAD "+(++n)+" LOADTYPE ROOF LIVE TITLE LLR");/* the case of NSCP 2015 Figure 207B.4-8 in the title (owner, 27 November 2026: "sa load cases ilagay mo if anong case yun") */
  if(wind)foreach(var d in new[]{"+X","-X","+Z","-Z"})lines.Add("LOAD "+(++n)+" LOADTYPE WIND TITLE WL"+d+(allWind||roofWind?" CASE 1":""));
  if(wind&&roofWind)foreach(var d in new[]{"+X","-X","+Z","-Z"})lines.Add("LOAD "+(++n)+" LOADTYPE WIND TITLE WL"+d+" CASE 1 -GCPI");
  if(wind&&allWind){if(torsion)foreach(var d in new[]{"+X","-X","+Z","-Z"})foreach(var t in new[]{"+T","-T"})lines.Add("LOAD "+(++n)+" LOADTYPE WIND TITLE WL"+d+" CASE 2 "+t);
   foreach(var x in new[]{"+X","-X"})foreach(var z in new[]{"+Z","-Z"})lines.Add("LOAD "+(++n)+" LOADTYPE WIND TITLE WL"+x+z+" CASE 3");
   if(torsion)foreach(var x in new[]{"+X","-X"})foreach(var z in new[]{"+Z","-Z"})foreach(var t in new[]{"+T","-T"})lines.Add("LOAD "+(++n)+" LOADTYPE WIND TITLE WL"+x+z+" CASE 4 "+t);}
  return string.Join("\n",lines);}
 /* the STAAD primary load section: each EQ case with its seismic load command and, as older STAAD needs, PERFORM ANALYSIS / CHANGE */
 /* "WL+X" -> "+X" */
 internal static string WindKey(string title){var k=System.Text.RegularExpressions.Regex.Match((title??"").Trim(),@"^WL((?:[+-][XZ]){1,2})\s+CASE\s+([1-4])(\s+-GCPI)?(?:\s+([+-])T)?$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
  if(k.Success){string dirs=k.Groups[1].Value.ToUpperInvariant(),c0=k.Groups[2].Value;if(c0=="1")return (k.Groups[3].Success?"1B":"")+dirs;return c0+dirs+(k.Groups[4].Success?k.Groups[4].Value+"T":"");}
  var c=System.Text.RegularExpressions.Regex.Match((title??"").Trim(),@"^WL([1234]B?)((?:[+-][XZ]){1,2})([+-]T)?$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
  if(c.Success)return c.Groups[1].Value+c.Groups[2].Value.ToUpperInvariant()+c.Groups[3].Value.ToUpperInvariant();var m=System.Text.RegularExpressions.Regex.Match(title??"",@"([+-])\s*([XZ])\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase);return m.Success?m.Groups[1].Value+m.Groups[2].Value.ToUpperInvariant():"";}
 internal static string PrimarySection(List<StaadPrimary> cases,StaadCombinationSettings s){var b=new StringBuilder("* ---- PRIMARY LOAD CASES (earthquake cases first) ----\n* Before them: DEFINE UBC LOAD (or the seismic definition of your STAAD)\n* with the zone / I / R / soil data and the seismic weights.");
  /* case B (owner: "apply also natural torsion kapag case b"): natural torsion from the centre of mass / centre of rigidity offset, accidental torsion through ACC = 0.05 Ax */
  if(s.Structure=="B"){b.Append("\n* Natural torsion: seismic weights at their true positions and\n* rigid floor diaphragms (FLOOR DIAPHRAGM ... TYPE RIG) put each\n* storey force at the centre of mass; its offset from the centre of\n* rigidity gives the natural torsion in the analysis.\n* Accidental torsion: ACC = 0.05 x Ax = ").Append(F(s.Acc*s.Ax)).Append(",\n* Ax = (dmax / 1.2 davg)^2, 1.0 to 3.0 (torsional irregularity).");}
  foreach(var p in cases){string type=p.Kind=="E"?(s.Structure=="C"?"NONE":"SEISMIC-H"):p.Kind=="D"?"DEAD":p.Kind=="L"?"LIVE":p.Kind=="Lr"?"ROOF LIVE":p.Kind=="W"?"WIND":p.Kind=="Ev"?"SEISMIC-V":"NONE";
   b.Append("\nLOAD ").Append(p.No).Append(" LOADTYPE ").Append(type).Append(" TITLE ").Append(p.Title);
   if(p.Kind=="E"){if(s.EqCommand!="None"&&p.Axis!=""){b.Append("\n").Append(s.EqCommand).Append(" ").Append(p.Axis).Append(" ").Append(p.Sign<0?"-1":"1");
      if(s.Structure=="B"){bool minus=System.Text.RegularExpressions.Regex.IsMatch(p.Title,@"-E$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);b.Append(" ACC ").Append(F(minus?-s.Acc*s.Ax:s.Acc*s.Ax));}}
     else b.Append("\n* add the seismic load of ").Append(p.Title).Append(" here");
     if(s.PerformChange)b.Append("\nPERFORM ANALYSIS\nCHANGE");}
   else if(p.Kind=="D")b.Append("\nSELFWEIGHT Y -1\n* add the floor / member dead loads here");
   else if(p.Kind=="W"&&WindKey(p.Title)!=""&&s.WindText.ContainsKey(WindKey(p.Title)))b.Append("\n").Append(s.WindText[WindKey(p.Title)]);
   else b.Append("\n* add the ").Append(p.Kind=="W"?"wind":p.Kind=="Lr"?"roof live":p.Kind=="L"?"live":"").Append(" loads here");}
  return b.ToString();}
 internal static string Staad(List<StaadCombo> combos,List<StaadPrimary> cases,StaadCombinationSettings s){var b=new StringBuilder();
  if(s.AutoPrimary){b.Append(PrimarySection(cases,s)).Append("\n");}
  b.Append("* ---- LOAD COMBINATIONS, NSCP 2015 (superstructure) ----");
  b.Append("\n* 1000 strength design (203.3.1), 2000 alternate basic ASD (203.4.2),");
  b.Append("\n* 3000 special seismic Em = Omega0 Eh (203.5), 4000 basic ASD (203.4.1),\n* 5000 wind drift, 6000 seismic drift.\n* f1 = 1.0 for ").Append(cases.Any(p=>p.Kind=="L"&&s.F1(p)==1)?string.Join(", ",cases.Where(p=>p.Kind=="L"&&s.F1(p)==1).Select(p=>p.Title)):"no live case").Append(", 0.5 for the others; rho = ").Append(F(s.Rho)).Append(cases.Any(p=>p.Kind=="Ev")?", Ev = the Seismic-V case":s.IncludeEv?", Ev = 0.5 Ca I D = "+F(0.5*s.Ca*s.I)+" D (in the D factor)":", Ev not included").Append(".");
  if(s.AltAsd)b.Append("\n* Alternate basic ASD: one-third increase in allowable stresses for W / E.");
  b.Append(s.EvInAsd?"\n* ASD: E = rho Eh + Ev.":"\n* ASD: E = rho Eh (Ev taken as zero for ASD, Section 208-18).");
  if(s.Special)b.Append("\n* Special seismic: Omega0 = ").Append(F(s.Omega0)).Append(", no rho / Ev in Em.");
  if(s.Drift)b.Append("\n* Drift: wind D + 0.5L + ").Append(F(s.DriftWind)).Append(s.DriftGravity?"W; seismic D + 0.5L + 1.0 Eh for P-Delta\n* (dS, Section 208.6.4; dM = 0.7 R dS).":"W; seismic 1.0 Eh (dS, Section 208.6.4; dM = 0.7 R dS).");
  b.Append(s.Structure=="C"?"\n* Seismic: irregular and unsymmetrical - orthogonal effects, 100 % of one direction + 30 % of the other.":s.Structure=="B"?"\n* Seismic: regular and unsymmetrical - natural torsion (centre of mass offset)\n* plus accidental torsion (+E / -E) in each EQ case.":"\n* Seismic: regular and symmetrical - each EQ direction alone.");
  var rpt=RepeatCases(combos);if(rpt.Count>0){b.Append("\n* ---- ORTHOGONAL SEISMIC LOAD CASES (paste after the primary loads; the EQ cases are Load Type None) ----");
   foreach(var r in rpt){b.Append("\nLOAD ").Append(r.No).Append(" LOADTYPE SEISMIC TITLE ").Append(r.Title).Append("\nREPEAT LOAD\n").Append(string.Join(" ",r.Repeat.Select(t=>t.Key.No.ToString(Ci)+" "+F(t.Value))));}}
  string group="";foreach(var c in combos){if(c.Group!=group){group=c.Group;b.Append("\n* ---- ").Append(group.ToUpperInvariant()).Append(" ----");}
   if(c.Eq!=""&&(combos.IndexOf(c)==0||combos[combos.IndexOf(c)-1].Eq!=c.Eq))b.Append("\n* ").Append(c.Eq.StartsWith("203-")?"Eq. "+c.Eq:c.Eq);
   b.Append("\nLOAD COMBINATION ").Append(c.No).Append(" ").Append(c.Title);
   b.Append("\n").Append(string.Join(" ",c.Terms.OrderBy(t=>t.Key.No).Select(t=>t.Key.No.ToString(Ci)+" "+F(t.Value))));}
  /* one envelope per group at the end (owner, 26 November 2026: "add define envelope at the end of load combo"): strength design and special
     seismic as STRENGTH, the ASD and drift groups as SERVICEABILITY */
  if(combos.Count>0){b.Append("\n* ---- ENVELOPES ----\nDEFINE ENVELOPE");int env=0;
   foreach(var g in combos.GroupBy(c=>c.Group)){var nos=g.Select(c=>c.No).OrderBy(x=>x).ToList();string type=g.Key.StartsWith("Strength")||g.Key.StartsWith("Special")?"STRENGTH":"SERVICEABILITY";
    b.Append("\n* ").Append(g.Key);b.Append("\n").Append(Ranges(nos)).Append(" ENVELOPE ").Append(++env).Append(" TYPE ").Append(type);}
   b.Append("\nEND DEFINE ENVELOPE");}
  /* analysis and the reports to print (owner: "include also the reports needed to print like print story drift, load list, print dia cr, print
     story stiffness etc."): the drift cases for the story drift, the EQ cases for the displacements (Ax) and member forces (rho) */
  if(s.PrintReports){var eqNos=cases.Where(p=>p.Kind=="E").Select(p=>p.No).OrderBy(x=>x).ToList();var drift=combos.Where(c=>c.Group=="Seismic drift").Select(c=>c.No).ToList();
   b.Append("\n* ---- ANALYSIS AND REPORTS ----\nPERFORM ANALYSIS PRINT LOAD DATA\n* centre of mass, centre of rigidity, story stiffness (soft story)\nPRINT CG\nPRINT DIAPHRAGM CR\nPRINT STORY STIFFNESS");
   if(drift.Count>0)b.Append("\n* story drift of the seismic drift cases (dS; dM = 0.7 R dS)\nLOAD LIST ").Append(Ranges(drift)).Append("\nPRINT STORY DRIFT");
   if(eqNos.Count>0)b.Append("\n* EQ cases: joint displacements (Ax) and member forces (rho)\nLOAD LIST ").Append(Ranges(eqNos)).Append("\nPRINT JOINT DISPLACEMENTS ALL\nPRINT MEMBER FORCES ALL");
   b.Append("\n* all cases: support reactions (foundations)\nLOAD LIST ALL\nPRINT SUPPORT REACTION ALL");}
  else if(s.AutoPrimary)b.Append("\n* Then PERFORM ANALYSIS once, after all loads and combinations.");
  return StaadLines.Wrap(b.ToString());}
 /* the owner's STAAD titles: DL, LL, LLR, EQ+X ..., WL+X ... (Seismic-H load type) */
 internal const string Example="LOAD 1 LOADTYPE DEAD TITLE DL\nLOAD 2 LOADTYPE LIVE TITLE LL1\nLOAD 3 LOADTYPE LIVE TITLE LL2\nLOAD 4 LOADTYPE ROOF LIVE TITLE LLR\nLOAD 5 LOADTYPE SEISMIC-H TITLE EQ+X\nLOAD 6 LOADTYPE SEISMIC-H TITLE EQ-X\nLOAD 7 LOADTYPE SEISMIC-H TITLE EQ+Z\nLOAD 8 LOADTYPE SEISMIC-H TITLE EQ-Z\nLOAD 9 LOADTYPE WIND TITLE WL+X\nLOAD 10 LOADTYPE WIND TITLE WL-X\nLOAD 11 LOADTYPE WIND TITLE WL+Z\nLOAD 12 LOADTYPE WIND TITLE WL-Z";
}
}
