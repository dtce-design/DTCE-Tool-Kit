using System;
namespace BeamScheduleFormatter {
static class OpeningHardware {
 public const double WoodDoorThickness=.045;
 // Quantity-estimating basis: Hager "Determine the Size of Hinge", not hinge load selection.
 public const string Source="https://hagerco.freshdesk.com/support/solutions/articles/73000552399-determine-the-size-of-hinge";
 public static int PerLeaf(double height,double width){if(double.IsNaN(height+width)||double.IsInfinity(height+width)||height<=0||width<=0)throw new Exception("Check door leaf height and width.");int hinges=Math.Max(2,checked((int)Math.Ceiling(Math.Round(height/.762,10))));if(width>.94)hinges++;return hinges;}
}
}
