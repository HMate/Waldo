using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Waldolaw
{
    public class GameBuilder
    {
        public GameBuilder(ILogger<GameBuilder> logger) { _logger = logger; }

        public Game Build(UserInputsJSON input)
        {
            List<Item> items = new List<Item>();
            foreach (var item in input.items)
            {
                // max.y - s1.y
                int sat0 = GetSatelliteDistance(Satellite0, item.distances);
                int sat2 = GetSatelliteDistance(Satellite2, item.distances);

                Pos pos = CalcItemPosition(sat0, sat2, input.mapsize);

                items.Add(new Item(item.name, ParseType(item.name), pos, item.fuel));
            }
            // Add satellites and base
            items.Add(new Item(Satellite0, ItemType.Satellite, new Pos(0, 0), 0));
            items.Add(new Item(Satellite1, ItemType.Satellite, new Pos(input.mapsize - 1, 0), 0));
            items.Add(new Item(Satellite2, ItemType.Satellite, new Pos(0, input.mapsize - 1), 0));
            items.Add(new Item(Satellite3, ItemType.Satellite, new Pos(input.mapsize - 1, input.mapsize - 1), 0));
            items.Add(new Item("BASE", ItemType.Base, new Pos((input.mapsize - 1) / 2, input.mapsize - 1), 0));
            items.Add(new Item("SHIP", ItemType.Ship, new Pos((input.mapsize - 1) / 2, input.mapsize - 1), input.fuel));

            return new Game(new Level(input.mapsize), items);
        }

        private int GetSatelliteDistance(string name, List<DistancesJSON> distances)
        {
            return distances.Find(d => d.SatelliteName == name)?.Distance ??
                ThrowHelper.ThrowArgumentException<int>($"Missing {name} from distances");
        }

        private Pos CalcItemPosition(int sat0, int sat2, int mapSize)
        {
            for (int x = 0; x < sat0 + 1; x++)
            {
                var y = sat0 - x;
                if ((sat2 - x) == mapSize - 1 - y)
                {
                    return new Pos(x, y);
                }
            }

            return ThrowHelper.ThrowInvalidOperationException<Pos>(
                $"Could not determine item pos: mapsize: {mapSize}, sat0: {sat0}, sat2: {sat2}"); ;
        }

        private ItemType ParseType(string name)
        {
            string[] parts = name.Split('_', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Count() < 1)
            {
                throw new Exception($"Failed to parse type out of '{name}'");
            }

            ItemType type = parts[0].ToUpper() switch
            {
                "WALDO" => ItemType.Waldo,
                "PLANET" => ItemType.Planet,
                "SPEED" => ItemType.Turbo,
                "SAT" => ItemType.Satellite,
                "ASTEROID" => ItemType.Asteroid,
                _ => ItemType.Empty
            };
            if (type == ItemType.Empty)
            {
                _logger.LogWarning("Could not parse type from '{name}'", name);
            }
            return type;
        }

        private readonly ILogger _logger;
        private readonly string Satellite0 = "SAT_0";
        private readonly string Satellite1 = "SAT_1";
        private readonly string Satellite2 = "SAT_2";
        private readonly string Satellite3 = "SAT_3";
    }
}
