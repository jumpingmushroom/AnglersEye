using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class CatchForecastTests
    {
        private static readonly RodParams Rod = new RodParams();
        private static readonly FishParams Fish = new FishParams();

        [Fact]
        public void SkillZero_Level1_Unhooked()
        {
            Forecast f = CatchForecast.Estimate(Rod, Fish, 1, 10.5f, 0f, 1000f, false, 0f);
            Assert.Equal(133.03, f.Cost, 2);
        }

        [Fact]
        public void MaxSkill_IsMuchCheaper()
        {
            Forecast f = CatchForecast.Estimate(Rod, Fish, 1, 10.5f, 1f, 1000f, false, 0f);
            Assert.Equal(13.55, f.Cost, 2);
        }

        [Fact]
        public void Level3_Hooked_UsesRemainingStruggleInsteadOfAFreshOne()
        {
            Forecast f = CatchForecast.Estimate(Rod, Fish, 3, 10.5f, 0f, 1000f, true, 1f);
            Assert.Equal(157.84, f.Cost, 2);
        }

        [Fact]
        public void HalfSkill_LerpsEveryTerm()
        {
            Forecast f = CatchForecast.Estimate(Rod, Fish, 1, 10.5f, 0.5f, 1000f, false, 0f);
            Assert.Equal(53.71, f.Cost, 2);
        }

        [Theory]
        [InlineData(200f, Verdict.Likely)]
        [InlineData(150f, Verdict.Tight)]
        [InlineData(100f, Verdict.Unlikely)]
        public void Verdict_FollowsMargin(float stamina, Verdict expected)
        {
            Assert.Equal(expected, CatchForecast.Estimate(Rod, Fish, 1, 10.5f, 0f, stamina, false, 0f).Verdict);
        }

        [Fact]
        public void Judge_Boundaries()
        {
            Assert.Equal(Verdict.Likely, CatchForecast.Judge(100f, 125f));
            Assert.Equal(Verdict.Tight, CatchForecast.Judge(100f, 100f));
            Assert.Equal(Verdict.Unlikely, CatchForecast.Judge(100f, 99.9f));
        }

        [Fact]
        public void AlreadyLanded_CostsOnlyTheRemainingStruggle()
        {
            Forecast f = CatchForecast.Estimate(Rod, Fish, 1, 0.5f, 0f, 10f, true, 0f);
            Assert.Equal(0.0, f.Cost, 3);
            Assert.Equal(Verdict.Likely, f.Verdict);
        }

        [Fact]
        public void NeverCalm_IsUnlikely()
        {
            var restless = new FishParams { EscapeWaitMin = 0f, EscapeWaitMax = 0f };
            Forecast f = CatchForecast.Estimate(Rod, restless, 1, 10.5f, 0f, 1000f, true, 0f);
            Assert.Equal(Verdict.Unlikely, f.Verdict);
        }

        [Fact]
        public void QualityBelowOne_TreatedAsOne()
        {
            Assert.Equal(CatchForecast.Estimate(Rod, Fish, 1, 10.5f, 0f, 1000f, false, 0f).Cost,
                         CatchForecast.Estimate(Rod, Fish, 0, 10.5f, 0f, 1000f, false, 0f).Cost);
        }
    }
}
