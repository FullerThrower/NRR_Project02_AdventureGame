using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using AdventureGame;

namespace AdventureGame.Tests
{
    public class AdventureGameCoreTests
    {
        // ---- Reflection helpers -------------------------------------------------
        private static T GetField<T>(object target, string name)
        {
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(f);
            return (T)f!.GetValue(target)!;
        }

        private static void SetField<T>(object target, string name, T value)
        {
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(f);
            f!.SetValue(target, value);
        }

        private static object? Call(object target, string name, params object?[] args)
        {
            var m = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(m);
            return m!.Invoke(target, args);
        }

        private static string CaptureOut(Action act)
        {
            var sw = new StringWriter();
            var original = Console.Out;
            Console.SetOut(sw);
            try { act(); }
            finally { Console.SetOut(original); }
            return sw.ToString();
        }

        // ---- Constants accessors (public readonly) ------------------------------
        private static string Const(object g, string field) =>
            (string)g.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public)!.GetValue(g)!;

        // ---- Test cases ---------------------------------------------------------

        [Fact]
        public void Init_SetsExpectedWorldAndState()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            // Adventurer starts at [1,0] (Room 3)
            Assert.Equal(1, GetField<int>(g, "aRow"));
            Assert.Equal(0, GetField<int>(g, "aCol"));

            var dungeon = GetField<Room[,]>(g, "dungeon");
            var r1 = dungeon[0, 0];
            var r2 = dungeon[0, 1];
            var r3 = dungeon[1, 0];
            var r4 = dungeon[1, 1];

            Assert.True(r1.IsLit());
            Assert.True(r1.HasSouth());
            Assert.True(r1.HasEast());
            Assert.True(r1.HasLamp());
            Assert.True(r1.HasKey());
            Assert.Equal("Room 1", r1.GetDescription());

            Assert.False(r2.IsLit());
            Assert.True(r2.HasSouth());
            Assert.True(r2.HasWest());
            Assert.Equal("Room 2", r2.GetDescription());

            Assert.True(r3.IsLit());
            Assert.True(r3.HasNorth());
            Assert.True(r3.HasEast());
            Assert.True(r3.HasChest());
            Assert.Equal("Room 3", r3.GetDescription());

            Assert.False(r4.IsLit());
            Assert.True(r4.HasNorth());
            Assert.True(r4.HasWest());
            Assert.Equal("Room 4", r4.GetDescription());

            Assert.False(GetField<bool>(g, "isChestOpen"));
            Assert.False(GetField<bool>(g, "hasPlayerQuit"));
            Assert.True(GetField<bool>(g, "isAdventureAlive"));
            Assert.Equal(string.Empty, GetField<string>(g, "lastDirection"));
        }

        [Fact]
        public void ShowScene_PrintsDescriptionIfLit_OtherwisePitchBlack()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            // Start in Room 3 (lit)
            var out1 = CaptureOut(() => Call(g, "ShowScene"));
            Assert.Contains("Room 3", out1);

            // Move to Room 2 (dark) and ensure no lamp: W to r1, D to r2
            var GO_NORTH = Const(g, "GO_NORTH");
            var GO_EAST  = Const(g, "GO_EAST");
            Call(g, "ProcessInput", GO_NORTH); // to r1
            Call(g, "ProcessInput", GO_EAST);  // to r2 (dark)
            var out2 = CaptureOut(() => Call(g, "ShowScene"));
            Assert.Contains("pitch black", out2);
        }

        [Fact]
        public void Movement_UpdatesPositionAndLastDirection()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var GO_NORTH = Const(g, "GO_NORTH");
            var GO_SOUTH = Const(g, "GO_SOUTH");
            var GO_EAST  = Const(g, "GO_EAST");
            var GO_WEST  = Const(g, "GO_WEST");

            // From [1,0] -> Go North to [0,0]
            Call(g, "ProcessInput", GO_NORTH);
            Assert.Equal(0, GetField<int>(g, "aRow"));
            Assert.Equal(0, GetField<int>(g, "aCol"));
            Assert.Equal(GO_SOUTH, GetField<string>(g, "lastDirection"));

            // From [0,0] -> Go East to [0,1]
            Call(g, "ProcessInput", GO_EAST);
            Assert.Equal(0, GetField<int>(g, "aRow"));
            Assert.Equal(1, GetField<int>(g, "aCol"));
            Assert.Equal(GO_WEST, GetField<string>(g, "lastDirection"));

            // From [0,1] -> West back to [0,0]
            Call(g, "ProcessInput", GO_WEST);
            Assert.Equal(0, GetField<int>(g, "aRow"));
            Assert.Equal(0, GetField<int>(g, "aCol"));
            Assert.Equal(GO_EAST, GetField<string>(g, "lastDirection"));
        }

        [Fact]
        public void InvalidMove_PrintsErrorAndPositionUnchanged()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            // From start [1,0] (Room 3) has no West exit.
            var GO_WEST = Const(g, "GO_WEST");

            var beforeRow = GetField<int>(g, "aRow");
            var beforeCol = GetField<int>(g, "aCol");

            var output = CaptureOut(() => Call(g, "ProcessInput", GO_WEST));
            Assert.Contains("cannot go west", output);
            Assert.Equal(beforeRow, GetField<int>(g, "aRow"));
            Assert.Equal(beforeCol, GetField<int>(g, "aCol"));
        }

        [Fact]
        public void GetLampAndKey_WorkAndRemoveItemsFromRoom()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var GO_NORTH = Const(g, "GO_NORTH");
            var GET_LAMP = Const(g, "GET_LAMP");
            var GET_KEY  = Const(g, "GET_KEY");

            // Go to Room 1 where lamp & key are
            Call(g, "ProcessInput", GO_NORTH);
            var dungeon = GetField<Room[,]>(g, "dungeon");
            var r1 = dungeon[0, 0];

            Assert.True(r1.HasLamp());
            Assert.True(r1.HasKey());

            var out1 = CaptureOut(() => Call(g, "ProcessInput", GET_LAMP));
            Assert.Contains("You got the lamp!", out1);
            Assert.True(GetField<Adventurer>(g, "adventurer").HasLamp());
            Assert.False(r1.HasLamp()); // removed from room

            var out2 = CaptureOut(() => Call(g, "ProcessInput", GET_KEY));
            Assert.Contains("You got the key!", out2);
            Assert.True(GetField<Adventurer>(g, "adventurer").HasKey());
            Assert.False(r1.HasKey()); // removed from room
        }

        [Fact]
        public void OpenChest_NoKey_ShowsWarning_DoesNotEndGame()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var OPEN_CHEST = Const(g, "OPEN_CHEST");

            // Start is Room 3 (has chest), no key yet
            var out1 = CaptureOut(() => Call(g, "ProcessInput", OPEN_CHEST));
            Assert.Contains("do not have the key", out1);
            Assert.False(GetField<bool>(g, "isChestOpen"));
            Assert.True(GetField<bool>(g, "isAdventureAlive"));
            Assert.False(GetField<bool>(g, "hasPlayerQuit"));

            // Verify IsGameOver() is false
            var over = (bool)Call(g, "IsGameOver")!;
            Assert.False(over);
        }

        [Fact]
        public void OpenChest_WithKey_WinsGame()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var GO_NORTH = Const(g, "GO_NORTH");
            var GO_SOUTH = Const(g, "GO_SOUTH");
            var GET_KEY  = Const(g, "GET_KEY");
            var OPEN_CHEST = Const(g, "OPEN_CHEST");

            // Go to Room1, grab key, back to Room3, open chest
            Call(g, "ProcessInput", GO_NORTH);
            Call(g, "ProcessInput", GET_KEY);
            Call(g, "ProcessInput", GO_SOUTH);

            var output = CaptureOut(() => Call(g, "ProcessInput", OPEN_CHEST));
            Assert.Contains("You got the treasure!", output);
            Assert.True(GetField<bool>(g, "isChestOpen"));
            Assert.True((bool)Call(g, "IsGameOver")!);
        }

        [Fact]
        public void Grue_Death_WhenInDarkNoLampAndNotBacktracking()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var GO_NORTH = Const(g, "GO_NORTH");
            var GO_EAST  = Const(g, "GO_EAST");
            var GO_WEST  = Const(g, "GO_WEST");

            // Move to Room1 (lit), then East to Room2 (dark), lastDirection becomes GO_WEST.
            Call(g, "ProcessInput", GO_NORTH); // to r1 (safe)
            Call(g, "ProcessInput", GO_EAST);  // to r2 (dark), lastDirection = GO_WEST

            // In dark room with no lamp; issuing a command not equal to lastDirection triggers Grue.
            var out1 = CaptureOut(() => Call(g, "ProcessInput", GO_EAST)); // not backtracking
            Assert.Contains("eaten alive", out1);
            Assert.False(GetField<bool>(g, "isAdventureAlive"));
            Assert.True((bool)Call(g, "IsGameOver")!);

            // Ensure backtracking would have been allowed (sanity): reset and try GO_WEST
            Call(g, "Init");
            Call(g, "ProcessInput", GO_NORTH);
            Call(g, "ProcessInput", GO_EAST); // in dark again, lastDirection = GO_WEST
            var out2 = CaptureOut(() => Call(g, "ProcessInput", GO_WEST)); // backtracking equals lastDirection
            Assert.DoesNotContain("eaten alive", out2);
            Assert.True(GetField<bool>(g, "isAdventureAlive"));
        }

        [Fact]
        public void Quit_SetsFlagAndEndsGame()
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var QUIT = Const(g, "QUIT");
            var output = CaptureOut(() => Call(g, "ProcessInput", QUIT));

            Assert.Contains("quit the game", output);
            Assert.True(GetField<bool>(g, "hasPlayerQuit"));
            Assert.True((bool)Call(g, "IsGameOver")!);
        }

        [Theory]
        [InlineData("X")]
        [InlineData("")]
        [InlineData(" north ")]
        public void IsValidInput_InvalidInputs_PrintError_AndReturnFalse(string raw)
        {
            var g = new AdventureGame();
            Call(g, "Init");

            var outText = CaptureOut(() =>
            {
                var result = (bool)Call(g, "IsValidInput", raw)!;
                Assert.False(result);
            });

            Assert.Contains("Invalid input", outText);
        }

        [Fact]
        public void ShowGameStartAndGameOver_SideEffectsArePrinted()
        {
            var g = new AdventureGame();

            var startOut = CaptureOut(() => Call(g, "ShowGameStartScreen"));
            Assert.Contains("Welcome to Adventure Game!", startOut);

            var endOut = CaptureOut(() => Call(g, "ShowGameOverScreen"));
            Assert.Contains("Game Over!", endOut);
        }

        [Fact]
        public void Start_FullHappyPath_WinByTreasure()
        {
            // Inputs: W (to Room1), L, K, S (back to Room3), O (open chest)
            var inputs = string.Join(Environment.NewLine, new[] { "W", "L", "K", "S", "O" }) + Environment.NewLine;
            Console.SetIn(new StringReader(inputs));

            var outWriter = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(outWriter);

            try
            {
                var g = new AdventureGame();
                g.Start();
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var output = outWriter.ToString();
            Assert.Contains("Welcome to Adventure Game!", output);
            Assert.Contains("Room 3", output);          // first scene
            Assert.Contains("Room 1", output);          // after going north
            Assert.Contains("You got the lamp!", output);
            Assert.Contains("You got the key!", output);
            Assert.Contains("You got the treasure!", output);
            Assert.Contains("Game Over!", output);
        }
    }
}
