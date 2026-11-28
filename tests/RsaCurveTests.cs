using System;using System.Linq;using System.Windows.Forms;using BeamScheduleFormatter;
class RsaCurveTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}static void Near(double a,double b,string why){Check(Math.Abs(a-b)<1e-6,why+" ("+a+" vs "+b+")");}
 [STAThread]static void Main(){
  /* Tables 208-4 / 208-5 near-source factors with interpolation */
  Near(NscpSpectrum.Na("A",2),1.5,"Na source A at 2 km");Near(NscpSpectrum.Nv("A",2),2.0,"Nv source A at 2 km");Near(NscpSpectrum.Na("A",3.5),1.35,"Na interpolated at 3.5 km");Near(NscpSpectrum.Nv("A",3.5),1.8,"Nv interpolated at 3.5 km");
  Near(NscpSpectrum.Na("B",10),1.0,"Na source B at 10 km");Near(NscpSpectrum.Nv("B",12.5),1.0,"Nv source B beyond 10 km");Near(NscpSpectrum.Nv("A",15),1.0,"Nv source A at 15 km");Near(NscpSpectrum.Na("C",0),1.0,"source C");
  /* Tables 208-7 / 208-8 */
  Near(NscpSpectrum.Ca(4,3,1.5),.66,"Ca zone 4 SD x Na");Near(NscpSpectrum.Cv(4,3,2.0),1.28,"Cv zone 4 SD x Nv");Near(NscpSpectrum.Ca(4,2,1),.40,"Ca zone 4 SC");Near(NscpSpectrum.Cv(4,4,1),.96,"Cv zone 4 SE");Near(NscpSpectrum.Ca(2,3,9),.28,"Ca zone 2 SD ignores Na");Near(NscpSpectrum.Cv(2,4,9),.64,"Cv zone 2 SE");
  /* Figure 208-3 shape */
  double ca=.66,cv=1.28,ts=cv/(2.5*ca),t0=.2*ts;Near(NscpSpectrum.Sa(0,ca,cv),.66,"Sa(0) = Ca");Near(NscpSpectrum.Sa(t0/2,ca,cv),.66*1.75,"linear rise to 2.5Ca");Near(NscpSpectrum.Sa(.5,ca,cv),1.65,"plateau 2.5Ca");Near(NscpSpectrum.Sa(2,ca,cv),.64,"Cv / T");
  var points=NscpSpectrum.Points(ca,cv,4,.05);Check(points.Any(p=>Math.Abs(p[0]-ts)<1e-9&&Math.Abs(p[1]-2.5*ca)<1e-9)&&points.Any(p=>Math.Abs(p[0]-t0)<1e-9&&Math.Abs(p[1]-2.5*ca)<1e-9)&&points[0][0]==0&&Math.Abs(points.Last()[0]-4)<1e-9,"T0, Ts, 0 and the last period are curve points");
  AccessCodes.Granted=true;using(var form=new RsaCurveForm()){string report=form.CurrentReport();
   Check(report.Contains("SPECTRUM CQC X 1 ACC SCALE 1.153724 DAMP 0.05 LIN")&&report.Contains("SPECTRUM CQC Z 1 ACC SCALE")&&report.Contains("LOAD 101 LOADTYPE SEISMIC TITLE RSA X")&&report.Contains("LOAD 102"),"STAAD input: two directions, scale g x I / R");
   Check(report.Contains("Ca (Table 208-7) = 0.44")&&report.Contains("Cv (Table 208-8) = 0.768")&&report.Contains("Ts = Cv / (2.5 Ca)"),"defaults: zone 4, SD, source A at 10 km (Na 1.0, Nv 1.2)");
   /* ELF beside the dynamic input (owner, 28 November 2026): DEFINE UBC LOAD with the sheet's zone, I, R, soil, Na / Nv and the Method A period */
   Check(report.Contains("DEFINE UBC LOAD")&&report.Contains("ZONE 0.4 I 1 RWX 8.5 RWZ 8.5 STYP 4 NA 1 NV 1.2 PX ")&&report.Contains("SELFWEIGHT 1"),"ELF input: DEFINE UBC LOAD from the sheet");
   Check(report.IndexOf("ELF (static lateral force)")>report.IndexOf("SPECTRUM CQC Z 1"),"report: dynamic first, then ELF");}
  Console.WriteLine("PASS: "+checks+" NSCP 2015 response spectrum checks");}
}
