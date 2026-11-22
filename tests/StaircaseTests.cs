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
  Console.WriteLine("PASS: "+checks+" staircase design checks");}
}
