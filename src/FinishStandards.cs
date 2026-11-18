namespace BeamScheduleFormatter {
// Standard finish groups with their BACKUP item numbers (owner list, 18 November 2026: "ilagay din yung item no.").
// Name = group name shown in Finishing Works; PayDescription = the BACKUP catalogue description used for routing.
sealed class FinishStandard {internal readonly string Code,Name,PayDescription;internal FinishStandard(string code,string name,string pay){Code=code;Name=name;PayDescription=pay;}
 internal string Caption{get{return Code+" | "+Name;}}
 internal static readonly FinishStandard[] All={
  new FinishStandard("1016(1)a","Two-Component Cementitious Waterproofing System, 3 Coats","Two-Component Cementitious Waterproofing System, 3 Coats"),
  new FinishStandard("1016(1)b","One-Component Polyurethane-Acrylic Liquid Applied Flexible Membrane, 3 Coats","One-Component Polyurethane-Acrylic Liquid Applied Flexible Membrane, 3 Coats"),
  new FinishStandard("1016(2)","4mm thk. Slated Bituminous Membrane Waterproofing","4mm thk. Slated Bituminous Membrane Waterproofing"),
  new FinishStandard("1016(2)","3mm thk. Sanded Bituminous Membrane Waterproofing","3mm thk. Sanded Bituminous Membrane Waterproofing"),
  new FinishStandard("1018(1)","Porcelain Glazed Tiles and Trims","Porcelain Glazed Tiles and Trims"),
  new FinishStandard("1018(2)","Porcelain Unglazed Tiles","Porcelain Unglazed Tiles"),
  new FinishStandard("1018(3)","Granite Stone Countertop","Granite Stone Countertop"),
  new FinishStandard("1018(7)","Textured Unglazed Tiles (Yellow Warning Tactile Tiles)","Textured Unglazed Tiles (Yellow Warning Tactile Tiles)"),
  new FinishStandard("1020(1)a","Vinyl Floor Tile","Vinyl Floor Tile"),
  new FinishStandard("1021(1)a","Plain Cement Floor Finish","Plain Cement Floor Finish"),
  new FinishStandard("1021(1)c","Plain Cement Floor Finish with Floor Hardener","Plain Cement Floor Finish with Floor Hardener"),
  new FinishStandard("1021(3)b","Floor Topping (Trowelled Finish)","Floor Topping (Trowelled Finish)"),
  new FinishStandard("1021(5)","Cement Broom Finish","Cement Broom Finish"),
  new FinishStandard("1021(6)","Nonslip Finish","Nonslip Finish"),
  new FinishStandard("1025(1)a","Bush Hammered Finish","Bush Hammered Finish"),
  new FinishStandard("1027(1)","Cement Plaster Finish (Paper Finish)","Cement Plaster Finish (Paper Finish)"),
  new FinishStandard("1027(1)","Cement Plaster Finish (Float Finish)","Cement Plaster Finish (Float Finish)"),
  new FinishStandard("1027(3)","Decorative Stone (Bricks)","Decorative Stone (Bricks)"),
  new FinishStandard("1034(1)b","Damp Proofing (Polyethylene Sheet)","Damp Proofing (Polyethylene Sheet)"),
  new FinishStandard("1034(1)b","Moisture Barrier and Pest Control Film, M168","Moisture Barrier and Pest Control Film, M168"),
  new FinishStandard("1039(1)","Aluminum Cladding","Aluminum Cladding"),
  new FinishStandard("1032(1)a","Masonary/Concrete Painting Works","Masonary/Concrete Painting Works (No Top Coat)"),
  new FinishStandard("1032(1)a","Masonary Painting Works (Skim Coat)","Masonary Painting Works (Skim Coat)"),
  new FinishStandard("1032(1)a","Concrete Painting Works (Chlorinated Rubber-based Floor Coating)","Concrete Painting Works (Chlorinated Rubber-based Floor Coating)"),
  new FinishStandard("1032(1)a","Concrete Painting Works (Polyurethane-based Floor Coating)","Concrete Painting Works (Polyurethane-based Floor Coating)"),
  new FinishStandard("1032(1)a","Concrete Painting Works (Epoxy-based Floor Coating)","Concrete Painting Works (Epoxy-based Floor Coating)"),
  new FinishStandard("1032(1)b","Wood Painting Works","Wood Painting Works"),
  new FinishStandard("1032(1)b","Wood Painting Works (Dry Wall)","Wood Painting Works (Dry Wall)"),
  new FinishStandard("1032(1)b","Wood Painting Works (Ceiling)","Wood Painting Works (Ceiling)"),
  new FinishStandard("1032(1)c","Metal Painting Works","Metal Painting Works"),
  new FinishStandard("1032(1)c","Metal Painting Works (Automotive Finish)","Metal Painting Works (Automotive Finish)"),
  new FinishStandard("1032(1)c","Metal Painting Works (with Rust Converter)","Metal Painting Works (with Rust Converter)"),
  new FinishStandard("1032(2)","Varnishing Works (Oil Wood Stain)","Varnishing Works (Oil Wood Stain)"),
  new FinishStandard("1032(4)","Ducco Finish","Ducco Finish"),
  new FinishStandard("1032(5)","Textured Finish","Textured Finish")};
 internal static FinishStandard Find(string group){string key=FinishTypeRegistry.Key(group);foreach(var s in All)if(FinishTypeRegistry.Key(s.Name)==key||FinishTypeRegistry.Key(s.Caption)==key||FinishTypeRegistry.Key(s.PayDescription)==key)return s;return null;}
}
}
