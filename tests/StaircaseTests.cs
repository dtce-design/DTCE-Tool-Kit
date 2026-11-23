using System;using System.Linq;using BeamScheduleFormatter;
/* Staircase design sheet: every stair type designs; loads, moment, steel and checks against hand calculations */
class StaircaseTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static bool Near(double a,double b,double tol=0.01){return Math.Abs(a-b)<=tol*Math.Max(1,Math.Abs(b));}
 static void Main(){
  foreach(var ty in StairInput.Types){var r=Staircase.Design(new StairInput{Type=ty});Check(r.Members.Count>0&&r.Checks.Count>=7&&r.Steel.Count>0,"designs "+ty);Check(r.Checks.All(c=>c.Ok),"default "+ty+" passes: "+string.Join("; ",r.Checks.Where(c=>!c.Ok).Select(c=>c.Name)));}
  /* straight flight, 9 risers 175 / 275, waist 175, no landings: hand check */
  var i=new StairInput{Type="Straight flight (waist slab)",N1=9};var s=Staircase.Design(i);double R=.175,T=.275,t=.175;double wd=23.6*(t*Math.Sqrt(R*R+T*T)/T+R/2)+1.1*(R+T)/T;
  Check(Near(s.Wd,wd,1e-6),"flight dead load "+s.Wd+" / "+wd);double L=8*0.275,w=1.2*wd+1.6*4.8;double mp,mn,v;Staircase.Analyse(s.Members[0],true,out mp,out mn,out v);
  Check(Near(mp,w*L*L/8,0.002),"simply supported moment wL²/8: "+mp+" / "+w*L*L/8);Check(Near(v,w*L/2,0.002),"shear wL/2");Check(mn<1e-9,"no negative moment when simply supported");
  i.Support="Both ends continuous";Staircase.Analyse(Staircase.Design(i).Members[0],true,out mp,out mn,out v);Check(Near(mn,w*L*L/12,0.002)&&Near(mp,w*L*L/24,0.01),"both ends continuous: wL²/12 at the supports, wL²/24 at midspan");
  /* flexure: As for Mu, phiMn of the provided bars exceeds Mu */double rho;double As=Staircase.AsRequired(10,1000,145,21,414,out rho);Check(As>180&&As<200,"As for 10 kN·m/m, d = 145: "+As);Check(Staircase.PhiMn(As,1000,145,21,414)>=10*0.999,"phiMn(As) = Mu");
  /* dog-legged: two flights, each with the half landing; headroom from the storey height */
  var d=Staircase.Design(new StairInput{Type="Dog-legged (two flights, half landing)"});Check(d.Members.Count==2&&Near(d.Members[0].Length,(8*275+1300)/1000.0,1e-9),"dog-leg flight with the half landing span");Check(d.Checks.Any(c=>c.Name.StartsWith("Headroom")&&c.Ok),"headroom under the next storey");
  var own=Staircase.Design(new StairInput{Type="Dog-legged (two flights, half landing)",Landing="Landing on its own beams / walls"});Check(own.Members.Count==3,"landing on its own supports is a third member");
  /* cantilever step: Mu = max(uniform, point at the tip) */var c0=new StairInput{Type="Cantilever steps (from a wall / spine beam)"};var cr=Staircase.Design(c0);double wD=cr.Wd*0.275,wL=cr.Wl*0.275,Lc=1.2;double Mu=Math.Max(1.2*wD*Lc*Lc/2+1.6*wL*Lc*Lc/2,1.2*wD*Lc*Lc/2+1.6*1.33*Lc);Check(Near(cr.Checks.First(q=>q.Name.StartsWith("Flexure")).Demand,Mu,1e-6),"cantilever step moment");
  /* geometry: a 220 riser fails, 2R + T out of range fails */var g=Staircase.Design(new StairInput{Type="Straight flight (waist slab)",Riser=220,Tread=230});Check(g.Checks.Any(q=>q.Name=="Riser height"&&!q.Ok)&&g.Checks.Any(q=>q.Name.StartsWith("Tread")&&!q.Ok),"geometry limits");
  /* access: the new design sheet has its own program bit */Check(ProgramAccess.ForSheet("Staircase-Design")==ProgramAccess.StaircaseDesign&&(ProgramAccess.All&ProgramAccess.StaircaseDesign)!=0&&ProgramAccess.Names.Contains("Staircase Design"),"staircase access");
  /* support beams at their place on the landing (owner, 23 November 2026) */
  {var b0=new StairInput();var r0=Staircase.Design(b0);var b1=new StairInput{LandingBeamAt=900};var r1=Staircase.Design(b1);
   Check(Math.Abs(r1.Members[0].Length-(r0.Members[0].Length-0.4))<1e-9&&Math.Abs(r1.Members[0].OverEnd-0.4)<1e-9,"landing beam 900 mm from the flight: span to the beam, 400 mm overhang beyond it");
   double bp,bn,bv;Staircase.Analyse(r1.Members[0],true,out bp,out bn,out bv);Check(bn>0&&bn>=(1.2*r1.WdLanding+1.6*r1.WlLanding)*0.4*0.4/2-1e-9,"the overhang hogs the end over the beam");
   Check(new StairInput{LandingBeamAt=2000}.LandingSpan(1300)==1300&&new StairInput().OnBeams,"a beam beyond the landing end, or 0, is at the landing end; beams are the default support");}
  {var oc=Staircase.Design(new StairInput{Support="One end continuous"});var m=oc.Members[0];double p1,n1,v1;var dd=Staircase.Analyse(m,true,out p1,out n1,out v1);double W=m.Segs.Sum(q=>(1.2*q.Wd+1.6*q.Wl)*q.Length);double expect=-W/m.Length*m.Length*m.Length/10;
   Check(Math.Abs(dd[2][0])<1e-9&&Math.Abs(dd[2][dd[2].Length-1]-expect)<1e-6,"one end continuous: 0 at the free end, wL²/10 hogging at the continuous end ("+dd[2][dd[2].Length-1]+" vs "+expect+")");}
  /* anchorage, nosing and chair bars (owner, 23 November 2026) */
  {var d0=new StairInput();Check(Staircase.HookLength(d0)==275&&Staircase.DevLength(d0)==525,"Ø12, f'c 21, fy 414: ldh 0.24 fy/√f'c db = 260 -> 275 mm; ld fy/(2.1√f'c) db = 516 -> 525 mm");
   var rs=Staircase.Design(d0);Check(rs.Steel.Any(q=>q[1].StartsWith("Nosing bars"))&&rs.Steel.Any(q=>q[1].StartsWith("Step bars (chair bars)"))&&rs.Steel.Any(q=>q[1].StartsWith("Anchorage of the top bars")&&q[2].Contains("ldh = 275")),"nosing bars, step (chair) bars and anchorage are listed");}
  Console.WriteLine("PASS: "+checks+" staircase design checks");}
}
