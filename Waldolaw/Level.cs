﻿using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Primitives;
using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Dynamic;
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
            _grid = Enumerable.Range(1, Size * Size).Select(x => new Cell()).ToArray();
        }

        public void PlaceItem(Pos position, Item item)
        {
            GetGridCell(position).Items.Add(item);
        }

        internal void RemoveItem(Pos position, Item item)
        {
            GetGridCell(position).Items.Remove(item);
        }

        internal void MoveItem(Item item, Pos currentPos, Pos newPos)
        {
            Guard.IsTrue(GetGridCell(currentPos).Items.Contains(item));
            GetGridCell(currentPos).Items.Remove(item);
            GetGridCell(newPos).Items.Add(item);
            item.Position = newPos;
        }

        public Item? ItemAt(Pos pos)
        {
            return _grid[GetIndex(pos)].Items.FirstOrDefault();
        }

        public Cell GetGridCell(Pos pos)
        {
            return _grid[GetIndex(pos)];
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

        private int GetIndex(Pos pos)
        {
            return Size * pos.Y + pos.X;
        }

        public void PrintLevel(Item ship)
        {
#if DEBUG
            _logger.Debug($"---- LEVEL: -- SHIP FUEL: {ship.Fuel} --");
            StringBuilder message = new StringBuilder();
            for (int i = 0; i < _grid.Length; i++)
            {
                var cell = _grid[i];
                if (cell.Items.Count == 0)
                {
                    message.Append(".");
                }
                else
                {
                    Item? cellShip = cell.Items.Find(it => it.Type == ItemType.Ship);
                    if (cellShip != null)
                    {
                        message.Append(ship.Direction.ToAscii());
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
                if (i != 0 && i % (Size - 1) == 0)
                {
                    _logger.Debug(message.ToString());
                    message.Clear();
                }
            }
#endif
        }

        public void PrintStepDistances()
        {
#if DEBUG
            _logger.Debug("---- LEVEL DISTANCES: ----");
            StringBuilder message = new StringBuilder();
            for (int i = 0; i < _grid.Length; i++)
            {
                var cell = _grid[i];
                message.Append($"{cell.StepDistance,3}|");
                if (i != 0 && i % (Size - 1) == 0)
                {
                    _logger.Debug(message.ToString());
                    message.Clear();
                }
            }
#endif
        }

        public void PrintFirstStepDirections()
        {
#if DEBUG
            _logger.Debug("---- LEVEL FIRST DIRECTIONS: ----");
            StringBuilder message = new StringBuilder();
            for (int i = 0; i < _grid.Length; i++)
            {
                var cell = _grid[i];
                message.Append($"{cell.FirstStepDirection.ToAscii(),3}|");
                if (i != 0 && i % (Size - 1) == 0)
                {
                    _logger.Debug(message.ToString());
                    message.Clear();
                }
            }
#endif
        }

        public void ClearGridDistances()
        {
            for (int i = 0; i < _grid.Length; i++)
            {
                var cell = _grid[i];
                cell.StepDistance = -1;
                cell.Steps = new() {
                        {Direction.Top, new () } ,
                        {Direction.Right, new () } ,
                        {Direction.Bottom, new () } ,
                        {Direction.Left, new () } ,
                    };
                cell.LastStepDirection = Direction.None;
                cell.FirstStepDirection = Direction.None;

            }
        }

        private readonly Cell[] _grid;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    }

    public class Cell
    {
        public List<Item> Items = new();
        public int StepDistance = -1;
        public Direction FirstStepDirection = Direction.None;
        public Direction LastStepDirection = Direction.None;
        public Dictionary<Direction, List<Direction>> Steps = new() {
            {Direction.Top, new () } ,
            {Direction.Right, new () } ,
            {Direction.Bottom, new () } ,
            {Direction.Left, new () } ,
        };
    }
}
