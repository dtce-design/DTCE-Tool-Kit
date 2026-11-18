using System;using System.Collections.Generic;using System.Linq;
namespace BeamScheduleFormatter {
// PERT-CPM (owner, 18 November 2026: "PERT CPM din, +/-20% ang optimistic at pessimistic"). Most likely time m = the
// activity duration from the output per hour; optimistic a = 0.8 m, pessimistic b = 1.2 m; expected te = (a + 4m + b) / 6;
// variance = ((b - a) / 6)^2. The network is the same realistic precedence network as the PDM (FS / SS / FF with lags);
// activity-on-arrow cannot show SS / FF overlaps without splitting activities, so the PERT network is drawn activity on
// node like the PDM. Project Te = CPM duration with te. Its variance is the sum of the variances of the activities whose
// durations drive the finish: the driving path is traced back from FINISH; an activity reached through an SS or FS link at
// its start and left at its finish counts, one only passed through by its start (SS) or its finish (FF) does not.
// Completion at 90 % / 95 % confidence = Te + 1.2816 s / Te + 1.6449 s (normal distribution).
sealed class PertActivity {internal PdmNode Node;internal double A,M,B,Te,Variance;}
sealed class PertResult {
 internal readonly List<PertActivity> Activities=new List<PertActivity>();internal double Te,Sigma;internal int P90,P95;internal readonly List<PertActivity> Chain=new List<PertActivity>();
 internal const double Optimistic=0.8,Pessimistic=1.2;
 internal static PertResult Build(PdmNetwork n){var r=new PertResult();foreach(var nd in n.Nodes){double m=nd.Row.Days,a=m*Optimistic,b=m*Pessimistic;r.Activities.Add(new PertActivity{Node=nd,A=a,M=m,B=b,Te=(a+4*m+b)/6,Variance=Math.Pow((b-a)/6,2)});}
  var act=r.Activities.ToDictionary(x=>x.Node);var end=n.Nodes.Where(x=>x.Critical&&x.EF==n.Total).OrderByDescending(x=>x.Row.Days).FirstOrDefault();
  /* trace the driving path back: state = node and whether we stand at its finish (true) or its start (false) */
  var node=end;bool atFinish=true;int guard=0;
  while(node!=null&&guard++<10000){int d=node.Row.Days;var tight=n.Links.Where(l=>l.To==node).FirstOrDefault(l=>{var p=l.From;int b=l.Type=="FS"?p.EF+l.Lag:l.Type=="SS"?p.ES+l.Lag:p.EF+l.Lag-d;return b==node.ES;});
   if(atFinish){if(tight==null||tight.Type!="FF")r.Chain.Add(act[node]);}else if(tight!=null&&tight.Type=="FF")r.Chain.Add(act[node]);
   if(tight==null)break;atFinish=tight.Type!="SS";node=tight.From;}
  r.Chain.Reverse();r.Te=n.Total;r.Sigma=Math.Sqrt(r.Chain.Sum(x=>x.Variance));r.P90=(int)Math.Ceiling(r.Te+1.2816*r.Sigma-1e-9);r.P95=(int)Math.Ceiling(r.Te+1.6449*r.Sigma-1e-9);return r;}
}
}
