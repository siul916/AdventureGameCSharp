# AdventureGameCSharp

Text-based adventure game built in C#. The player explores a dungeon, finds a lamp and a key, opens the treasure chest, and then must escape through the dungeon exit while avoiding the Grue.

## Game Objective

The player wins only after opening the treasure chest and reaching the dungeon exit. Opening the chest does not end the game immediately.

After the treasure chest is opened, the Grue starts pursuing the adventurer. If the Grue and the adventurer reach the same room at any point, the player loses.

## Dungeon

The dungeon is designed in:

```text
res/DungeonTemplate.txt
```

The game loads the dungeon from this file using the `DungeonLoader` class.

## Commands

```text
W - Go north
S - Go south
D - Go east
A - Go west
L - Get lamp
K - Get key
O - Open chest
Q - Quit
```

## Mechanics

- Lit rooms show their room description.
- Unlit rooms show a pitch-black message.
- Without the lamp, taking an action in an unlit room other than returning to the previous room causes the player to be eaten by the Grue.
- The lamp allows the player to safely explore unlit rooms.
- The key is required to open the treasure chest.
- After the chest is opened, the Grue pursues the player.
- The player wins by escaping through the dungeon exit after opening the chest.

## How To Run

Open the solution file in Visual Studio:

```text
AdventureGameCSharp.sln
```

Then run the `AdventureGame` project.
