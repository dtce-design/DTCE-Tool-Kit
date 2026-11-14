using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
class RoofProfile {public string Name,Family,SideLap,Material;public string MaterialType{get{return string.IsNullOrWhiteSpace(Material)?RoofingPaySpecification.LegacyMaterial(Name):Material;}}public double Width,Cover;}

}
