using Xunit;
using AdventureGame;

namespace AdventureGame.Tests
{
    public class RoomTests
    {
        [Fact]
        public void Ctor_SetsExpectedDefaults()
        {
            var r = new Room();

            Assert.False(r.IsLit());
            Assert.False(r.HasLamp());
            Assert.False(r.HasKey());
            Assert.False(r.HasChest());
            Assert.False(r.HasNorth());
            Assert.False(r.HasSouth());
            Assert.False(r.HasEast());
            Assert.False(r.HasWest());
            Assert.Equal(string.Empty, r.GetDescription());

            var expected = "Room[isLit=False, hasLamp=False, hasKey=False, hasChest=False, hasNorth=False, hasSouth=False, hasEast=False, hasWest=False, description=]";
            Assert.Equal(expected, r.ToString());
        }

        [Fact]
        public void SetLit_ReflectsInGetter()
        {
            var r = new Room();
            r.SetLit(true);
            Assert.True(r.IsLit());
            r.SetLit(false);
            Assert.False(r.IsLit());
        }

        [Fact]
        public void SetLamp_ReflectsInGetter()
        {
            var r = new Room();
            r.SetLamp(true);
            Assert.True(r.HasLamp());
            r.SetLamp(false);
            Assert.False(r.HasLamp());
        }

        [Fact]
        public void SetKey_ReflectsInGetter()
        {
            var r = new Room();
            r.SetKey(true);
            Assert.True(r.HasKey());
            r.SetKey(false);
            Assert.False(r.HasKey());
        }

        [Fact]
        public void SetChest_ReflectsInGetter()
        {
            var r = new Room();
            r.SetChest(true);
            Assert.True(r.HasChest());
            r.SetChest(false);
            Assert.False(r.HasChest());
        }

        [Fact]
        public void SetNorth_ReflectsInGetter()
        {
            var r = new Room();
            r.SetNorth(true);
            Assert.True(r.HasNorth());
            r.SetNorth(false);
            Assert.False(r.HasNorth());
        }

        [Fact]
        public void SetSouth_ReflectsInGetter()
        {
            var r = new Room();
            r.SetSouth(true);
            Assert.True(r.HasSouth());
            r.SetSouth(false);
            Assert.False(r.HasSouth());
        }

        [Fact]
        public void SetEast_ReflectsInGetter()
        {
            var r = new Room();
            r.SetEast(true);
            Assert.True(r.HasEast());
            r.SetEast(false);
            Assert.False(r.HasEast());
        }

        [Fact]
        public void SetWest_ReflectsInGetter()
        {
            var r = new Room();
            r.SetWest(true);
            Assert.True(r.HasWest());
            r.SetWest(false);
            Assert.False(r.HasWest());
        }

        [Theory]
        [InlineData("")]
        [InlineData("A dim, dusty chamber.")]
        [InlineData("Line1\nLine2")]
        public void SetDescription_Then_GetDescription_ReturnsSame(string desc)
        {
            var r = new Room();
            r.SetDescription(desc);
            Assert.Equal(desc, r.GetDescription());
        }

        [Fact]
        public void SetDescription_AllowsNull()
        {
            var r = new Room();
#pragma warning disable CS8625 // Allow passing null to SetDescription for test
            r.SetDescription(null);
#pragma warning restore CS8625
            Assert.Null(r.GetDescription()); // current implementation stores null as-is
            // Ensure ToString() includes "description=" followed by nothing (prints "description=" + null -> "description=")
            Assert.Contains("description=", r.ToString());
        }

        [Fact]
        public void Flags_AreIndependent()
        {
            var r = new Room();

            r.SetLit(true);
            r.SetLamp(true);
            r.SetKey(false);
            r.SetChest(true);
            r.SetNorth(false);
            r.SetSouth(true);
            r.SetEast(false);
            r.SetWest(true);

            Assert.True(r.IsLit());
            Assert.True(r.HasLamp());
            Assert.False(r.HasKey());
            Assert.True(r.HasChest());
            Assert.False(r.HasNorth());
            Assert.True(r.HasSouth());
            Assert.False(r.HasEast());
            Assert.True(r.HasWest());
        }

        [Fact]
        public void ToString_ReflectsCurrentState()
        {
            var r = new Room();

            r.SetLit(true);
            r.SetLamp(true);
            r.SetKey(true);
            r.SetChest(false);
            r.SetNorth(true);
            r.SetSouth(false);
            r.SetEast(true);
            r.SetWest(false);
            r.SetDescription("Hallway");

            var s = r.ToString();

            Assert.Equal(
                "Room[isLit=True, hasLamp=True, hasKey=True, hasChest=False, hasNorth=True, hasSouth=False, hasEast=True, hasWest=False, description=Hallway]",
                s);
        }
    }
}
