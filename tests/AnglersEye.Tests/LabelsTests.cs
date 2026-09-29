using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class LabelsTests
    {
        private static readonly Glyphs G = Glyphs.Unicode();
        private static readonly BaitOption Cold = new BaitOption("FishingBaitCold", "Cold bait", 0.6f);
        private static readonly BaitAdvice Carried = new BaitAdvice { Best = Cold, Carried = true, CarriedCount = 12 };
        private static readonly BaitAdvice Missing = new BaitAdvice { Best = Cold, Carried = false, CarriedCount = 0 };

        [Fact]
        public void Stars_AreQualityMinusOne()
        {
            Assert.Equal("", Labels.Stars(1, G));
            Assert.Equal("★★", Labels.Stars(3, G));
            Assert.Equal("Pike ★", Labels.WithStars("Pike", 2, G));
            Assert.Equal("Pike", Labels.WithStars("Pike", 1, G));
        }

        [Fact]
        public void Bait_CarriedAndMissing_WithOptionalOdds()
        {
            Assert.Equal("Cold bait ✔ (x12)", Labels.Bait(Carried, G, false));
            Assert.Equal("Cold bait 60% ✔ (x12)", Labels.Bait(Carried, G, true));
            Assert.Equal("Cold bait ✖", Labels.Bait(Missing, G, false));
        }

        [Fact]
        public void HoverSuffix_StarsThenBaitLine()
        {
            Assert.Equal(" ★★\nBait: Cold bait ✔ (x12)", Labels.HoverSuffix(3, Carried, G, false));
            Assert.Equal("\nBait: Cold bait ✖", Labels.HoverSuffix(1, Missing, G, false));
            Assert.Equal("", Labels.HoverSuffix(1, null, G, false));
        }

        [Fact]
        public void OnFloat_WorksOrNeeds()
        {
            Assert.Equal("Pike ★ · ✔", Labels.OnFloat("Pike", 2, true, "Cold bait", G));
            Assert.Equal("Pike · needs Cold bait ✖", Labels.OnFloat("Pike", 1, false, "Cold bait", G));
        }

        [Fact]
        public void Level_TextFallback()
        {
            Assert.Equal("", Labels.Level(1));
            Assert.Equal("", Labels.Level(0));
            Assert.Equal("Lv 3", Labels.Level(3));
        }

        [Fact]
        public void NeedsBait_Message()
        {
            Assert.Equal("Angler's Eye: Pike needs Cold bait", Labels.NeedsBait("Pike", "Cold bait"));
        }

        [Fact]
        public void Resolve_FallsBackPerGlyph()
        {
            Glyphs g = Glyphs.Resolve(c => c != '★' && c != '✔');
            Assert.Equal("*", g.Star);
            Assert.Equal("+", g.Yes);
            Assert.Equal("✖", g.No);
            Assert.Equal("·", g.Sep);
        }
    }
}
