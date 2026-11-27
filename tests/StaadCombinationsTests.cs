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
  var lrfd=all.Where(c=>c.Group=="Strength design (203.3.1)").ToList();
  Check(lrfd[0].No==1001&&lrfd[0].Title=="1.4DL"&&lrfd[0].Eq=="203-1"&&lrfd[1].Title=="1.2DL+1.6LL1+1.6LL2+0.5LLR"&&lrfd[1].Eq=="203-2","LRFD 1001 203-1, 1002 203-2: "+lrfd[1].Title);
  Check(lrfd[2].Title=="1.2DL+0.5LL1+LL2+1.6LLR"&&lrfd[2].Eq=="203-3","f1 per live case: LL1 0.5, LL2 1.0: "+lrfd[2].Title);
  /* 203-1, 203-2, 203-3 (1 + 4 wind), 203-4 (4), 203-5 (8), 203-6 (4), 203-7 (8) */
  Check(lrfd.Count==1+1+5+4+8+4+8&&lrfd.Last().Eq=="203-7"&&lrfd.Count(c=>c.Eq=="203-5")==8,"LRFD count and order: "+lrfd.Count);
  var rpt=StaadCombinations.RepeatCases(all);Check(rpt.Count==8&&rpt[0].No==13&&rpt[0].Title=="1.0(EQ+X)+0.3(EQ+Z)"&&rpt.Any(r=>r.Title=="1.0(EQ-Z)+0.3(EQ-X)")&&rpt[4].Title=="1.0(EQ+Z)+0.3(EQ+X)","orthogonal load cases as in the STAAD notes: "+string.Join(" | ",rpt.Select(r=>r.No+" "+r.Title)));
  var e5=lrfd.First(c=>c.Eq=="203-5");double ev=0.5*0.44;Check(Math.Abs(e5.Terms.First(t=>t.Key.Title=="DL").Value-(1.2+ev))<1e-9&&e5.Terms.Any(t=>t.Key.Repeat!=null&&Math.Abs(t.Value-1)<1e-9)&&Math.Abs(e5.Terms.First(t=>t.Key.Title=="LL1").Value-0.5)<1e-9,"203-5: 1.2D + Ev + f1L + 1.0E");
  var e7=lrfd.First(c=>c.Eq=="203-7");Check(Math.Abs(e7.Terms.First(t=>t.Key.Title=="DL").Value-(0.9-ev))<1e-9&&!e7.Terms.Any(t=>t.Key.Kind=="L"),"203-7: 0.9D - Ev + E");
  /* NSCP 2015 as printed: 2000 alternate basic ASD 203-13 to 203-18, 3000 special 203-19 / 203-20, basic ASD 203-8 to 203-12 (4000) only when chosen;
     Ev = 0 in ASD (208-18) */
  Check(!all.Any(c=>c.Group=="Basic ASD (203.4.1)"),"basic ASD off by default");
  var alt=all.Where(c=>c.Group=="Alternate basic ASD (203.4.2)").ToList();
  Check(alt[0].No==2001&&alt[0].Eq=="203-13"&&alt.Count==(4+8)+4+8+1+4+8&&alt.Last().Eq=="203-18","alternate ASD 2001...: 203-13 to 203-18: "+alt.Count);
  var a13=alt.First(c=>c.Eq=="203-13"&&c.Terms.Any(t=>t.Key.Kind=="E"));Check(Math.Abs(a13.Terms.First(t=>t.Key.Title=="DL").Value-1)<1e-9&&a13.Terms.Any(t=>t.Key.Kind=="Lr"&&Math.Abs(t.Value-0.75)<1e-9)&&a13.Terms.Any(t=>t.Key.Repeat!=null&&Math.Abs(t.Value-0.75/1.4)<1e-9),"203-13: D + 0.75[L + Lr + E/1.4], no Ev in ASD");
  var a15=alt.First(c=>c.Eq=="203-15");Check(Math.Abs(a15.Terms.First(t=>t.Key.Title=="DL").Value-0.6)<1e-9,"203-15: 0.6D + E/1.4 (Ev = 0 for ASD)");
  Check(alt.First(c=>c.Eq=="203-16").Title=="DL+LL1+LL2+LLR"&&alt.Where(c=>c.Eq=="203-17").All(c=>c.Terms.Any(t=>t.Key.Kind=="W"&&Math.Abs(t.Value-0.6)<1e-9))&&Math.Abs(alt.First(c=>c.Eq=="203-18").Terms.First(t=>t.Key.Title=="DL").Value-1)<1e-9,"203-16 to 203-18");
  var sp=all.Where(c=>c.Group=="Special seismic (203.5)").ToList();Check(sp.Count==16&&sp[0].No==3001&&sp[0].Eq=="203-19"&&sp.Last().Eq=="203-20"&&sp[0].Terms.Any(t=>t.Key.Repeat!=null&&Math.Abs(t.Value-2.8)<1e-9),"special seismic 3001...: 203-19 / 203-20, Omega0 Eh");
  var withBasic=new StaadCombinationSettings{Asd=true,EvInAsd=true};withBasic.F1One.Add("LL2");var allB4=StaadCombinations.Build(list,withBasic);var basic=allB4.Where(c=>c.Group=="Basic ASD (203.4.1)").ToList();
  Check(basic[0].No==4001&&basic[0].Eq=="203-8"&&basic.Count==1+1+1+1+4+8&&basic.Last().Eq=="203-12"&&Math.Abs(basic.Last().Terms.First(t=>t.Key.Title=="DL").Value-(1+ev/1.4))<1e-9,"basic ASD 4001...: 203-8 to 203-12, Ev in ASD only when chosen");
  var wd=all.Where(c=>c.Group=="Wind drift").ToList();var sd=all.Where(c=>c.Group=="Seismic drift").ToList();
  Check(wd.Count==4&&wd[0].No==5001&&Math.Abs(wd[0].Terms.First(t=>t.Key.Kind=="W").Value-0.6)<1e-9&&sd.Count==8&&sd[0].No==6001&&sd.All(c=>c.Terms.Count==1&&Math.Abs(c.Terms[0].Value-1)<1e-9),"wind drift 5001..., seismic drift 6001...");
  /* class A: each EQ case alone; class B: EQ +X +E ... each alone; Load Type None EQ cases still read as seismic */
  var a=new StaadCombinationSettings{Structure="A"};var allA=StaadCombinations.Build(list,a);Check(allA.Count(c=>c.Group=="Strength design (203.3.1)"&&(c.Eq=="203-5"||c.Eq=="203-7"))==8&&StaadCombinations.RepeatCases(allA).Count==0,"class A: 4 EQ cases x 2");
  var bList=StaadCombinations.Parse("1 DL Dead\n2 EQ +X +E Seismic\n3 EQ +X -E Seismic\n4 EQ -X +E Seismic\n5 EQ -X -E Seismic\n6 EQ +Z +E Seismic\n7 EQ +Z -E Seismic\n8 EQ -Z +E Seismic\n9 EQ -Z -E Seismic");
  var allB=StaadCombinations.Build(bList,new StaadCombinationSettings{Structure="B"});Check(bList.Count(p=>p.Kind=="E")==8&&allB.Count(c=>c.Group=="Strength design (203.3.1)"&&(c.Eq=="203-5"||c.Eq=="203-7"))==16&&bList[1].Axis=="X"&&bList[1].Sign==1,"class B: 8 EQ cases with accidental torsion, each alone");
  var none=StaadCombinations.Parse("LOAD 5 LOADTYPE NONE TITLE EQ+X");Check(none[0].Kind=="E"&&none[0].Axis=="X","Load Type None EQ case (class C primaries)");
  var asdE=alt.First(c=>c.Eq=="203-18");Check(asdE.Terms.Any(t=>t.Key.Repeat!=null&&Math.Abs(t.Value-1/1.4)<1e-9),"ASD E / 1.4");
  var rr=StaadCombinations.Build(StaadCombinations.Parse("1 DL Dead\n2 EQ Seismic-H"),new StaadCombinationSettings{Rho=1.3,IncludeEv=false,Asd=false});var e1=rr.Where(c=>c.Eq=="203-5"||c.Eq=="203-7").ToList();
  Check(e1.Count==4&&e1.Any(c=>c.Terms.Any(t=>t.Key.Title=="EQ"&&Math.Abs(t.Value+1.3)<1e-9)),"one E case: +E and -E, times rho 1.3");
  var text=StaadCombinations.Staad(all,list,s);Check(text.Contains("LOAD COMBINATION 1001 1.4DL")&&text.Contains("LOAD 13 LOADTYPE SEISMIC TITLE 1.0(EQ+X)+0.3(EQ+Z)\nREPEAT LOAD\n5 1 7 0.3")&&text.Contains("LOAD COMBINATION 3001 ")&&text.Contains("* Eq. 203-5")&&text.Split('\n').All(x=>x.TrimEnd('\r').Length<=72),"STAAD text: REPEAT LOAD cases, combinations, lines within 72 columns");
  Check(ProgramAccess.ForSheet("Load-Combinations")==ProgramAccess.LoadCombinations&&(ProgramAccess.All&ProgramAccess.LoadCombinations)!=0&&ProgramAccess.Names.Contains("Load Combinations (NSCP 2015)"),"own access program");
  try{StaadCombinations.Build(StaadCombinations.Parse("1 LL"),s);Check(false,"no dead load must be refused");}catch(Exception ex){Check(ex.Message.Contains("dead"),"no dead load case: "+ex.Message);}
  /* automatic primary loads, earthquake cases first, PERFORM ANALYSIS / CHANGE after each EQ case (owner, 26 November 2026) */
  {var b=StaadCombinations.Parse(StaadCombinations.AutoPrimary("B",2,true,true));Check(b.Count==16&&b.Take(8).All(p=>p.Kind=="E")&&b[0].Title=="EQ+X+E"&&b[7].Title=="EQ-Z-E"&&b[8].Title=="DL"&&b[11].Kind=="Lr"&&b.Skip(12).All(p=>p.Kind=="W"),"structure B: 8 EQ cases first, then DL, LL1, LL2, LLR, WL");
   var sb=new StaadCombinationSettings{Structure="B",AutoPrimary=true,PerformChange=true};var txt=StaadCombinations.Staad(StaadCombinations.Build(b,sb),b,sb);
   Check(txt.Contains("LOAD 2 LOADTYPE SEISMIC-H TITLE EQ+X-E\nUBC LOAD X 1 DEC 1 ACC -0.05\nPERFORM ANALYSIS\nCHANGE")&&txt.Contains("LOAD 9 LOADTYPE DEAD TITLE DL\n* add the dead loads here")&&!txt.Contains("SELFWEIGHT"),"primary section with UBC LOAD, ACC and PERFORM ANALYSIS / CHANGE");
   Check(StaadCombinations.Build(b,sb).Any(x=>x.Title=="1.42DL+0.5LL1+0.5LL2+(EQ+X+E)")&&StaadCombinations.Build(b,new StaadCombinationSettings{Structure="B",Rho=1.3}).Any(x=>x.Title.EndsWith("+1.3(EQ+X+E)")),"signed titles in brackets: 1.3(EQ+X+E)");
   Check(!StaadCombinations.Staad(StaadCombinations.Build(b,new StaadCombinationSettings{Structure="B",AutoPrimary=true}),b,new StaadCombinationSettings{Structure="B",AutoPrimary=true}).Contains("CHANGE"),"no CHANGE by default (RCDC reads one analysis)");
   {var sx=new StaadCombinationSettings{Structure="B",AutoPrimary=true,Ax=1.5};var tx=StaadCombinations.Staad(StaadCombinations.Build(b,sx),b,sx);Check(tx.Contains("UBC LOAD X 1 DEC 1 ACC 0.075")&&tx.Contains("UBC LOAD X 1 DEC 1 ACC -0.075")&&tx.Contains("Natural torsion")&&tx.Contains("FLOOR DIAPHRAGM"),"case B: natural torsion note and ACC = 0.05 Ax");}
   {var se=new StaadCombinationSettings{Structure="B"};var te=StaadCombinations.Staad(StaadCombinations.Build(b,se),b,se);Check(te.Contains("DEFINE ENVELOPE\n* Strength design (203.3.1)\n1001 TO 1031 ENVELOPE 1 TYPE STRENGTH")&&te.Contains("ENVELOPE 2 TYPE SERVICEABILITY")&&te.Contains("TYPE STRENGTH")&&te.IndexOf("DEFINE ENVELOPE")>te.LastIndexOf("LOAD COMBINATION")&&te.Contains("* ---- ANALYSIS AND REPORTS ----\nPERFORM ANALYSIS\n* envelopes")&&te.IndexOf("END DEFINE ENVELOPE")<te.IndexOf("PRINT CG")&&!te.Contains("PRINT LOAD DATA"),"PERFORM ANALYSIS after the last combination, then DEFINE ENVELOPE (STAAD 2026: no DEFINE between the loads and the analysis), no PRINT LOAD DATA");
    {var sa=new StaadCombinationSettings{Structure="B",AutoPrimary=true,EqCommand="UBC LOAD"};var ta=StaadCombinations.Staad(StaadCombinations.Build(b,sa),b,sa);
     Check(ta.IndexOf("END DEFINE ENVELOPE")>ta.LastIndexOf("\nLOAD COMBINATION ")&&!ta.Contains("DEFINE UBC LOAD\n")&&ta.Contains("* Before them: DEFINE UBC LOAD"),"automatic primary loads: no DEFINE UBC LOAD written (the owner's own input has it), envelopes after the combinations");}
   {var sr=new StaadCombinationSettings{Structure="B",AutoPrimary=true,EqCommand="UBC LOAD",RefLoads=StaadCombinations.ReferenceLoads("DEFINE REFERENCE LOADS\nLOAD R1 LOADTYPE Dead  TITLE DL\nSELFWEIGHT Y -1\nLOAD R2 LOADTYPE Live  TITLE LL1\nLOAD R3 LOADTYPE Live  TITLE LL2\nLOAD R4 LOADTYPE Mass  TITLE REF MASS MODEL\nEND DEFINE REFERENCE LOADS")};
    var tr=StaadCombinations.Staad(StaadCombinations.Build(b,sr),b,sr);
    Check(tr.Contains("TITLE DL\nREFERENCE LOAD\nR1 1.0")&&tr.Contains("TITLE LL1\nREFERENCE LOAD\nR2 1.0")&&tr.Contains("TITLE LL2\nREFERENCE LOAD\nR3 1.0")&&!tr.Contains("R4 1.0")&&!tr.Contains("SELFWEIGHT"),"DL / LL1 / LL2 point at the reference loads R1 / R2 / R3 of the model (mass model left out)");}
    Check(te.Contains("PRINT DIAPHRAGM CR")&&te.Contains("PRINT STORY STIFFNESS")&&te.Contains("LOAD LIST 6001 TO 6008\nPRINT STORY DRIFT")&&te.Contains("LOAD LIST 1 TO 8\nPRINT JOINT DISPLACEMENTS ALL")&&te.Contains("LOAD LIST ALL\nPRINT SUPPORT REACTION ALL"),"print commands with their load lists");}
   {var dg=StaadCombinations.Build(b,new StaadCombinationSettings{Structure="B",DriftGravity=true}).Where(x=>x.Group=="Seismic drift").ToList();var d0=StaadCombinations.Build(b,new StaadCombinationSettings{Structure="B"}).Where(x=>x.Group=="Seismic drift").ToList();
    Check(d0.All(x=>x.Terms.All(t=>t.Key.Kind=="E"))&&dg.All(x=>x.Terms.Any(t=>t.Key.Kind=="D"&&t.Value==1.0)&&x.Terms.Any(t=>t.Key.Kind=="L"&&t.Value==0.5)),"seismic drift: 1.0 Eh, or D + 0.5L + 1.0 Eh for P-Delta");}
   var c=StaadCombinations.Parse(StaadCombinations.AutoPrimary("C",1,false,false));Check(c.Count==6&&c[0].Title=="EQ+X"&&c[4].Title=="DL"&&c[5].Title=="LL","structure C: 4 EQ, DL, LL");
   var sc=new StaadCombinationSettings{Structure="C",AutoPrimary=true,PerformChange=false};var t2=StaadCombinations.Staad(StaadCombinations.Build(c,sc),c,sc);Check(t2.Contains("LOAD 1 LOADTYPE NONE TITLE EQ+X\nUBC LOAD X 1\nLOAD 2")&&!t2.Contains("CHANGE"),"structure C: Load Type None; no CHANGE when switched off");
   Check(!StaadCombinations.Staad(StaadCombinations.Build(c,new StaadCombinationSettings()),c,new StaadCombinationSettings()).Contains("PRIMARY LOAD CASES"),"pasted primaries: no primary section");}
  Console.WriteLine("PASS: "+checks+" load combination checks");}
}
