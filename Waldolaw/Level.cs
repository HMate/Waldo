using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Primitives;
using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waldolaw
{
    public class Level
    {
        public int Size { get; private set; }

        public Level(int size)
        {
            Size = size;
            _grid = Enumerable.Range(1, Size).Select(y => Enumerable.Range(1, Size).Select(x => new Cell()).ToArray()).ToArray();
        }

        public Item? ItemAt(int x, int y)
        {
            return _grid[y][x].Items.FirstOrDefault();
        }

        public void PlaceItem(Pos position, Item item)
        {
            GetGridCell(position).Items.Add(item);
        }

        public Cell GetGridCell(Pos pos)
        {
            return _grid[pos.Y][pos.X];
        }

        public List<(Pos, Direction)> GetNeighbours(Pos current)
        {
            List<(Pos, Direction)> result = new();
            if (current.X > 0)
            {
                result.Add((current - new Pos(1, 0), Direction.Left));
            }
            if (current.X < Size - 1)
            {
                result.Add((current + new Pos(1, 0), Direction.Right));
            }
            if (current.Y > 0)
            {
                result.Add((current - new Pos(0, 1), Direction.Top));
            }
            if (current.Y < Size - 1)
            {
                result.Add((current + new Pos(0, 1), Direction.Bottom));
            }

            return result;
        }

        public void PrintLevel(Item ship)
        {
            _logger.Debug($"---- LEVEL: -- SHIP FUEL: {ship.Fuel} --");
            foreach (var row in _grid)
            {
                StringBuilder message = new StringBuilder();
                foreach (var cell in row)
                {
                    if (cell.Items.Count == 0)
                    {
                        message.Append(".");
                    }
                    else
                    {
                        Item? cellShip = cell.Items.Find(it => it.Type == ItemType.Ship);
                        if (cellShip != null)
                        {
                            switch (ship.Direction)
                            {
                                case Direction.Top:
                                    message.Append("^");
                                    break;
                                case Direction.Right:
                                    message.Append(">");
                                    break;
                                case Direction.Bottom:
                                    message.Append("V");
                                    break;
                                case Direction.Left:
                                    message.Append("<");
                                    break;
                                case Direction.None:
                                    message.Append("!");
                                    break;
                                default:
                                    break;
                            }
                        }
                        else if (cell.Items[0].Type == ItemType.Waldo)
                        {
                            message.Append("W");
                        }
                        else if (cell.Items[0].Type == ItemType.Satellite)
                        {
                            message.Append("S");
                        }
                        else if (cell.Items[0].Type == ItemType.Planet)
                        {
                            message.Append("P");
                        }
                        else if (cell.Items[0].Type == ItemType.Asteroid)
                        {
                            message.Append("A");
                        }
                        else if (cell.Items[0].Type == ItemType.Turbo)
                        {
                            message.Append("T");
                        }
                        else if (cell.Items[0].Type == ItemType.Base)
                        {
                            message.Append("B");
                        }
                    }
                }
                _logger.Debug(message.ToString());
            }
        }

        public void PrintManhattanDistances()
        {
            _logger.Debug("---- LEVEL DISTANCES: ----");
            foreach (var row in _grid)
            {
                StringBuilder message = new StringBuilder();
                foreach (var cell in row)
                {
                    message.Append($"{cell.manhattanDistance,3}|");
                }
                _logger.Debug(message.ToString());
            }
        }

        public void ClearGridDistances()
        {
            foreach (var row in _grid)
            {
                foreach (var cell in row)
                {
                    cell.manhattanDistance = -1;
                }
            }
        }

        internal void MoveItem(Item item, Pos currentPos, Pos newPos)
        {
            Guard.IsTrue(GetGridCell(currentPos).Items.Contains(item));
            GetGridCell(currentPos).Items.Remove(item);
            GetGridCell(newPos).Items.Add(item);
            item.Position = newPos;
        }

        private readonly Cell[][] _grid;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    }

    public class Cell
    {
        public List<Item> Items = new();
        public int manhattanDistance = -1;
    }
}
