using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class ReelPolicyTests
    {
        [Theory]
        [InlineData(true, true, true, true)]
        [InlineData(true, true, false, false)]
        [InlineData(true, false, true, false)]
        [InlineData(false, true, true, false)]
        public void SuppressReel_OnlyWhenOnHookedAndStruggling(bool on, bool hooked, bool escaping, bool expected)
        {
            Assert.Equal(expected, ReelPolicy.SuppressReel(on, hooked, escaping));
        }

        [Theory]
        [InlineData(false, 1.2f, 0.5f)]
        [InlineData(true, 1.2f, 1.2f)]
        [InlineData(true, 0.1f, 0.5f)]
        [InlineData(true, 9f, 1.5f)]
        public void HookWindow_VanillaUnlessExtended_AndClamped(bool extended, float seconds, float expected)
        {
            Assert.Equal(expected, ReelPolicy.HookWindow(extended, seconds), 3);
        }

        [Fact]
        public void InHookWindow_IsStrictlyLessThan()
        {
            Assert.True(ReelPolicy.InHookWindow(10.49f, 10f, 0.5f));
            Assert.False(ReelPolicy.InHookWindow(10.5f, 10f, 0.5f));
        }
    }
}
