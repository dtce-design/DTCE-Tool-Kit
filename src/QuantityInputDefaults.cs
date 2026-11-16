using System;using System.Linq;
namespace BeamScheduleFormatter {
// Only the count/multiplier of measured items is manual. Reinforcement layers,
// hardware per unit and derived output quantities keep their existing rules.
static class QuantityInputDefaults {
 internal static bool IsMultiplier(string header){string value=(header??"").ToLowerInvariant();return (value.Contains("qty")||value.Contains("quantity")||value.Trim()=="pieces"||value.Trim()=="runs")&&!new[]{"per ","/unit","layer","bar count","tie count","buy","purchase"}.Any(value.Contains);}
}
}
