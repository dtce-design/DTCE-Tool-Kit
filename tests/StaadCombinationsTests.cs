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
  /* one group each (owner: "1 group each for LRFD, BASIC ASD, ALTERNATIVE BASIC ASD, SPECIAL SEISMIC, WIND DRIFT, EQ DRIFT"), in code order */
  var lrfd=all.Where(c=>c.Group=="LRFD (203.3.1)").ToList();
  Check(lrfd[0].No==1001&&lrfd[0].Title=="1.4DL"&&lrfd[0].Eq=="203-1"&&lrfd[1].Title=="1.2DL+1.6LL1+1.6LL2+0.5LLR"&&lrfd[1].Eq=="203-2","LRFD 1001 203-1, 1002 203-2: "+lrfd[1].Title);
  Check(lrfd[2].Title=="1.2DL+0.5LL1+LL2+1.6LLR"&&lrfd[2].Eq=="203-3","f1 per live case: LL1 0.5, LL2 1.0: "+lrfd[2].Title);
  /* 203-1, 203-2, 203-3 (1 + 4 wind), 203-4 (4), 203-5 (8), 203-6 (4), 203-7 (8) */
  Check(lrfd.Count==1+1+5+4+8+4+8&&lrfd.Last().Eq=="203-7"&&lrfd.Count(c=>c.Eq=="203-5")==8,"LRFD count and order: "+lrfd.Count);
  var rpt=StaadCombinations.RepeatCases(all);Check(rpt.Count==8&&rpt[0].No==13&&rpt[0].Title=="1.0(EQ+X)+0.3(EQ+Z)"&&rpt.Any(r=>r.Title=="1.0(EQ-Z)+0.3(EQ-X)")&&rpt[4].Title=="1.0(EQ+Z)+0.3(EQ+X)","orthogonal load cases as in the STAAD notes: "+string.Join(" | ",rpt.Select(r=>r.No+" "+r.Title)));
  var e5=lrfd.First(c=>c.Eq=="203-5");double ev=0.5*0.44;Check(Math.Abs(e5.Terms.First(t=>t.Key.Title=="DL").Value-(1.2+ev))<1e-9&&e5.Terms.Any(t=>t.Key.Repeat!=null&&Math.Abs(t.Value-1)<1e-9)&&Math.Abs(e5.Terms.First(t=>t.Key.Title=="LL1").Value-0.5)<1e-9,"203-5: 1.2D + Ev + f1L + 1.0E");
  var e7=lrfd.First(c=>c.Eq=="203-7");Check(Math.Abs(e7.Terms.First(t=>t.Key.Title=="DL").Value-(0.9-ev))<1e-9&&!e7.Terms.Any(t=>t.Key.Kind=="L"),"203-7: 0.9D - Ev + E");
  var basic=all.Where(c=>c.Group=="Basic ASD (203.4.1)").ToList();
  Check(basic[0].No==2001&&basic[0].Eq=="203-8"&&basic.Count==1+1+1+1+(4+8)+(4+8)+4+8,"basic ASD 2001..., 203-8 to 203-15: "+basic.Count);
  var b13=basic.First(c=>c.Eq=="203-13"&&c.Terms.Any(t=>t.Key.Kind=="E"));Check(b13.Terms.Any(t=>t.Key.Kind=="Lr"&&Math.Abs(t.Value-0.75)<1e-9)&&b13.Terms.Any(t=>t.Key.Repeat!=null&&Math.Abs(t.Value-0.75/1.4)<1e-9),"203-13 with E: D + 0.75[L + Lr + E/1.4]");
  var alt=all.Where(c=>c.Group=="Alternative basic ASD (203.4.2)").ToList();
  Check(alt[0].No==3001&&alt[0].Eq=="203-16"&&alt[0].Title=="DL+LL1+LL2+LLR"&&alt.Count==1+4+8&&alt.Where(c=>c.Eq=="203-17").All(c=>c.Terms.Any(t=>t.Key.Kind=="W"&&Math.Abs(t.Value-0.6)<1e-9)),"alternative ASD 3001...: 203-16, 203-17 D + L + 0.6W, 203-18");
  var sp=all.Where(c=>c.Group=="Special seismic (203.5)").ToList();Check(sp.Count==16&&sp[0].No==4001&&sp[0].Terms.Any(t=>t.Key.Repeat!=null&&Math.Abs(t.Value-2.8)<1e-9),"special seismic 4001...: Omega0 Eh");
  var wd=all.Where(c=>c.Group=="Wind drift").ToList();var sd=all.Where(c=>c.Group=="Seismic drift").ToList();
  Check(wd.Count==4&&wd[0].No==5001&&Math.Abs(wd[0].Terms.First(t=>t.Key.Kind=="W").Value-0.6)<1e-9&&sd.Count==8&&sd[0].No==6001&&sd.All(c=>c.Terms.Count==1&&Math.Abs(c.Terms[0].Value-1)<1e-9),"wind drift 5001..., seismic drift 6001...");
  /* class A: each EQ case alone; class B: EQ +X +E ... each alone; Load Type None EQ cases still read as seismic */
  var a=new StaadCombinationSettings{Structure="A"};var allA=StaadCombinations.Build(list,a);Check(allA.Count(c=>c.Group=="LRFD (203.3.1)"&&(c.Eq=="203-5"||c.Eq=="203-7"))==8&&StaadCombinations.RepeatCases(allA).Count==0,"class A: 4 EQ cases x 2");
  var bList=StaadCombinations.Parse("1 DL Dead\n2 EQ +X +E Seismic\n3 EQ +X -E Seismic\n4 EQ -X +E Seismic\n5 EQ -X -E Seismic\n6 EQ +Z +E Seismic\n7 EQ +Z -E Seismic\n8 EQ -Z +E Seismic\n9 EQ -Z -E Seismic");
  var allB=StaadCombinations.Build(bList,new StaadCombinationSettings{Structure="B"});Check(bList.Count(p=>p.Kind=="E")==8&&allB.Count(c=>c.Group=="LRFD (203.3.1)"&&(c.Eq=="203-5"||c.Eq=="203-7"))==16&&bList[1].Axis=="X"&&bList[1].Sign==1,"class B: 8 EQ cases with accidental torsion, each alone");
  var none=StaadCombinations.Parse("LOAD 5 LOADTYPE NONE TITLE EQ+X");Check(none[0].Kind=="E"&&none[0].Axis=="X","Load Type None EQ case (class C primaries)");
  var asdE=basic.First(c=>c.Eq=="203-12"&&c.Terms.Any(t=>t.Key.Kind=="E"));Check(asdE.Terms.Any(t=>t.Key.Repeat!=null&&Math.Abs(t.Value-1/1.4)<1e-9),"ASD E / 1.4");
  var rr=StaadCombinations.Build(StaadCombinations.Parse("1 DL Dead\n2 EQ Seismic-H"),new StaadCombinationSettings{Rho=1.3,IncludeEv=false,Asd=false});var e1=rr.Where(c=>c.Eq=="203-5"||c.Eq=="203-7").ToList();
  Check(e1.Count==4&&e1.Any(c=>c.Terms.Any(t=>t.Key.Title=="EQ"&&Math.Abs(t.Value+1.3)<1e-9)),"one E case: +E and -E, times rho 1.3");
  var text=StaadCombinations.Staad(all,list,s);Check(text.Contains("LOAD COMBINATION 1001 1.4DL")&&text.Contains("LOAD 13 LOADTYPE SEISMIC TITLE 1.0(EQ+X)+0.3(EQ+Z)\nREPEAT LOAD\n5 1 7 0.3")&&text.Contains("LOAD COMBINATION 3001 ")&&text.Contains("* Eq. 203-5")&&text.Split('\n').All(x=>x.TrimEnd('\r').Length<=72),"STAAD text: REPEAT LOAD cases, combinations, lines within 72 columns");
  Check(ProgramAccess.ForSheet("Load-Combinations")==ProgramAccess.LoadCombinations&&(ProgramAccess.All&ProgramAccess.LoadCombinations)!=0&&ProgramAccess.Names.Contains("Load Combinations (NSCP 2015)"),"own access program");
  try{StaadCombinations.Build(StaadCombinations.Parse("1 LL"),s);Check(false,"no dead load must be refused");}catch(Exception ex){Check(ex.Message.Contains("dead"),"no dead load case: "+ex.Message);}
  Console.WriteLine("PASS: "+checks+" load combination checks");}
}
