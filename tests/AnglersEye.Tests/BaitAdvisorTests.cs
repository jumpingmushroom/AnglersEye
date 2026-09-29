using System.Collections.Generic;
using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class BaitAdvisorTests
    {
        private static readonly BaitOption Cold = new BaitOption("FishingBaitCold", "Cold bait", 0.6f);
        private static readonly BaitOption Hot = new BaitOption("FishingBaitHot", "Hot bait", 0.3f);

        private static Dictionary<string, int> Bag(params (string id, int n)[] items)
        {
            var d = new Dictionary<string, int>();
            foreach (var (id, n) in items) d[id] = n;
            return d;
        }

        [Fact]
        public void EmptyOrNullTable_ReturnsNull()
        {
            Assert.Null(BaitAdvisor.Advise(new BaitOption[0], Bag()));
            Assert.Null(BaitAdvisor.Advise(null, Bag()));
        }

        [Fact]
        public void PicksHighestChanceCarried()
        {
            BaitAdvice a = BaitAdvisor.Advise(new[] { Hot, Cold }, Bag(("FishingBaitCold", 3), ("FishingBaitHot", 10)));
            Assert.Equal("FishingBaitCold", a.Best.BaitId);
            Assert.True(a.Carried);
            Assert.Equal(3, a.CarriedCount);
        }

        [Fact]
        public void BetterBaitNotCarried_FallsBackToCarriedOne()
        {
            BaitAdvice a = BaitAdvisor.Advise(new[] { Cold, Hot }, Bag(("FishingBaitHot", 10)));
            Assert.Equal("FishingBaitHot", a.Best.BaitId);
            Assert.True(a.Carried);
            Assert.Equal(10, a.CarriedCount);
        }

        [Fact]
        public void NothingCarried_ReturnsBestOverall_NotCarried()
        {
            BaitAdvice a = BaitAdvisor.Advise(new[] { Hot, Cold }, Bag(("SomethingElse", 5)));
            Assert.Equal("FishingBaitCold", a.Best.BaitId);
            Assert.False(a.Carried);
            Assert.Equal(0, a.CarriedCount);
        }

        [Fact]
        public void ZeroCountIsNotCarried()
        {
            BaitAdvice a = BaitAdvisor.Advise(new[] { Cold }, Bag(("FishingBaitCold", 0)));
            Assert.False(a.Carried);
        }

        [Fact]
        public void EqualChance_LargerStackWins()
        {
            var a1 = new BaitOption("A", "A bait", 0.5f);
            var b1 = new BaitOption("B", "B bait", 0.5f);
            BaitAdvice a = BaitAdvisor.Advise(new[] { a1, b1 }, Bag(("A", 2), ("B", 9)));
            Assert.Equal("B", a.Best.BaitId);
        }

        [Fact]
        public void DuplicateEntries_CombineTheirChances()
        {
            var first = new BaitOption("A", "A bait", 0.5f);
            var second = new BaitOption("A", "A bait", 0.5f);
            BaitAdvice a = BaitAdvisor.Advise(new[] { first, second }, Bag(("A", 1)));
            Assert.Equal(0.75f, a.Best.Chance, 3);
        }
    }
}
