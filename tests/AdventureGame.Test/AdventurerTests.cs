using Xunit;
using AdventureGame;

namespace AdventureGame.Tests
{
    public class AdventurerTests
    {
        [Fact]
        public void Ctor_SetsExpectedDefaults()
        {
            var a = new Adventurer();

            Assert.False(a.HasLamp());
            Assert.False(a.HasKey());

            var expected = "Adventurer[hasLamp=False, hasKey=False]";
            Assert.Equal(expected, a.ToString());
        }

        [Fact]
        public void SetLamp_ReflectsInGetter()
        {
            var a = new Adventurer();

            a.SetLamp(true);
            Assert.True(a.HasLamp());

            a.SetLamp(false);
            Assert.False(a.HasLamp());
        }

        [Fact]
        public void SetKey_ReflectsInGetter()
        {
            var a = new Adventurer();

            a.SetKey(true);
            Assert.True(a.HasKey());

            a.SetKey(false);
            Assert.False(a.HasKey());
        }

        [Fact]
        public void LampAndKey_AreIndependent()
        {
            var a = new Adventurer();

            a.SetLamp(true);
            a.SetKey(false);
            Assert.True(a.HasLamp());
            Assert.False(a.HasKey());

            a.SetLamp(false);
            a.SetKey(true);
            Assert.False(a.HasLamp());
            Assert.True(a.HasKey());
        }

        [Fact]
        public void ToString_ReflectsCurrentState()
        {
            var a = new Adventurer();

            a.SetLamp(true);
            a.SetKey(true);

            var s = a.ToString();

            Assert.Equal("Adventurer[hasLamp=True, hasKey=True]", s);
        }
    }
}
