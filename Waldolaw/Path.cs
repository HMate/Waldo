using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waldolaw
{
    public record Path
    {
        public List<Item> Nodes { get; internal set; }
        public Item Last { get; private set; }
        public Direction Facing { get; private set; }
        public int Steps { get; internal set; }
        public bool HasWaldo { get; internal set; }
        public int FuelCost { get; internal set; } = 0;
        public int FuelAvailable { get; internal set; }
        public int FuelLastFound { get; internal set; } = 0;
        public int Speed { get; internal set; }


        public Path(Item last, int steps, int fuelAvailable, int speed, Direction facing)
        {
            Nodes = new List<Item>() { last };
            Last = last;
            Steps = steps;
            Facing = facing;
            FuelAvailable = fuelAvailable;
            Speed = speed;
            if (last.Type == ItemType.Waldo)
            {
                HasWaldo = true;
            }
        }

        public Path Extend(Item last, int additionalSteps, Direction endFacing)
        {
            int fuelHere = (last.Type == ItemType.Planet) ? last.Fuel : 0;
            Path result = this with
            {
                Nodes = new List<Item>(Nodes).Append(last).ToList(),
                HasWaldo = HasWaldo || last.Type == ItemType.Waldo,
                Last = last,
                Steps = Steps + additionalSteps,
                Facing = endFacing,
                FuelCost = FuelCost + Simulator.CalcFuelCost(additionalSteps, Speed),
                FuelAvailable = FuelAvailable + FuelLastFound, // TODO: Consider max tankable fuel ...
                FuelLastFound = fuelHere,
                Speed = (last.Type == ItemType.Turbo) ? (Speed + 1) : Speed,
            };
            return result;
        }

        public override string ToString()
        {
            string planets = string.Join(" - ", Nodes.Select(x => $"{x.Name}({x.Position.X},{x.Position.Y})"));
            return $"{planets} | FΔ={FuelAvailable - FuelCost}, F+={FuelLastFound}, S={Steps}";
        }

        public bool Contains(Pos pos)
        {
            return Nodes.Find(n => n.Position == pos) != null;
        }

        /// <summary>
        /// Path is valid = has enough fuel for it.
        /// </summary>
        internal bool IsValid()
        {
            return FuelCost <= FuelAvailable;
        }

        /// <summary>
        /// Path is complete = has fuel + has Waldo + reached Base
        /// </summary>
        internal bool IsComplete()
        {
            return Last.Type == ItemType.Base && HasWaldo && IsValid();
        }

        /// <summary>
        /// Target still worth visiting if it is Base or has fuel left on it.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal bool StillWorthVisit(Pos target)
        {
            // TODO: consider max fuel tank size while building path.
            Item? visited = Nodes.Find(n => n.Position == target);
            if (visited == null)
                return true;
            if (visited.Type == ItemType.Base)
                return true;
            return false;
        }
    }
}
