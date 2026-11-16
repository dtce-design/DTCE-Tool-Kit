using System;using System.Linq;
namespace BeamScheduleFormatter {
// Only the count/multiplier of measured items is manual. Reinforcement layers,
// hardware per unit and derived output quantities keep their existing rules.
static class QuantityInputDefaults {
 internal static bool IsNumber(object value){double number;return value is byte||value is short||value is int||value is long||value is float||value is double||value is decimal||value!=null&&double.TryParse(Convert.ToString(value),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out number);}
 internal static bool IsMultiplier(string header){string value=(header??"").ToLowerInvariant();return (value.Contains("qty")||value.Contains("quantity")||value.Trim()=="pieces"||value.Trim()=="runs")&&!new[]{"per ","/unit","layer","bar count","tie count","buy","purchase"}.Any(value.Contains);}
}
}
