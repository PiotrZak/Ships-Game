using GameEngine.Models;

namespace GameEngine.Logic;

public abstract class ShotLogic
{
    /// <summary>
    /// Executes one shot turn for the current player.
    /// </summary>
    /// <param name="targetFleet">The fleet being shot at.</param>
    /// <param name="targetMap">The map that tracks shot results for the target fleet.</param>
    /// <param name="isAiTurn">When true the shot is chosen automatically.</param>
    /// <param name="smartAI">
    /// Optional SmartAI instance.  When provided it supplies the shot coordinate
    /// and is updated with the result so it can learn for subsequent turns.
    /// When null an AI turn falls back to random selection.
    /// </param>
    public static void Shot(PlayerFleet targetFleet, Map targetMap, bool isAiTurn, SmartAI? smartAI = null)
    {
        while (true)
        {
            Console.WriteLine("Where to shot? (eg A5)");

            string? userShotCoordinates;
            if (isAiTurn)
            {
                userShotCoordinates = smartAI != null ? smartAI.ChooseShot() : AIChoose();
                Console.WriteLine("AI chooses: " + userShotCoordinates);
            }
            else
            {
                userShotCoordinates = Console.ReadLine();
            }

            if (Validators.IsInputCorrect(userShotCoordinates))
            {
                var (x, y) = Parsers.ParseUserInput(userShotCoordinates);

                if (targetMap.Coordinates.TryGetValue((x, y), out var allocationType))
                {
                    switch (allocationType)
                    {
                        case AllocationType.AllyShip:
                            Console.WriteLine("Cannot attack allies!");
                            continue;
                        case AllocationType.ShotMissed:
                            Console.WriteLine("Hey its already missed, don't waste energy and resources!");
                            continue;
                        case AllocationType.AllyShipHitted:
                            Console.WriteLine("Its your ship already attacked!");
                            continue;
                        case AllocationType.EnemyHitted:
                            Console.WriteLine("Its enemy ship already attacked");
                            continue;
                        case AllocationType.Water:
                        case AllocationType.EnemyShip:
                        default:
                            var (wasHit, wasSunk, shipLength) = HandleHit(targetFleet, targetMap, x, y);
                            if (smartAI != null)
                            {
                                if (wasHit) smartAI.RecordHit(x, y);
                                else smartAI.RecordMiss(x, y);
                                if (wasSunk) smartAI.RecordSunk(shipLength);
                            }
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Key not found - Out Of board");
                    if (isAiTurn) continue;
                }
            }
            else
            {
                continue;
            }

            break;
        }
    }

    public static string? AIChoose()
    {
        Random random = new Random();
        int columnIndex = random.Next(10);
        char column = (char)('A' + columnIndex);
        int row = random.Next(1, 11);
        return $"{column}{row}";
    }

    public static (bool wasHit, bool wasSunk, int shipLength) HandleHit(PlayerFleet playerFleet, Map map, int x, int y)
    {
        var hitted = IsHitted(map, (x, y));
        if (hitted)
        {
            var ship = playerFleet.Ships.SingleOrDefault(ship => ship.Position.ContainsKey((x, y)));

            if (ship == null)
            {
                throw new Exception("Opposite fleet passes - there isn't ship of this fleet on this coordinate");
            }

            ship.Position[(x, y)] = false;
            bool wasSunk = ship.IsSunk;
            if (wasSunk)
            {
                Console.WriteLine("Congrats! enemy ship " + ship.ShipClass + " sunked!");
                Console.WriteLine("Left Enemy ships " + playerFleet.ShipsLeft());
            }
            else
            {
                Console.WriteLine("Enemy Ship " + ship.ShipClass + " hitted - left ship fields: " + ship.Position.Count(isHitted => isHitted.Value));
            }

            return (true, wasSunk, ship.Length);
        }

        return (false, false, 0);
    }
    
    public static bool IsHitted(Map map, (int, int) coordinates)
    {
        if (map.Coordinates[coordinates] == AllocationType.EnemyShip)
        {
            map.Coordinates[coordinates] = AllocationType.EnemyHitted;
            return true;
        }
        Console.WriteLine("Miss");
        map.Coordinates[coordinates] = AllocationType.ShotMissed;

        return false;
    }
}