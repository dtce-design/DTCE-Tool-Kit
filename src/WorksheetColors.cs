using System.Drawing;
namespace BeamScheduleFormatter {
static class WorksheetColors {
 internal static Color Input {get{return DesktopAppearance.Palette.Input;}}
 internal static Color Output {get{return DesktopAppearance.Palette.Surface;}}
 internal static Color Selection {get{return DesktopAppearance.Palette.Selected;}}
 internal static Color Focus {get{return DesktopAppearance.Palette.Dark?Color.FromArgb(144,191,246):Color.FromArgb(37,91,157);}}
}
}
