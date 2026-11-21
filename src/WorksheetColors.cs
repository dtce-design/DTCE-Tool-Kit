using System.Drawing;
namespace BeamScheduleFormatter {
static class WorksheetColors {
 /* inputs and computed cells clearly apart in every table (owner, 21 November 2026: "di ko naman na makita if anong cell ang input lang
    at cell na automatic computation"): pale yellow = you type here, pale green-grey = computed by the app */
 internal static Color Input {get{return DesktopAppearance.Palette.Dark?Color.FromArgb(60,56,38):Color.FromArgb(255,249,222);}}
 internal static Color Output {get{return DesktopAppearance.Palette.Dark?Color.FromArgb(34,46,42):Color.FromArgb(232,241,236);}}
 internal static Color Selection {get{return DesktopAppearance.Palette.Selected;}}
 internal static Color Focus {get{return DesktopAppearance.Palette.Dark?Color.FromArgb(144,191,246):Color.FromArgb(37,91,157);}}
}
}
