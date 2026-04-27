using GameEngine.Logic;

namespace GameEngine.Models;

public enum GameMode
{
    PvP,
    PvA,
    AvA,
    SoH, // Strait of Ormuz
}

public class GameTable
{
    private readonly Map map;
    private readonly Map opponentBoard;
    private readonly Map playerBoard;
    private readonly List<PlayerFleet> fleets;
    private readonly PlayerFleet p1Fleet;
    private readonly PlayerFleet p2Fleet;

    public GameTable()
    {
        map = Map.GenerateMap();
        opponentBoard = Map.GenerateMap();
        playerBoard = Map.GenerateMap();
        var p1 = new Player( "P1", false);
        var p2 = new Player("P2", false);
        p1Fleet = PopulateFleet.BuildPlayerFleet(playerBoard, p1);
        p2Fleet = PopulateFleet.BuildPlayerFleet(opponentBoard, p2);
    }

    public void Start()
    {
        Console.WriteLine("Welcome to the Game!");
        Console.WriteLine("Please select a game mode:");
        Console.WriteLine("1. PvP");
        Console.WriteLine("2. Player vs AI");
        Console.WriteLine("3. AI vs AI");
        Console.WriteLine("4. Strait of Ormuz (Player vs Smart AI, limited fleet)");

        string input = Console.ReadLine();
        
        switch (input)
        {
            case "1":
                Console.WriteLine("You selected 2 Players mode.");
                PlayGame(GameMode.PvP);
                break;
                
            case "2":
                Console.WriteLine("You selected 1 Player vs AI mode.");
                PlayGame(GameMode.PvA);
                break;
            case "3":
                Console.WriteLine("You selected AI vs AI mode.");
                PlayGame(GameMode.AvA);
                break;
            case "4":
                Console.WriteLine("You selected Strait of Ormuz mode.");
                PlayStraitOfOrmuz();
                break;
            default:
                Console.WriteLine("Invalid input. Please try again.");
                break;
        }
    }

    private static List<PlayerFleet> BuildPlayersFleets(Map opponentBoard, Map playerBoard, Player p1, Player p2)
    {
        var p1Fleet = PopulateFleet.BuildPlayerFleet(playerBoard, p1);
        var p2Fleet = PopulateFleet.BuildPlayerFleet(opponentBoard, p2);
            
        return new List<PlayerFleet> { p1Fleet, p2Fleet };
    }

    private void PlayGame(GameMode mode)
    {
        // Create SmartAI instances for AI-controlled players
        SmartAI? p1SmartAI = mode == GameMode.AvA ? new SmartAI() : null;
        SmartAI? p2SmartAI = mode is GameMode.PvA or GameMode.AvA ? new SmartAI() : null;

        var round = 0;
        
        while (!p1Fleet.IsFleetSunk() && !p2Fleet.IsFleetSunk())
        {
            if ((round & 1) != 1)
            {
                // P1's turn: shoot at P2's fleet on opponentBoard
                opponentBoard.DrawMap(false);
                Console.WriteLine("Player 1 turn.");
                bool isP1Ai = mode == GameMode.AvA;
                ShotLogic.Shot(p2Fleet, opponentBoard, isP1Ai, p1SmartAI);
            }
            else
            {
                // P2's turn: shoot at P1's fleet on playerBoard
                playerBoard.DrawMap(false);
                Console.WriteLine("Player 2 turn.");
                bool isP2Ai = mode is GameMode.PvA or GameMode.AvA;
                ShotLogic.Shot(p1Fleet, playerBoard, isP2Ai, p2SmartAI);
            }

            round++;
        }

        if (p2Fleet.IsFleetSunk())
            Console.WriteLine("Player 1 wins!");
        else
            Console.WriteLine("Player 2 wins!");

        Console.WriteLine("Game Over, Rounds: " + round);
        Console.WriteLine("---------");
    }

    /// <summary>
    /// Strait of Ormuz scenario: a limited-fleet engagement in the world's most
    /// strategic waterway.  The human Commander faces a Smart AI opponent using
    /// a reduced fleet (Destroyer, Submarine, Cruiser) on a standard board.
    /// </summary>
    private void PlayStraitOfOrmuz()
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║        STRAIT OF ORMUZ SCENARIO          ║");
        Console.WriteLine("║  A limited-fleet engagement in the        ║");
        Console.WriteLine("║  world's most strategic waterway.         ║");
        Console.WriteLine("║  Fleet: Destroyer · Submarine · Cruiser   ║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
        Console.WriteLine();

        var straitFleet = new[] { ShipClass.Destroyer, ShipClass.Submarine, ShipClass.Cruiser };

        var playerMap = Map.GenerateMap();
        var aiMap     = Map.GenerateMap();

        var humanPlayer = new Player("Commander", false);
        var aiPlayer    = new Player("Enemy Fleet", true);

        var playerFleet = PopulateFleet.BuildCustomFleet(playerMap, humanPlayer, straitFleet);
        var aiFleet     = PopulateFleet.BuildCustomFleet(aiMap, aiPlayer, straitFleet);

        // Smart AI knows only the three-ship fleet
        var smartAI = new SmartAI(straitFleet);

        int round = 0;
        while (!playerFleet.IsFleetSunk() && !aiFleet.IsFleetSunk())
        {
            if ((round & 1) == 0)
            {
                // Commander's turn
                aiMap.DrawMap(false);
                Console.WriteLine("Commander's turn – engage the enemy!");
                ShotLogic.Shot(aiFleet, aiMap, isAiTurn: false, smartAI: null);
            }
            else
            {
                // Smart AI's turn
                playerMap.DrawMap(false);
                Console.WriteLine("Enemy fleet attacks!");
                ShotLogic.Shot(playerFleet, playerMap, isAiTurn: true, smartAI: smartAI);
            }

            round++;
        }

        Console.WriteLine();
        Console.WriteLine(aiFleet.IsFleetSunk()
            ? "★  Victory! The Strait of Ormuz is secured!"
            : "✗  Defeat! The enemy controls the strait!");
        Console.WriteLine("Game Over, Rounds: " + round);
        Console.WriteLine("---------");
    }
    
    //if p1 then p2 is enemy, and if p2 then p1 is enemy
    private static void ReverseEnemyContext(Map map)
    {
        var updatedCoordinates = new Dictionary<(int, int), AllocationType>();

        foreach (var (key, value) in map.Coordinates)
        {
            switch (value)
            {
                case AllocationType.EnemyShip:
                    updatedCoordinates[key] = AllocationType.AllyShip;
                    break;
                case AllocationType.EnemyHitted:
                    updatedCoordinates[key] = AllocationType.AllyShipHitted;
                    break;
                case AllocationType.AllyShip:
                    updatedCoordinates[key] = AllocationType.EnemyShip;
                    break;
                case AllocationType.AllyShipHitted:
                    updatedCoordinates[key] = AllocationType.EnemyHitted;
                    break;
                case AllocationType.Water:
                case AllocationType.ShotMissed:
                    updatedCoordinates[key] = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        map.Coordinates = updatedCoordinates;
    }
}