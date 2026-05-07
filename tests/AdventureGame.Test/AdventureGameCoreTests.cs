using System;
using System.IO;
using System.Reflection;
using Xunit;
using AdventureGame;

namespace AdventureGame.Tests
{
	public class AdventureGameCoreTests
	{
		private static T GetField<T>(object target, string name)
		{
			var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.NotNull(f);
			return (T)f!.GetValue(target)!;
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

		private static string Const(object g, string field) =>
			(string)g.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public)!.GetValue(g)!;

		[Fact]
		public void Init_LoadsDungeonTemplateAndState()
		{
			var g = new AdventureGame();
			Call(g, "Init");

			Assert.Equal(1, GetField<int>(g, "aRow"));
			Assert.Equal(1, GetField<int>(g, "aCol"));
			Assert.Equal(1, GetField<int>(g, "exitRow"));
			Assert.Equal(5, GetField<int>(g, "exitCol"));
			Assert.Equal(3, GetField<int>(g, "grueRow"));
			Assert.Equal(1, GetField<int>(g, "grueCol"));

			var dungeon = GetField<Room[,]>(g, "dungeon");

			Assert.True(dungeon[1, 1].HasLamp());
			Assert.True(dungeon[1, 3].HasKey());
			Assert.True(dungeon[3, 4].HasChest());
			Assert.Null(dungeon[0, 0]);
			Assert.False(GetField<bool>(g, "isChestOpen"));
			Assert.False(GetField<bool>(g, "hasPlayerQuit"));
			Assert.True(GetField<bool>(g, "isAdventureAlive"));
			Assert.False(GetField<bool>(g, "hasPlayerWon"));
		}

		[Fact]
		public void ShowScene_PrintsDescriptionIfLit_OtherwisePitchBlack()
		{
			var g = new AdventureGame();
			Call(g, "Init");

			var out1 = CaptureOut(() => Call(g, "ShowScene"));
			Assert.Contains("Entrance hall", out1);

			Call(g, "ProcessInput", Const(g, "GO_EAST"));
			Call(g, "ProcessInput", Const(g, "GO_EAST"));
			Call(g, "ProcessInput", Const(g, "GO_SOUTH"));

			var out2 = CaptureOut(() => Call(g, "ShowScene"));
			Assert.Contains("pitch black", out2);
		}

		[Fact]
		public void Movement_UpdatesPositionAndLastDirection()
		{
			var g = new AdventureGame();
			Call(g, "Init");

			var GO_EAST = Const(g, "GO_EAST");
			var GO_WEST = Const(g, "GO_WEST");

			Call(g, "ProcessInput", GO_EAST);
			Assert.Equal(1, GetField<int>(g, "aRow"));
			Assert.Equal(2, GetField<int>(g, "aCol"));
			Assert.Equal(GO_WEST, GetField<string>(g, "lastDirection"));

			Call(g, "ProcessInput", GO_WEST);
			Assert.Equal(1, GetField<int>(g, "aRow"));
			Assert.Equal(1, GetField<int>(g, "aCol"));
			Assert.Equal(GO_EAST, GetField<string>(g, "lastDirection"));
		}

		[Fact]
		public void InvalidMove_PrintsErrorAndPositionUnchanged()
		{
			var g = new AdventureGame();
			Call(g, "Init");

			var beforeRow = GetField<int>(g, "aRow");
			var beforeCol = GetField<int>(g, "aCol");

			var output = CaptureOut(() => Call(g, "ProcessInput", Const(g, "GO_NORTH")));

			Assert.Contains("cannot go north", output);
			Assert.Equal(beforeRow, GetField<int>(g, "aRow"));
			Assert.Equal(beforeCol, GetField<int>(g, "aCol"));
		}

		[Fact]
		public void GetLampAndKey_WorkAndRemoveItemsFromRoom()
		{
			var g = new AdventureGame();
			Call(g, "Init");

			var GET_LAMP = Const(g, "GET_LAMP");
			var GET_KEY = Const(g, "GET_KEY");
			var GO_EAST = Const(g, "GO_EAST");
			var dungeon = GetField<Room[,]>(g, "dungeon");

			var out1 = CaptureOut(() => Call(g, "ProcessInput", GET_LAMP));
			Assert.Contains("You got the lamp!", out1);
			Assert.True(GetField<Adventurer>(g, "adventurer").HasLamp());
			Assert.False(dungeon[1, 1].HasLamp());

			Call(g, "ProcessInput", GO_EAST);
			Call(g, "ProcessInput", GO_EAST);

			var out2 = CaptureOut(() => Call(g, "ProcessInput", GET_KEY));
			Assert.Contains("You got the key!", out2);
			Assert.True(GetField<Adventurer>(g, "adventurer").HasKey());
			Assert.False(dungeon[1, 3].HasKey());
		}

		[Fact]
		public void OpenChest_NoKey_ShowsWarning_DoesNotEndGame()
		{
			var g = new AdventureGame();
			Call(g, "Init");
			Call(g, "ProcessInput", Const(g, "GET_LAMP"));
			MoveToChest(g);

			var output = CaptureOut(() => Call(g, "ProcessInput", Const(g, "OPEN_CHEST")));

			Assert.Contains("do not have the key", output);
			Assert.False(GetField<bool>(g, "isChestOpen"));
			Assert.True(GetField<bool>(g, "isAdventureAlive"));
			Assert.False((bool)Call(g, "IsGameOver")!);
		}

		[Fact]
		public void OpenChest_WithKey_DoesNotWinUntilAdventurerReachesExit()
		{
			var g = new AdventureGame();
			Call(g, "Init");
			GetKeyAndMoveToChest(g);

			var output = CaptureOut(() => Call(g, "ProcessInput", Const(g, "OPEN_CHEST")));
			Assert.Contains("You got the treasure", output);
			Assert.True(GetField<bool>(g, "isChestOpen"));

			Call(g, "UpdateGameState");
			Assert.False((bool)Call(g, "IsGameOver")!);

			MoveAndUpdate(g, Const(g, "GO_EAST"));
			MoveAndUpdate(g, Const(g, "GO_NORTH"));
			var winOutput = CaptureOut(() => MoveAndUpdate(g, Const(g, "GO_NORTH")));

			Assert.Contains("escaped the dungeon", winOutput);
			Assert.True(GetField<bool>(g, "hasPlayerWon"));
			Assert.True((bool)Call(g, "IsGameOver")!);
		}

		[Fact]
		public void Grue_Death_WhenInDarkNoLampAndNotBacktracking()
		{
			var g = new AdventureGame();
			Call(g, "Init");

			Call(g, "ProcessInput", Const(g, "GO_EAST"));
			Call(g, "ProcessInput", Const(g, "GO_EAST"));
			Call(g, "ProcessInput", Const(g, "GO_SOUTH"));

			var out1 = CaptureOut(() => Call(g, "ProcessInput", Const(g, "GO_EAST")));
			Assert.Contains("eaten alive", out1);
			Assert.False(GetField<bool>(g, "isAdventureAlive"));

			Call(g, "Init");
			Call(g, "ProcessInput", Const(g, "GO_EAST"));
			Call(g, "ProcessInput", Const(g, "GO_EAST"));
			Call(g, "ProcessInput", Const(g, "GO_SOUTH"));

			var out2 = CaptureOut(() => Call(g, "ProcessInput", Const(g, "GO_NORTH")));
			Assert.DoesNotContain("eaten alive", out2);
			Assert.True(GetField<bool>(g, "isAdventureAlive"));
		}

		[Fact]
		public void Grue_PursuesAfterChestOpen_AndCatchesAdventurerInSameRoom()
		{
			var g = new AdventureGame();
			Call(g, "Init");
			GetKeyAndMoveToChest(g);

			Call(g, "ProcessInput", Const(g, "OPEN_CHEST"));
			Call(g, "UpdateGameState");

			var output = CaptureOut(() => MoveAndUpdate(g, Const(g, "GO_WEST")));

			Assert.Contains("Grue caught you", output);
			Assert.False(GetField<bool>(g, "isAdventureAlive"));
			Assert.True((bool)Call(g, "IsGameOver")!);
		}

		[Fact]
		public void Quit_SetsFlagAndEndsGame()
		{
			var g = new AdventureGame();
			Call(g, "Init");

			var output = CaptureOut(() => Call(g, "ProcessInput", Const(g, "QUIT")));

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
		public void Start_FullHappyPath_WinByEscapingAfterTreasure()
		{
			var inputs = string.Join(Environment.NewLine, new[] { "L", "D", "D", "K", "S", "S", "D", "O", "D", "W", "W" }) + Environment.NewLine;
			var originalIn = Console.In;
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
				Console.SetIn(originalIn);
				Console.SetOut(originalOut);
			}

			var output = outWriter.ToString();
			Assert.Contains("Welcome to Adventure Game!", output);
			Assert.Contains("Entrance hall", output);
			Assert.Contains("You got the lamp!", output);
			Assert.Contains("You got the key!", output);
			Assert.Contains("You got the treasure", output);
			Assert.Contains("You escaped the dungeon with the treasure!", output);
			Assert.Contains("You win!", output);
		}

		private static void MoveToChest(AdventureGame g)
		{
			Call(g, "ProcessInput", Const(g, "GO_EAST"));
			Call(g, "ProcessInput", Const(g, "GO_EAST"));
			Call(g, "ProcessInput", Const(g, "GO_SOUTH"));
			Call(g, "ProcessInput", Const(g, "GO_SOUTH"));
			Call(g, "ProcessInput", Const(g, "GO_EAST"));
		}

		private static void GetKeyAndMoveToChest(AdventureGame g)
		{
			Call(g, "ProcessInput", Const(g, "GET_LAMP"));
			Call(g, "ProcessInput", Const(g, "GO_EAST"));
			Call(g, "ProcessInput", Const(g, "GO_EAST"));
			Call(g, "ProcessInput", Const(g, "GET_KEY"));
			Call(g, "ProcessInput", Const(g, "GO_SOUTH"));
			Call(g, "ProcessInput", Const(g, "GO_SOUTH"));
			Call(g, "ProcessInput", Const(g, "GO_EAST"));
		}

		private static void MoveAndUpdate(AdventureGame g, string direction)
		{
			Call(g, "ProcessInput", direction);
			Call(g, "UpdateGameState");
		}
	}
}
