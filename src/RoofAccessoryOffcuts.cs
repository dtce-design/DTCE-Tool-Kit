using System;using System.Linq;using System.Collections.Generic;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class ProgramOfWorksForm {
 void ReuseRoofAccessoryOffcuts(){
  var valid=roofAccessories.Rows.Cast<DataGridViewRow>().Where(row=>row.Tag is RoofAccessoryQuantity);
  // Fabrication description/type/developed width must match; unknown profiles are never mixed by type alone.
  foreach(var group in valid.GroupBy(row=>(Cell(row,2)+"|"+Cell(row,1).Trim()+"|"+OptionalNumber(row,5,0).ToString("R",System.Globalization.CultureInfo.InvariantCulture)).ToUpperInvariant())){
   var cuts=new List<StockAllocation>();var owners=new Dictionary<StockAllocation,DataGridViewRow>();
   foreach(var row in group){double run=Number(row,3),stock=Number(row,6),lap=Number(row,7)/1000;int qty=Count(row,4),segments=run<=stock?1:RoofMath.Ceil((run-lap)/(stock-lap));double tail=run-(segments-1)*(stock-lap);Action<double,long> add=(cut,pieces)=>{if(pieces<=0)return;var a=new StockAllocation{UnitMassOverride=1,Allowed=new List<double>{stock},Demand=new StockDemand{Grade=1,Diameter=1,Cut=cut,Pieces=pieces},Choice=new StockChoice{Length=stock,Cut=cut,Required=pieces}};cuts.Add(a);owners.Add(a,row);};add(stock,(long)(segments-1)*qty);add(tail,qty);var q=(RoofAccessoryQuantity)row.Tag;q.Pieces=0;q.BuyLength=0;q.BuyArea=0;}
   RebarOffcuts.Allocate(cuts);
   foreach(var bin in cuts.SelectMany(a=>a.Stocks).Distinct()){var owner=owners[bin.Used.Keys.OrderByDescending(a=>a.Demand.Cut).First()];var q=(RoofAccessoryQuantity)owner.Tag;q.Pieces=checked(q.Pieces+(int)bin.Count);q.BuyLength+=bin.Count*bin.Length;q.BuyArea+=bin.Count*bin.Length*OptionalNumber(owner,5,0)/1000;}
   foreach(var row in group){var q=(RoofAccessoryQuantity)row.Tag;row.Cells[8].Value=q.Pieces;row.Cells[9].Value=q.BuyLength.ToString("0.00");row.Cells[10].Value=q.BuyArea.ToString("0.00");}
  }
 }
}
}
