using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class PanelViewTests
    {
        private static readonly BaitOption Cold = new BaitOption("FishingBaitCold", "Cold bait", 0.6f);
        private static readonly BaitAdvice Carried = new BaitAdvice { Best = Cold, Carried = true, CarriedCount = 12 };
        private static readonly BaitAdvice Missing = new BaitAdvice { Best = Cold, Carried = false, CarriedCount = 0 };

        [Fact]
        public void Target_CarriedBait_NormalWithCount()
        {
            PanelView v = PanelView.Target("Pike", 2, Carried, false);
            Assert.Equal("Pike", v.Title);
            Assert.Equal(2, v.Level);
            Assert.Equal("Cold bait (12)", v.Body);
            Assert.Equal(Tone.Normal, v.BodyTone);
            Assert.False(v.BodyIsBig);
            Assert.Null(v.Bar);
            Assert.Null(v.Forecast);
        }

        [Fact]
        public void Target_MissingBait_IsRed()
        {
            PanelView v = PanelView.Target("Pike", 1, Missing, false);
            Assert.Equal("needs Cold bait", v.Body);
            Assert.Equal(Tone.Bad, v.BodyTone);
        }

        [Fact]
        public void Target_Odds_FollowTheBaitName()
        {
            Assert.Equal("Cold bait 60% (12)", PanelView.Target("Pike", 1, Carried, true).Body);
            Assert.Equal("needs Cold bait 60%", PanelView.Target("Pike", 1, Missing, true).Body);
        }

        [Fact]
        public void Target_NoBaitTable_TitleOnly()
        {
            PanelView v = PanelView.Target("Pike", 3, null, true);
            Assert.Equal("Pike", v.Title);
            Assert.Equal(3, v.Level);
            Assert.Null(v.Body);
        }

        [Fact]
        public void Waiting_WithSubject_BodyAndForecast()
        {
            PanelView v = PanelView.Waiting("Pike", 2, Verdict.Tight);
            Assert.Equal("Pike", v.Title);
            Assert.Equal(2, v.Level);
            Assert.Equal("Waiting…", v.Body);
            Assert.Equal(Tone.Normal, v.BodyTone);
            Assert.False(v.BodyIsBig);
            Assert.Equal("tight", v.Forecast);
            Assert.Equal(Tone.Warn, v.ForecastTone);
        }

        [Fact]
        public void Waiting_ForecastOff_NoForecast()
        {
            PanelView v = PanelView.Waiting("Pike", 1, null);
            Assert.Equal("Waiting…", v.Body);
            Assert.Null(v.Forecast);
        }

        [Fact]
        public void Waiting_NoSubject_TitleAlone()
        {
            PanelView v = PanelView.Waiting(null, 0, null);
            Assert.Equal("Waiting…", v.Title);
            Assert.Equal(0, v.Level);
            Assert.Null(v.Body);
            Assert.Null(v.Forecast);
        }

        [Fact]
        public void Bite_BigYellow()
        {
            PanelView v = PanelView.Bite("Pike", 2);
            Assert.Equal("Pike", v.Title);
            Assert.Equal("BITE!", v.Body);
            Assert.True(v.BodyIsBig);
            Assert.Equal(Tone.Bite, v.BodyTone);
            Assert.Null(v.Bar);

            Assert.Null(PanelView.Bite(null, 0).Title);
        }

        [Fact]
        public void Hooked_Calm_GreenReel()
        {
            PanelView v = PanelView.Hooked("Pike", 2, false, true, 0f, 0f, Verdict.Likely);
            Assert.Equal("Pike", v.Title);
            Assert.Equal("REEL", v.Body);
            Assert.True(v.BodyIsBig);
            Assert.Equal(Tone.Good, v.BodyTone);
            Assert.Null(v.Bar);
            Assert.Equal("can land", v.Forecast);
            Assert.Equal(Tone.Good, v.ForecastTone);
        }

        [Fact]
        public void Hooked_Struggling_AmberWaitWithDrainingBar()
        {
            PanelView v = PanelView.Hooked("Pike", 1, true, true, 1.5f, 3f, Verdict.Unlikely);
            Assert.Equal("WAIT", v.Body);
            Assert.True(v.BodyIsBig);
            Assert.Equal(Tone.Warn, v.BodyTone);
            Assert.Equal(0.5f, v.Bar.Value, 3);
            Assert.Equal("unlikely", v.Forecast);
            Assert.Equal(Tone.Bad, v.ForecastTone);
        }

        [Fact]
        public void StruggleFraction_ClampedAndSafe()
        {
            Assert.Equal(0f, PanelView.StruggleFraction(1f, 0f));
            Assert.Equal(0f, PanelView.StruggleFraction(-1f, 3f));
            Assert.Equal(1f, PanelView.StruggleFraction(4f, 3f));
            Assert.Equal(0.25f, PanelView.StruggleFraction(0.75f, 3f), 3);
            Assert.Equal(0f, PanelView.Hooked("Pike", 1, true, true, 2f, 0f, null).Bar.Value);
        }

        [Fact]
        public void Hooked_StruggleIndicatorOff_ForecastOnly()
        {
            PanelView v = PanelView.Hooked("Pike", 1, true, false, 1f, 2f, Verdict.Tight);
            Assert.Equal("Pike", v.Title);
            Assert.Null(v.Body);
            Assert.Null(v.Bar);
            Assert.Equal("tight", v.Forecast);
        }

        [Fact]
        public void Hooked_ForecastOff_NoForecast()
        {
            PanelView v = PanelView.Hooked("Pike", 1, false, true, 0f, 0f, null);
            Assert.Equal("REEL", v.Body);
            Assert.Null(v.Forecast);
        }

        [Fact]
        public void Hooked_NothingToShow_IsNull()
        {
            Assert.Null(PanelView.Hooked("Pike", 1, true, false, 1f, 2f, null));
        }

        [Fact]
        public void ForecastWords()
        {
            Assert.Equal("can land", PanelView.WordFor(Verdict.Likely));
            Assert.Equal("tight", PanelView.WordFor(Verdict.Tight));
            Assert.Equal("unlikely", PanelView.WordFor(Verdict.Unlikely));
            Assert.Equal(Tone.Good, PanelView.ToneFor(Verdict.Likely));
            Assert.Equal(Tone.Warn, PanelView.ToneFor(Verdict.Tight));
            Assert.Equal(Tone.Bad, PanelView.ToneFor(Verdict.Unlikely));
        }
    }
}
