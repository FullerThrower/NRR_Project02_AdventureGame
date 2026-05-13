using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AdventureGame;

public class AdventureGame
{
	public readonly string GO_NORTH = "W";
	public readonly string GO_SOUTH = "S";
	public readonly string GO_EAST = "D";
	public readonly string GO_WEST = "A";
	public readonly string GET_LAMP = "L";
	public readonly string GET_KEY = "K";
	public readonly string OPEN_CHEST = "O";
	public readonly string QUIT = "Q";

	private Adventurer adventurer = default!;
	private Room[,] dungeon = default!;

	private int aRow;
	private int aCol;

	private int exitRow;
	private int exitCol;

	private int grueRow;
	private int grueCol;

	private bool isChestOpen;
	private bool isGrueChasing;
	private bool hasPlayerQuit;
	private bool isAdventureAlive;
	private bool hasPlayerWon;

	private string lastDirection = string.Empty;

	public AdventureGame()
	{

	}

	public void Start()
	{
		Init();

		ShowGameStartScreen();

		string input;

		do
		{
			ShowScene();

			do
			{
				ShowInputOptions();

				input = GetInput();
			}
			while (!IsValidInput(input));

			ProcessInput(input);

			UpdateGameState();
		}
		while (!IsGameOver());

		ShowGameOverScreen();
	}

	private void Init()
{
	adventurer = new Adventurer();

	string dungeonFilePath = ResolveDungeonFilePath("Dungeon01.txt");

	LoadDungeonFromFile(dungeonFilePath);

	isChestOpen = false;
	isGrueChasing = false;
	hasPlayerQuit = false;
	isAdventureAlive = true;
	hasPlayerWon = false;

	lastDirection = string.Empty;
}

private string ResolveDungeonFilePath(string fileName)
{
	string[] startingFolders =
	{
		AppContext.BaseDirectory,
		Directory.GetCurrentDirectory()
	};

	foreach (string startingFolder in startingFolders)
	{
		DirectoryInfo? folder = new DirectoryInfo(startingFolder);

		while (folder != null)
		{
			string possiblePath = Path.Combine(folder.FullName, "res", fileName);

			if (File.Exists(possiblePath))
			{
				return possiblePath;
			}

			folder = folder.Parent;
		}
	}

	throw new FileNotFoundException($"Could not find res/{fileName}. Make sure the file is inside your project's res folder.");
}

	private void LoadDungeonFromFile(string filePath)
	{
		string[] lines = File.ReadAllLines(filePath)
			.Select(line => line.Trim())
			.Where(line => line.Length > 0 && !line.StartsWith("//"))
			.ToArray();

		int mapIndex = Array.IndexOf(lines, "MAP");
		int descriptionsIndex = Array.IndexOf(lines, "DESCRIPTIONS");

		if (mapIndex == -1)
		{
			throw new FormatException("Dungeon file must contain a MAP section.");
		}

		if (descriptionsIndex == -1)
		{
			throw new FormatException("Dungeon file must contain a DESCRIPTIONS section.");
		}

		Dictionary<string, string> metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		for (int i = 0; i < mapIndex; i++)
		{
			int equalsIndex = lines[i].IndexOf('=');

			if (equalsIndex == -1)
			{
				throw new FormatException($"Invalid metadata line: {lines[i]}");
			}

			string key = lines[i].Substring(0, equalsIndex).Trim();
			string value = lines[i].Substring(equalsIndex + 1).Trim();

			metadata[key] = value;
		}

		int rows = int.Parse(metadata["ROWS"]);
		int cols = int.Parse(metadata["COLS"]);

		(int startRow, int startCol) = ParseCoordinates(metadata["START"]);
		(exitRow, exitCol) = ParseCoordinates(metadata["EXIT"]);

		(int lampRow, int lampCol) = ParseCoordinates(metadata["LAMP"]);
		(int keyRow, int keyCol) = ParseCoordinates(metadata["KEY"]);
		(int chestRow, int chestCol) = ParseCoordinates(metadata["CHEST"]);
		(grueRow, grueCol) = ParseCoordinates(metadata["GRUE"]);

		if (mapIndex + rows >= descriptionsIndex)
		{
			throw new FormatException("The number of MAP rows does not match ROWS.");
		}

		dungeon = new Room[rows, cols];

		for (int row = 0; row < rows; row++)
		{
			string mapLine = lines[mapIndex + 1 + row];

			if (mapLine.Length != cols)
			{
				throw new FormatException($"Map row {row} must have exactly {cols} characters.");
			}

			for (int col = 0; col < cols; col++)
			{
				char tile = mapLine[col];

				if (tile != '#')
				{
					Room room = new Room();
					room.SetLit(false);
					room.SetDescription("A quiet dungeon room.");
					dungeon[row, col] = room;
				}
			}
		}

		for (int i = descriptionsIndex + 1; i < lines.Length; i++)
		{
			string[] parts = lines[i].Split('|', 3);

			if (parts.Length != 3)
			{
				throw new FormatException($"Invalid description line: {lines[i]}");
			}

			(int row, int col) = ParseCoordinates(parts[0]);
			ValidateRoomCoordinate(row, col, "description");

			bool isLit = parts[1] == "1";
			string description = parts[2];

			dungeon[row, col].SetLit(isLit);
			dungeon[row, col].SetDescription(description);
		}

		ValidateRoomCoordinate(startRow, startCol, "adventurer start");
		ValidateRoomCoordinate(exitRow, exitCol, "exit");
		ValidateRoomCoordinate(lampRow, lampCol, "lamp");
		ValidateRoomCoordinate(keyRow, keyCol, "key");
		ValidateRoomCoordinate(chestRow, chestCol, "chest");
		ValidateRoomCoordinate(grueRow, grueCol, "Grue");

		aRow = startRow;
		aCol = startCol;

		dungeon[lampRow, lampCol].SetLamp(true);
		dungeon[keyRow, keyCol].SetKey(true);
		dungeon[chestRow, chestCol].SetChest(true);

		SetRoomConnections();
	}

	private (int row, int col) ParseCoordinates(string text)
	{
		string[] parts = text.Split(',');

		if (parts.Length != 2)
		{
			throw new FormatException($"Invalid coordinate format: {text}");
		}

		int row = int.Parse(parts[0]);
		int col = int.Parse(parts[1]);

		return (row, col);
	}

	private void ValidateRoomCoordinate(int row, int col, string name)
	{
		if (!IsValidRoom(row, col))
		{
			throw new FormatException($"The {name} coordinate must be inside a valid room.");
		}
	}

	private void SetRoomConnections()
	{
		for (int row = 0; row < dungeon.GetLength(0); row++)
		{
			for (int col = 0; col < dungeon.GetLength(1); col++)
			{
				if (dungeon[row, col] != null)
				{
					Room room = dungeon[row, col];

					room.SetNorth(IsValidRoom(row - 1, col));
					room.SetSouth(IsValidRoom(row + 1, col));
					room.SetEast(IsValidRoom(row, col + 1));
					room.SetWest(IsValidRoom(row, col - 1));
				}
			}
		}
	}

	private void ShowGameStartScreen()
	{
		Console.WriteLine("Welcome to Adventure Game!");
		Console.WriteLine("Find the lamp, get the key, open the chest, and escape through the dungeon exit.");
		Console.WriteLine("After opening the chest, the Grue will start chasing you!");
		Console.WriteLine();
	}

	private void ShowScene()
	{
		Room r = dungeon[aRow, aCol];

		Console.WriteLine("----------------------------------------");
		Console.WriteLine($"Adventurer Position: Row {aRow}, Col {aCol}");

		if (isGrueChasing)
		{
			Console.WriteLine($"Grue Position: Row {grueRow}, Col {grueCol}");
		}

		if (adventurer.HasLamp() || r.IsLit())
		{
			Console.WriteLine(r.GetDescription());

			if (aRow == exitRow && aCol == exitCol)
			{
				Console.WriteLine("You see the dungeon exit here.");
			}
		}
		else
		{
			Console.WriteLine("This room is pitch black!");
		}

		Console.WriteLine("----------------------------------------");
	}

	private void ShowInputOptions()
	{
		string options = ""
		+ $"GO NORTH [{GO_NORTH}] | GO EAST [{GO_EAST}] | GET LAMP [{GET_LAMP}] | OPEN CHEST [{OPEN_CHEST}]\n"
		+ $"GO SOUTH [{GO_SOUTH}] | GO WEST [{GO_WEST}] | GET KEY  [{GET_KEY}] | QUIT       [{QUIT}]\n"
		+ $"> ";

		Console.Write(options);
	}

	private string GetInput()
	{
		return (Console.ReadLine() ?? string.Empty).Trim().ToUpper();
	}

	private bool IsValidInput(string input)
	{
		string[] validInputs =
		{
			GO_NORTH,
			GO_SOUTH,
			GO_EAST,
			GO_WEST,
			GET_LAMP,
			GET_KEY,
			OPEN_CHEST,
			QUIT
		};

		if (!validInputs.Contains(input))
		{
			Console.WriteLine("ERROR: Invalid input. Please try again.");
			return false;
		}

		return true;
	}

	private void ProcessInput(string input)
	{
		Room r = dungeon[aRow, aCol];

		// WHAT: Keeps the original Grue darkness mechanic.
		// WHY: If the player enters an unlit room without the lamp, only going back is safe.
		if (!adventurer.HasLamp() && !r.IsLit() && input != lastDirection)
		{
			Console.WriteLine("You got eaten alive by the Grue in the dark!");
			isAdventureAlive = false;
		}
		else if (input == GO_NORTH)
		{
			GoNorth(r);
		}
		else if (input == GO_SOUTH)
		{
			GoSouth(r);
		}
		else if (input == GO_EAST)
		{
			GoEast(r);
		}
		else if (input == GO_WEST)
		{
			GoWest(r);
		}
		else if (input == GET_LAMP)
		{
			GetLamp(r);
		}
		else if (input == GET_KEY)
		{
			GetKey(r);
		}
		else if (input == OPEN_CHEST)
		{
			OpenChest(r);
		}
		else
		{
			Quit();
		}
	}

	private void UpdateGameState()
	{
		if (hasPlayerQuit || !isAdventureAlive || hasPlayerWon)
		{
			return;
		}

		if (isGrueChasing && IsGrueInSameRoomAsAdventurer())
		{
			Console.WriteLine("The Grue and the Adventurer are in the same room!");
			isAdventureAlive = false;
			return;
		}

		if (isChestOpen && IsAdventurerAtExit())
		{
			Console.WriteLine("You reached the dungeon exit after opening the chest!");
			hasPlayerWon = true;
			return;
		}

		if (isGrueChasing)
		{
			MoveGrueTowardAdventurer();

			Console.WriteLine("You hear the Grue moving closer...");

			if (IsGrueInSameRoomAsAdventurer())
			{
				Console.WriteLine("The Grue caught you!");
				isAdventureAlive = false;
			}
		}
	}

	private bool IsGameOver()
	{
		return hasPlayerWon || hasPlayerQuit || !isAdventureAlive;
	}

	private void ShowGameOverScreen()
	{
		Console.WriteLine();

		if (hasPlayerWon)
		{
			Console.WriteLine("YOU WIN! You escaped the dungeon with the treasure.");
		}
		else if (hasPlayerQuit)
		{
			Console.WriteLine("You quit the game.");
		}
		else
		{
			Console.WriteLine("GAME OVER! The Grue got you.");
		}
	}

	private void GoNorth(Room r)
	{
		if (r.HasNorth())
		{
			aRow -= 1;
			lastDirection = GO_SOUTH;
		}
		else
		{
			Console.WriteLine("You cannot go north!\a");
		}
	}

	private void GoSouth(Room r)
	{
		if (r.HasSouth())
		{
			aRow += 1;
			lastDirection = GO_NORTH;
		}
		else
		{
			Console.WriteLine("You cannot go south!\a");
		}
	}

	private void GoEast(Room r)
	{
		if (r.HasEast())
		{
			aCol += 1;
			lastDirection = GO_WEST;
		}
		else
		{
			Console.WriteLine("You cannot go east!\a");
		}
	}

	private void GoWest(Room r)
	{
		if (r.HasWest())
		{
			aCol -= 1;
			lastDirection = GO_EAST;
		}
		else
		{
			Console.WriteLine("You cannot go west!\a");
		}
	}

	private void GetLamp(Room r)
	{
		if (r.HasLamp())
		{
			Console.WriteLine("You got the lamp!");
			adventurer.SetLamp(true);
			r.SetLamp(false);
		}
		else
		{
			Console.WriteLine("There is no lamp in this room.");
		}
	}

	private void GetKey(Room r)
	{
		if (r.HasKey())
		{
			Console.WriteLine("You got the key!");
			adventurer.SetKey(true);
			r.SetKey(false);
		}
		else
		{
			Console.WriteLine("There is no key in this room.");
		}
	}

	private void OpenChest(Room r)
	{
		if (r.HasChest())
		{
			if (adventurer.HasKey())
			{
				Console.WriteLine("You opened the treasure chest!");
				Console.WriteLine("The Grue heard the chest open and is now pursuing you!");

				isChestOpen = true;
				isGrueChasing = true;

				r.SetChest(false);
			}
			else
			{
				Console.WriteLine("You do not have the key!");
			}
		}
		else
		{
			Console.WriteLine("There is no chest in this room.");
		}
	}

	private void Quit()
	{
		Console.WriteLine("You quit the game!");
		hasPlayerQuit = true;
	}

	private bool IsAdventurerAtExit()
	{
		return aRow == exitRow && aCol == exitCol;
	}

	private bool IsGrueInSameRoomAsAdventurer()
	{
		return aRow == grueRow && aCol == grueCol;
	}

	private void MoveGrueTowardAdventurer()
	{
		(int nextRow, int nextCol) = FindNextGrueStep();

		grueRow = nextRow;
		grueCol = nextCol;
	}

	private (int row, int col) FindNextGrueStep()
	{
		(int row, int col) start = (grueRow, grueCol);
		(int row, int col) target = (aRow, aCol);

		if (start == target)
		{
			return start;
		}

		Queue<(int row, int col)> queue = new Queue<(int row, int col)>();
		Dictionary<(int row, int col), (int row, int col)> previous = new Dictionary<(int row, int col), (int row, int col)>();

		bool[,] visited = new bool[dungeon.GetLength(0), dungeon.GetLength(1)];

		queue.Enqueue(start);
		visited[start.row, start.col] = true;

		while (queue.Count > 0)
		{
			(int row, int col) current = queue.Dequeue();

			if (current == target)
			{
				break;
			}

			foreach ((int row, int col) neighbor in GetNeighbors(current.row, current.col))
			{
				if (!visited[neighbor.row, neighbor.col])
				{
					visited[neighbor.row, neighbor.col] = true;
					previous[neighbor] = current;
					queue.Enqueue(neighbor);
				}
			}
		}

		if (!visited[target.row, target.col])
		{
			return start;
		}

		(int row, int col) step = target;

		while (previous.ContainsKey(step) && previous[step] != start)
		{
			step = previous[step];
		}

		return step;
	}

	private IEnumerable<(int row, int col)> GetNeighbors(int row, int col)
	{
		if (IsValidRoom(row - 1, col))
		{
			yield return (row - 1, col);
		}

		if (IsValidRoom(row + 1, col))
		{
			yield return (row + 1, col);
		}

		if (IsValidRoom(row, col + 1))
		{
			yield return (row, col + 1);
		}

		if (IsValidRoom(row, col - 1))
		{
			yield return (row, col - 1);
		}
	}

	private bool IsValidRoom(int row, int col)
	{
		return row >= 0 &&
			   row < dungeon.GetLength(0) &&
			   col >= 0 &&
			   col < dungeon.GetLength(1) &&
			   dungeon[row, col] != null;
	}
}
