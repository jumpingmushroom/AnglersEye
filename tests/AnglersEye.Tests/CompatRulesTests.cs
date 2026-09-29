using System.Collections.Generic;
using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class CompatRulesTests
    {
        private static CompatVerdict Eval(string[] guids, Dictionary<string, IList<string>> owners = null)
        {
            return CompatRules.Evaluate(new HashSet<string>(guids), owners ?? new Dictionary<string, IList<string>>());
        }

        [Fact]
        public void NothingInstalled_NothingOff()
        {
            CompatVerdict v = Eval(new[] { "com.jumpingmushroom.larder" });
            Assert.Equal(Feature.None, v.Disabled);
            Assert.Empty(v.Reasons);
        }

        [Fact]
        public void Hooked_TurnsOffAssistsForecastAndCues_KeepsId()
        {
            CompatVerdict v = Eval(new[] { CompatRules.Hooked });
            Assert.True(v.IsOff(Feature.SmartBait));
            Assert.True(v.IsOff(Feature.SmartReel));
            Assert.True(v.IsOff(Feature.HookWindow));
            Assert.True(v.IsOff(Feature.Forecast));
            Assert.True(v.IsOff(Feature.BiteCue));
            Assert.True(v.IsOff(Feature.StruggleIndicator));
            Assert.False(v.IsOff(Feature.HoverInfo));
            Assert.False(v.IsOff(Feature.FloatLabel));
        }

        [Fact]
        public void TrollingFishing_KeepsCues()
        {
            CompatVerdict v = Eval(new[] { CompatRules.TrollingFishing });
            Assert.True(v.IsOff(Feature.SmartBait));
            Assert.True(v.IsOff(Feature.SmartReel));
            Assert.True(v.IsOff(Feature.HookWindow));
            Assert.True(v.IsOff(Feature.Forecast));
            Assert.False(v.IsOff(Feature.BiteCue));
            Assert.False(v.IsOff(Feature.StruggleIndicator));
        }

        [Fact]
        public void ForeignPatches_TurnOffOnlyTheMatchingFeature()
        {
            var owners = new Dictionary<string, IList<string>>
            {
                { CompatRules.FixedUpdate, new List<string> { "games.loxley.comfyfishing" } },
                { CompatRules.TryToHook, new List<string>() },
                { CompatRules.GetStaminaUse, new List<string> { "com.orianaventure.mod.ReelyGoodRod" } }
            };
            CompatVerdict v = Eval(new string[0], owners);
            Assert.Equal(Feature.SmartReel | Feature.Forecast, v.Disabled);
            Assert.Contains(v.Reasons, r => r.Contains("games.loxley.comfyfishing") && r.Contains("smart reel"));
            Assert.Contains(v.Reasons, r => r.Contains("ReelyGoodRod") && r.Contains("forecast"));
        }

        [Fact]
        public void Describe_ListsFlagsInOrder()
        {
            Assert.Equal("smart bait, forecast, extended hook window",
                CompatRules.Describe(Feature.HookWindow | Feature.SmartBait | Feature.Forecast));
        }
    }
}
