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

	private Adventurer adventurer;
	private Room[,] dungeon;
	private int aRow;
	private int aCol;
	private int exitRow;
	private int exitCol;
	private int grueRow;
	private int grueCol;
	private bool isChestOpen;
	private bool hasPlayerQuit;
	private bool isAdventureAlive;
	private bool hasPlayerWon;
	private string lastDirection;

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
			while(!IsValidInput(input));

			ProcessInput(input);

			UpdateGameState();
		}
		while(!IsGameOver());

		ShowGameOverScreen();
	}

	private void Init()
	{
		adventurer = new Adventurer();

		Dungeon loadedDungeon = DungeonLoader.LoadDungeon(GetDungeonFilePath());
		dungeon = loadedDungeon.Rooms;

		aRow = loadedDungeon.StartRow;
		aCol = loadedDungeon.StartCol;
		exitRow = loadedDungeon.ExitRow;
		exitCol = loadedDungeon.ExitCol;
		grueRow = loadedDungeon.GrueRow;
		grueCol = loadedDungeon.GrueCol;

		isChestOpen = false;
		hasPlayerQuit = false;
		isAdventureAlive = true;
		hasPlayerWon = false;

		lastDirection = string.Empty;
	}

	private string GetDungeonFilePath()
	{
		string? directory = AppContext.BaseDirectory;

		while(directory != null)
		{
			string filePath = Path.Combine(directory, "res", "DungeonTemplate.txt");

			if(File.Exists(filePath))
			{
				return filePath;
			}

			directory = Directory.GetParent(directory)?.FullName;
		}

		throw new FileNotFoundException("Could not find res/DungeonTemplate.txt.");
	}

	private void ShowGameStartScreen()
	{
		Console.WriteLine("Welcome to Adventure Game!");
	}

	private void ShowScene()
	{
		var r = dungeon[aRow, aCol];

		if(adventurer.HasLamp() || r.IsLit())
		{
			Console.WriteLine(r.GetDescription());
		}
		else
		{
			Console.WriteLine("This room is pitch black!");
		}
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
		return Console.ReadLine()!.ToUpper();
	}

	private bool IsValidInput(string input)
	{
		string[] validInputs = { GO_NORTH, GO_SOUTH, GO_EAST, GO_WEST, GET_LAMP, GET_KEY, OPEN_CHEST, QUIT };

		if(!validInputs.Contains(input))
		{
			Console.WriteLine("ERROR: Invalid input. Please try again.");
			return false;
		}

		return true;
	}

	private void ProcessInput(string input)
	{
		Room r = dungeon[aRow, aCol];

		if(!adventurer.HasLamp() && !r.IsLit() && input != lastDirection)
		{
			Console.WriteLine("You got eaten alive by the Grue!");
			isAdventureAlive = false;
		}
		else if(input == GO_NORTH)
		{
			GoNorth(r);
		}
		else if(input == GO_SOUTH)
		{
			GoSouth(r);
		}
		else if(input == GO_EAST)
		{
			GoEast(r);
		}
		else if(input == GO_WEST)
		{
			GoWest(r);
		}
		else if(input == GET_LAMP)
		{
			GetLamp(r);
		}
		else if(input == GET_KEY)
		{
			GetKey(r);
		}
		else if(input == OPEN_CHEST)
		{
			OpenChest(r);
		}
		else// if(input == QUIT)
		{
			Quit();
		}
	}

	private void UpdateGameState()
	{
		if(!isAdventureAlive || hasPlayerQuit)
		{
			return;
		}

		if(isChestOpen && aRow == exitRow && aCol == exitCol)
		{
			Console.WriteLine("You escaped the dungeon with the treasure!");
			hasPlayerWon = true;
			return;
		}

		if(!isChestOpen)
		{
			return;
		}

		if(IsGrueInAdventurerRoom())
		{
			LoseToPursuingGrue();
			return;
		}

		MoveGrueTowardAdventurer();

		if(IsGrueInAdventurerRoom())
		{
			LoseToPursuingGrue();
		}
	}

	private bool IsGameOver()
	{
		return hasPlayerWon || hasPlayerQuit || !isAdventureAlive;
	}

	private void ShowGameOverScreen()
	{
		if(hasPlayerWon)
		{
			Console.WriteLine("You win!");
		}
		else
		{
			Console.WriteLine("Game Over!");
		}
	}

	private void GoNorth(Room r)
	{
		if(r.HasNorth())
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
		if(r.HasSouth())
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
		if(r.HasEast())
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
		if(r.HasWest())
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
		if(r.HasLamp())
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
		if(r.HasKey())
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
		if(r.HasChest())
		{
			if(adventurer.HasKey())
			{
				Console.WriteLine("You got the treasure! The Grue heard the chest open and is chasing you!");
				isChestOpen = true;
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

	private bool IsGrueInAdventurerRoom()
	{
		return grueRow == aRow && grueCol == aCol;
	}

	private void LoseToPursuingGrue()
	{
		Console.WriteLine("The Grue caught you!");
		isAdventureAlive = false;
	}

	private void MoveGrueTowardAdventurer()
	{
		Queue<(int row, int col)> frontier = new();
		Dictionary<(int row, int col), (int row, int col)> cameFrom = new();
		var start = (grueRow, grueCol);
		var goal = (row: aRow, col: aCol);

		frontier.Enqueue(start);
		cameFrom[start] = start;

		while(frontier.Count > 0)
		{
			var current = frontier.Dequeue();

			if(current == goal)
			{
				break;
			}

			foreach(var next in GetNeighborPositions(current.row, current.col))
			{
				if(!cameFrom.ContainsKey(next))
				{
					frontier.Enqueue(next);
					cameFrom[next] = current;
				}
			}
		}

		if(!cameFrom.ContainsKey(goal))
		{
			return;
		}

		var step = goal;

		while(cameFrom[step] != start)
		{
			step = cameFrom[step];
		}

		grueRow = step.row;
		grueCol = step.col;
	}

	private List<(int row, int col)> GetNeighborPositions(int row, int col)
	{
		List<(int row, int col)> positions = new();

		AddIfTraversable(positions, row - 1, col);
		AddIfTraversable(positions, row, col + 1);
		AddIfTraversable(positions, row + 1, col);
		AddIfTraversable(positions, row, col - 1);

		return positions;
	}

	private void AddIfTraversable(List<(int row, int col)> positions, int row, int col)
	{
		if(row >= 0 &&
			row < dungeon.GetLength(0) &&
			col >= 0 &&
			col < dungeon.GetLength(1) &&
			dungeon[row, col] != null)
		{
			positions.Add((row, col));
		}
	}
}
