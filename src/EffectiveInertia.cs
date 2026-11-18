using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
// Effective moment of inertia of a rectangular RC beam, NSCP 2015 Section 424.2.3.5 (Eq. 424.2.3.5a):
// Ie = (Mcr/Ma)^3 Ig + [1 − (Mcr/Ma)^3] Icr ≤ Ig,  Mcr = fr Ig / yt,  fr = 0.62 λ √fc'  (Section 419.2.3.1).
class EffectiveInertiaInput {public double B,H,D,DPrime,As,AsPrime,Fc,Ma,Lambda=1,Es=200000;}
class EffectiveInertiaResult {public double Ec,N,Ig,Yt,Fr,Mcr,C,Icr,Ie,Ratio,RhoPrime;}
static class EffectiveInertia {
 static bool Finite(double v){return !double.IsNaN(v)&&!double.IsInfinity(v);}
 internal static EffectiveInertiaResult Calculate(EffectiveInertiaInput i){
  foreach(double v in new[]{i.B,i.H,i.D,i.As,i.Fc,i.Ma,i.Lambda,i.Es})if(!Finite(v)||v<=0)throw new Exception("Enter positive b, h, d, As, fc', Ma, λ and Es.");
  if(!Finite(i.AsPrime)||i.AsPrime<0||!Finite(i.DPrime)||i.DPrime<0)throw new Exception("As' and d' must be zero or positive.");
  if(i.D>=i.H||i.D<=i.H/3)throw new Exception("Effective depth d must be less than h and more than h/3.");if(i.AsPrime>0&&(i.DPrime<=0||i.DPrime>=i.D))throw new Exception("d' must be between 0 and d when As' is entered.");
  if(i.Fc<14||i.Fc>100)throw new Exception("fc' should be between 14 and 100 MPa.");if(i.Lambda>1)throw new Exception("λ is at most 1.0.");
  var r=new EffectiveInertiaResult();r.Ec=4700*Math.Sqrt(i.Fc);r.N=i.Es/r.Ec;r.Ig=i.B*Math.Pow(i.H,3)/12;r.Yt=i.H/2;r.Fr=.62*i.Lambda*Math.Sqrt(i.Fc);r.Mcr=r.Fr*r.Ig/r.Yt/1e6;
  // Cracked transformed section: (b/2)c² + [(n−1)As' + n As]c − [(n−1)As' d' + n As d] = 0.
  double a=i.B/2,bq=(r.N-1)*i.AsPrime+r.N*i.As,cq=-((r.N-1)*i.AsPrime*i.DPrime+r.N*i.As*i.D);r.C=(-bq+Math.Sqrt(bq*bq-4*a*cq))/(2*a);
  r.Icr=i.B*Math.Pow(r.C,3)/3+(r.N-1)*i.AsPrime*Math.Pow(r.C-i.DPrime,2)+r.N*i.As*Math.Pow(i.D-r.C,2);
  double ratio=Math.Min(1,r.Mcr/i.Ma),cube=Math.Pow(ratio,3);r.Ie=Math.Min(r.Ig,cube*r.Ig+(1-cube)*r.Icr);r.Ratio=r.Ig/r.Ie;r.RhoPrime=i.AsPrime/(i.B*i.D);return r;}
 static string F(double v){return v.ToString("R",CultureInfo.InvariantCulture);}
 internal static string Basis(EffectiveInertiaInput i){return "b="+F(i.B)+"; h="+F(i.H)+"; d="+F(i.D)+"; d'="+F(i.DPrime)+"; As="+F(i.As)+"; As'="+F(i.AsPrime)+"; fc'="+F(i.Fc)+"; Ma="+F(i.Ma)+"; λ="+F(i.Lambda);}
 internal static EffectiveInertiaInput Parse(string basis){var values=new Dictionary<string,double>();foreach(string part in (basis??"").Split(';')){var kv=part.Split('=');if(kv.Length!=2)continue;double v;if(!double.TryParse(kv[1].Trim(),NumberStyles.Float,CultureInfo.InvariantCulture,out v))throw new Exception("Invalid Ie basis value: "+part);values[kv[0].Trim()]=v;}
  Func<string,double> get=k=>{double v;if(!values.TryGetValue(k,out v))throw new Exception("Ie basis is missing "+k+".");return v;};return new EffectiveInertiaInput{B=get("b"),H=get("h"),D=get("d"),DPrime=get("d'"),As=get("As"),AsPrime=get("As'"),Fc=get("fc'"),Ma=get("Ma"),Lambda=values.ContainsKey("λ")?values["λ"]:1};}
 internal static string[][] Rows(EffectiveInertiaInput i,EffectiveInertiaResult r){Func<double,int,string> f=(v,d)=>v.ToString("F"+d,CultureInfo.InvariantCulture);Func<double,string> e=v=>v.ToString("0.000E+0",CultureInfo.InvariantCulture);return new[]{
  new[]{"Section","b = "+f(i.B,0)+" mm, h = "+f(i.H,0)+" mm, d = "+f(i.D,0)+" mm, d' = "+f(i.DPrime,0)+" mm","As = "+f(i.As,0)+" mm², As' = "+f(i.AsPrime,0)+" mm², fc' = "+f(i.Fc,1)+" MPa, λ = "+f(i.Lambda,2)},
  new[]{"Ec, n","Ec = 4700√fc' = "+f(r.Ec,0)+" MPa","n = Es/Ec = 200000 / "+f(r.Ec,0)+" = "+f(r.N,3)},
  new[]{"Ig, Mcr","Ig = b h³/12 = "+e(r.Ig)+" mm⁴","fr = 0.62λ√fc' = "+f(r.Fr,3)+" MPa;  Mcr = fr Ig / yt = "+f(r.Mcr,2)+" kN·m"},
  new[]{"Icr","c = "+f(r.C,1)+" mm (cracked neutral axis)","Icr = b c³/3 + (n−1)As'(c−d')² + n As (d−c)² = "+e(r.Icr)+" mm⁴"},
  new[]{"Ie (Eq. 424.2.3.5a)","Ma = "+f(i.Ma,2)+" kN·m; (Mcr/Ma)³ = "+f(Math.Pow(Math.Min(1,r.Mcr/i.Ma),3),4),"Ie = "+e(r.Ie)+" mm⁴ ≤ Ig;  Ig/Ie = "+f(r.Ratio,3)+";  ρ' = As'/(b d) = "+f(r.RhoPrime,5)}};}
}
}
