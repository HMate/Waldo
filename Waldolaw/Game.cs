using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;

namespace Waldolaw
{
    public record struct Pos(int X, int Y);

    public enum ItemType
    {
        Empty = 0,
        Waldo,
        Base,
        Ship,
        Planet,
        Satellite,
        Asteroid,
        Turbo
    }

    public record Item
    {
        public string Name { get; set; }
        public ItemType Type { get; set; }
        public Pos Position { get; set; }
        public int Fuel { get; set; }

        public Item(string name, ItemType type, Pos position, int fuel = 0)
        {
            Name = name;
            Type = type;
            Position = position;
            Fuel = fuel;
        }
    }

    public class Game
    {
        public Level Level { get; private set; }
        public List<Item> Items { get; private set; }

        public Item Waldo { get; private set; }
        public Item Base { get; private set; }
        public Item Ship { get; private set; }

        public Game(Level level, List<Item> items)
        {
            Level = level;
            Items = items;

            foreach (Item item in Items)
            {
                switch (item.Type)
                {
                    case ItemType.Waldo:
                        Waldo = item;
                        break;
                    case ItemType.Base:
                        Base = item;
                        break;
                    case ItemType.Ship:
                        Ship = item;
                        break;
                }
            }

            Guard.IsNotNull(Waldo, $"items should contain a {nameof(Waldo)} entry");
            Guard.IsNotNull(Base, $"items should contain a {nameof(Base)} entry");
            Guard.IsNotNull(Ship, $"items should contain a {nameof(Ship)} entry");
        }
    }
}
