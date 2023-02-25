using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waldolaw
{
    public record struct Pos(int X, int Y);

    public record Item
    {
        public string Name { get; set; }
        public Pos Position { get; set; }
        public int Fuel { get; set; }

        public Item(string Name, Pos Position, int Fuel = 0)
        {
            this.Name = Name;
            this.Position = Position;
            this.Fuel = Fuel;
        }
    }

    public class Game
    {
        public Level Level { get; private set; }
        public List<Item> Items { get; private set; }

        public Game(Level Level, List<Item> Items)
        {
            this.Level = Level;
            this.Items = Items;
        }
    }
}
