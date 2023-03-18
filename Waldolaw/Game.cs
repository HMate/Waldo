using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;

namespace Waldolaw
{
    public enum Direction
    {
        Top,
        Right,
        Bottom,
        Left,
        None,
    }

    public static class DirectionExtensions
    {
        public static readonly Direction[] MAIN_DIRECTIONS = new[] { Direction.Top, Direction.Right, Direction.Bottom, Direction.Left };

        public static int CostTo(this Direction current, Direction target)
        {
            if (current == target) return 0;
            else if (current == Direction.None || target == Direction.None) return 0;
            else if ((current == Direction.Top || target == Direction.Top) &&
                (current == Direction.Left || target == Direction.Left))
            {
                return 1;
            }
            else
            {
                return Math.Abs(target - current);
            }
        }

        public static Direction GetDirectionToTurn(this Direction current, Direction target)
        {
            if (current == Direction.None || target == Direction.None) { return Direction.None; }
            int turnCost = current - target;
            if (turnCost == 0) { return Direction.None; }
            if ((turnCost == -1) || (turnCost == 3))
            {
                return Direction.Right;
            }
            return Direction.Left;
        }

        public static Direction Reverse(this Direction current)
        {
            return current switch
            {
                Direction.Top => Direction.Bottom,
                Direction.Left => Direction.Right,
                Direction.Right => Direction.Left,
                Direction.Bottom => Direction.Top,
                _ => Direction.None,
            };
        }

        public static string ToAscii(this Direction current)
        {

            return current switch
            {
                Direction.Top => "^",
                Direction.Right => ">",
                Direction.Bottom => "V",
                Direction.Left => "<",
                _ => "!",
            };
        }
    }

    public record struct Pos(int X, int Y)
    {
        public static int HammingDist(Pos targetPos, Pos position)
        {
            return Math.Abs(targetPos.X - position.X) + Math.Abs(targetPos.Y - position.Y);
        }

        public static Pos operator +(Pos self, Pos other)
        {
            return new Pos(self.X + other.X, self.Y + other.Y);
        }

        public static Pos operator +(Pos self, Direction dir)
        {
            return dir switch
            {
                Direction.Top => new Pos(self.X, self.Y - 1),
                Direction.Right => new Pos(self.X + 1, self.Y),
                Direction.Bottom => new Pos(self.X, self.Y + 1),
                Direction.Left => new Pos(self.X - 1, self.Y),
                _ => new Pos(self.X, self.Y),
            };
        }

        public static Pos operator -(Pos self, Pos other)
        {
            return new Pos(self.X - other.X, self.Y - other.Y);
        }

        public int getDiff(Pos other)
        {
            Pos diff = (this - other);
            return Math.Abs(diff.X) + Math.Abs(diff.Y);
        }
    }

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
        public Direction Direction { get; set; } = Direction.Top;
        public int Speed { get; internal set; } = 1;

        public Item(string name, ItemType type, Pos position, int fuel = 0)
        {
            Name = name;
            Type = type;
            Position = position;
            Fuel = fuel;
        }
    }

    public class Game<CellType> where CellType : Cell, new()
    {
        public Level<CellType> Level { get; private set; }
        public int MaxFuel { get; private set; }
        public int MaxSpeed { get; private set; }
        public List<Item> Items { get; private set; }

        public Item Waldo { get; private set; }
        public Item Base { get; private set; }
        public Item Ship { get; private set; }

        public Game(int mapSize, int maxFuel, int maxSpeed, List<Item> items)
        {
            Level = new Level<CellType>(mapSize);
            Items = items;
            MaxFuel = maxFuel;
            MaxSpeed = maxSpeed;

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
                Level.PlaceItem(item.Position, item);
            }
#if DEBUG
            Guard.IsNotNull(Waldo, $"items should contain a {nameof(Waldo)} entry");
            Guard.IsNotNull(Base, $"items should contain a {nameof(Base)} entry");
            Guard.IsNotNull(Ship, $"items should contain a {nameof(Ship)} entry");
#endif
        }

        internal Game<CellT> Copy<CellT>() where CellT : Cell, new()
        {
            List<Item> items = Items.Select(i => i with { }).ToList();
            Game<CellT> copy = new Game<CellT>(Level.Size, MaxFuel, MaxSpeed, items);
            return copy;
        }
    }
}
