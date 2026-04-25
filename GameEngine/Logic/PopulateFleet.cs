using GameEngine.Models;

namespace GameEngine.Logic;

public class PopulateFleet
{
    public static PlayerFleet BuildPlayerFleet(Map map, Player player)
    {
        //var allocateType = player.Name == "P1" ? AllocationType.AllyShip : AllocationType.EnemyShip;
        var allocateType = AllocationType.EnemyShip;
        Console.WriteLine("Bulding " + player.Name + " fleet");
        
        var playerFleet = PopulateShips.RandomLocalize(map, allocateType);
        playerFleet.AssignPlayer(player);
        
        return playerFleet;
    }

    /// <summary>Builds a fleet using only the specified ship classes (used for special modes).</summary>
    public static PlayerFleet BuildCustomFleet(Map map, Player player, IEnumerable<ShipClass> ships)
    {
        var allocateType = AllocationType.EnemyShip;
        Console.WriteLine("Building " + player.Name + " fleet");

        var playerFleet = PopulateShips.RandomLocalizeWithFleet(map, allocateType, ships);
        playerFleet.AssignPlayer(player);

        return playerFleet;
    }
}