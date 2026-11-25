using System;using System.Linq;using BeamScheduleFormatter;
/* Load Combinations (NSCP 2015): parsing of the STAAD primary loads, numbering by group, factors, orthogonal seismic sets, titles */
class StaadCombinationsTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static void Main(){
  var list=StaadCombinations.Parse(StaadCombinations.Example);
  Check(list.Count==12&&list[0].Kind=="D"&&list[1].Kind=="L"&&list[2].Kind=="L"&&list[3].Kind=="Lr"&&list.Count(p=>p.Kind=="E")==4&&list.Count(p=>p.Kind=="W")==4,"example: DL, LL1, LL2, LLR, 4 EQ (Seismic-H), 4 WL: "+string.Join(",",list.Select(p=>p.Title+"="+p.Kind)));
  var eqx=list.First(p=>p.Title=="EQ+X");var eqzm=list.First(p=>p.Title=="EQ-Z");Check(eqx.Axis=="X"&&eqx.Sign==1&&eqzm.Axis=="Z"&&eqzm.Sign==-1,"EQ+X / EQ-Z axis and sign");
  var lc=StaadCombinations.Parse("1 EQ+X Seismic-H\nLoad Case 2: DL Dead\nLOAD COMB 100 X\n3 LL\n4 WL+Z");Check(lc.Count==4&&lc[0].Kind=="E"&&lc[0].Title=="EQ+X"&&lc[1].Kind=="D"&&lc[1].Title=="DL"&&lc[2].Kind=="L"&&lc[3].Kind=="W","load case list form (owner: 1 EQ+X Seismic-H): "+string.Join(",",lc.Select(p=>p.No+" "+p.Title+"="+p.Kind)));
  var s=new StaadCombinationSettings{Rho=1.0,Ca=0.44,I=1.0};s.F1One.Add("LL2");var all=StaadCombinations.Build(list,s);
  Check(all.Where(c=>c.Group=="LRFD gravity").Select(c=>c.No).SequenceEqual(new[]{1001,1002,1003}),"LRFD gravity 1001-1003");
  Check(all.First(c=>c.No==1001).Title=="1.4DL"&&all.First(c=>c.No==1002).Title=="1.2DL+1.6LL1+1.6LL2+0.5LLR","titles with the STAAD names: "+all.First(c=>c.No==1002).Title);
  Check(all.First(c=>c.No==1003).Title=="1.2DL+0.5LL1+LL2+1.6LLR","f1 per live case: LL1 0.5, LL2 1.0 (owner: hiwalay yung LL1 at LL2): "+all.First(c=>c.No==1003).Title);
  Check(all.Count(c=>c.Group=="LRFD wind")==12&&all.Where(c=>c.Group=="LRFD wind").Min(c=>c.No)==2001,"3 LRFD wind combinations per wind case, from 2001");
  /* class C: eight REPEAT LOAD cases 1.0(EQ+X)+0.3(EQ+Z) ... numbered after the primary loads, each in 2 LRFD and 3 ASD combinations */
  var rpt=StaadCombinations.RepeatCases(all);Check(rpt.Count==8&&rpt[0].No==13&&rpt[0].Title=="1.0(EQ+X)+0.3(EQ+Z)"&&rpt.Any(r=>r.Title=="1.0(EQ-Z)+0.3(EQ-X)")&&rpt[4].Title=="1.0(EQ+Z)+0.3(EQ+X)","orthogonal load cases as in the STAAD notes: "+string.Join(" | ",rpt.Select(r=>r.No+" "+r.Title)));
  var lrfdE=all.Where(c=>c.Group=="LRFD seismic").ToList();Check(lrfdE.Count==16&&lrfdE[0].No==3001,"16 LRFD seismic combinations (8 cases x 2): "+lrfdE.Count);
  var first=lrfdE[0];double ev=0.5*0.44;Check(Math.Abs(first.Terms.First(t=>t.Key.Title=="DL").Value-(1.2+ev))<1e-9&&first.Terms.Any(t=>t.Key.Repeat!=null&&Math.Abs(t.Value-1)<1e-9)&&Math.Abs(first.Terms.First(t=>t.Key.Title=="LL1").Value-0.5)<1e-9,"1.2D + Ev + f1 L + 1.0 (orthogonal case)");
  var low=lrfdE[1];Check(Math.Abs(low.Terms.First(t=>t.Key.Title=="DL").Value-(0.9-ev))<1e-9&&!low.Terms.Any(t=>t.Key.Kind=="L"),"0.9D - Ev, no live load");
  Check(all.Where(c=>c.Group=="ASD gravity").Select(c=>c.No).SequenceEqual(new[]{4001,4002,4003,4004})&&all.Count(c=>c.Group=="ASD wind")==12&&all.Where(c=>c.Group=="ASD wind").Min(c=>c.No)==5001&&all.Count(c=>c.Group=="ASD seismic")==24&&all.Where(c=>c.Group=="ASD seismic").Min(c=>c.No)==6001,"ASD groups numbered 4000 / 5000 / 6000");
  /* class A: each EQ case alone; class B: EQ +X +E ... each alone; Load Type None EQ cases still read as seismic */
  var a=new StaadCombinationSettings{Structure="A"};var allA=StaadCombinations.Build(list,a);Check(allA.Count(c=>c.Group=="LRFD seismic")==8&&StaadCombinations.RepeatCases(allA).Count==0,"class A: 4 EQ cases x 2");
  var bList=StaadCombinations.Parse("1 DL Dead\n2 EQ +X +E Seismic\n3 EQ +X -E Seismic\n4 EQ -X +E Seismic\n5 EQ -X -E Seismic\n6 EQ +Z +E Seismic\n7 EQ +Z -E Seismic\n8 EQ -Z +E Seismic\n9 EQ -Z -E Seismic");
  var allB=StaadCombinations.Build(bList,new StaadCombinationSettings{Structure="B"});Check(bList.Count(p=>p.Kind=="E")==8&&allB.Count(c=>c.Group=="LRFD seismic")==16&&bList[1].Axis=="X"&&bList[1].Sign==1,"class B: 8 EQ cases with accidental torsion, each alone");
  var none=StaadCombinations.Parse("LOAD 5 LOADTYPE NONE TITLE EQ+X");Check(none[0].Kind=="E"&&none[0].Axis=="X","Load Type None EQ case (class C primaries)");
  /* drift: 7000 wind D + 0.5L + 0.6W per wind case, 8000 seismic 1.0 Eh per seismic case (owner: "also for drift check wind and seismic") */
  var wd=all.Where(c=>c.Group=="Wind drift").ToList();var sd=all.Where(c=>c.Group=="Seismic drift").ToList();
  Check(wd.Count==4&&wd[0].No==7001&&Math.Abs(wd[0].Terms.First(t=>t.Key.Kind=="W").Value-0.6)<1e-9&&Math.Abs(wd[0].Terms.First(t=>t.Key.Title=="LL1").Value-0.5)<1e-9,"wind drift 7001-7004: D + 0.5L + 0.6W");
  Check(sd.Count==8&&sd[0].No==8001&&sd.All(c=>c.Terms.Count==1&&Math.Abs(c.Terms[0].Value-1)<1e-9&&c.Terms[0].Key.Repeat!=null),"seismic drift 8001-8008: 1.0 of each orthogonal case, no gravity, no rho");
  var asdE=all.First(c=>c.No==6001);Check(asdE.Terms.Any(t=>t.Key.Repeat!=null&&Math.Abs(t.Value-1/1.4)<1e-9),"ASD E / 1.4");
  var rr=StaadCombinations.Build(StaadCombinations.Parse("1 DL Dead\n2 EQ Seismic-H"),new StaadCombinationSettings{Rho=1.3,IncludeEv=false,Asd=false});var e1=rr.Where(c=>c.Group=="LRFD seismic").ToList();
  Check(e1.Count==4&&e1.Any(c=>c.Terms.Any(t=>t.Key.Title=="EQ"&&Math.Abs(t.Value+1.3)<1e-9)),"one E case: +E and -E, times rho 1.3");
  var text=StaadCombinations.Staad(all,list,s);Check(text.Contains("LOAD COMBINATION 1001 1.4DL")&&text.Contains("LOAD 13 LOADTYPE SEISMIC TITLE 1.0(EQ+X)+0.3(EQ+Z)\nREPEAT LOAD\n5 1 7 0.3")&&text.Contains("LOAD COMBINATION 3001 ")&&text.Split('\n').All(x=>x.TrimEnd('\r').Length<=72),"STAAD text: REPEAT LOAD cases, combinations, lines within 72 columns");
  Check(ProgramAccess.ForSheet("Load-Combinations")==ProgramAccess.LoadCombinations&&(ProgramAccess.All&ProgramAccess.LoadCombinations)!=0&&ProgramAccess.Names.Contains("Load Combinations (NSCP 2015)"),"own access program");
  try{StaadCombinations.Build(StaadCombinations.Parse("1 LL"),s);Check(false,"no dead load must be refused");}catch(Exception ex){Check(ex.Message.Contains("dead"),"no dead load case: "+ex.Message);}
  Console.WriteLine("PASS: "+checks+" load combination checks");}
}
