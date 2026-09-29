using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class StruggleTrackerTests
    {
        [Fact]
        public void NoCatch_IsZero()
        {
            var t = new StruggleTracker();
            Assert.Equal(0f, t.Update(false, false, 0f));
        }

        [Fact]
        public void Start_CapturesTheFirstRemaining_ThenHoldsWhileItDrains()
        {
            var t = new StruggleTracker();
            Assert.Equal(3f, t.Update(true, true, 3f));
            Assert.Equal(3f, t.Update(true, true, 2.5f));
            Assert.Equal(3f, t.Update(true, true, 0.1f));
        }

        [Fact]
        public void ReRoll_WhileEscaping_StartsANewTotal()
        {
            var t = new StruggleTracker();
            t.Update(true, true, 3f);
            t.Update(true, true, 0.2f);
            Assert.Equal(2f, t.Update(true, true, 2f));
            Assert.Equal(2f, t.Update(true, true, 1f));
        }

        [Fact]
        public void CalmGap_ThenANewStruggle_StartsANewTotal()
        {
            var t = new StruggleTracker();
            t.Update(true, true, 3f);
            t.Update(true, true, 0.5f);
            t.Update(true, false, 0f);
            t.Update(true, false, 0f);
            // A shorter struggle than the last one: still captured, because escaping turned on.
            Assert.Equal(1.2f, t.Update(true, true, 1.2f));
            Assert.Equal(1.2f, t.Update(true, true, 0.6f));
        }

        [Fact]
        public void CatchReleased_ResetsToZero_AndTheNextCatchStartsFresh()
        {
            var t = new StruggleTracker();
            t.Update(true, true, 3f);
            Assert.Equal(0f, t.Update(false, false, 0f));
            Assert.Equal(1.5f, t.Update(true, true, 1.5f));
        }

        [Fact]
        public void CatchLostMidStruggle_NextHookCountsAsAStart()
        {
            var t = new StruggleTracker();
            t.Update(true, true, 3f);
            t.Update(true, true, 2.9f);
            t.Update(false, false, 0f);
            // Same frame pattern as the lost fish, but lower remaining: must still be captured.
            Assert.Equal(2f, t.Update(true, true, 2f));
        }
    }
}
