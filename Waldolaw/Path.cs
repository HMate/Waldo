
namespace Waldolaw
{
    public record Path
    {
        public List<Item> Nodes { get; internal set; }
        public List<Direction> StepsList { get; internal set; }
        public List<Pos> Visited { get; internal set; } = new();
        public Item Last { get; private set; }
        public Direction Facing { get; private set; }
        public int ShipFuelCurrently { get; private set; }
        public int Steps { get; internal set; }
        public int StepsAtWaldo { get; internal set; }
        public bool HasWaldo { get; internal set; }
        public int TotalFuelSpent { get; internal set; } = 0;
        public int TotalFuelTanked { get; internal set; }
        public int FuelLastFound { get; internal set; } = 0;
        public int Speed { get; internal set; }
        public int FuelSpent { get; private set; } = 0;

        private Dictionary<Pos, int> _FuelTanked = new();
        private readonly int _MaxShipFuel;
        private readonly int _MaxShipSpeed;
        private Path? _Parent = null;

        public Path(Item last, int steps, int fuelAvailable, int maxFuel, int speed, int maxSpeed, Direction facing, List<Direction> stepsList)
        {
            Nodes = new List<Item>() { last };
            Last = last;
            Steps = steps;
            StepsList = stepsList;
            Facing = facing;
            ShipFuelCurrently = fuelAvailable;
            TotalFuelTanked = fuelAvailable;
            Speed = speed;
            _MaxShipFuel = maxFuel;
            _MaxShipSpeed = maxSpeed;
            if (last.Type == ItemType.Waldo)
            {
                HasWaldo = true;
            }
        }

        public Path Extend(Item last, int additionalSteps, List<Direction> steps)
        {
            bool isPlanet = (last.Type == ItemType.Planet);
            bool isTurbo = (last.Type == ItemType.Turbo);
            int fuelTankedHere = 0;
            int fuelSpentThisMove = Simulator.CalcFuelCost(additionalSteps, Speed);
            int planetFuelTanked = FuelTankedInPreviousStops(last);
            if (isPlanet)
            {
                int fuelHere = last.Fuel - planetFuelTanked;
                fuelTankedHere = Math.Max(0, Math.Min(fuelHere, _MaxShipFuel - ShipFuelCurrently - FuelLastFound + fuelSpentThisMove));
                planetFuelTanked += fuelTankedHere;
            }
            var visited = Visited;
            if (isTurbo)
            {
                visited = Visited.Append(last.Position).ToList();
            }

            Path result = this with
            {
                Nodes = new List<Item>(Nodes).Append(last).ToList(),
                HasWaldo = HasWaldo || last.Type == ItemType.Waldo,
                Last = last,
                Steps = Steps + additionalSteps,
                StepsAtWaldo = last.Type == ItemType.Waldo ? Steps + additionalSteps : StepsAtWaldo,
                StepsList = StepsList.Concat(steps).ToList(),
                Facing = steps.Last(),
                TotalFuelSpent = TotalFuelSpent + fuelSpentThisMove,
                TotalFuelTanked = TotalFuelTanked + FuelLastFound,
                FuelSpent = fuelSpentThisMove,
                ShipFuelCurrently = Math.Min(ShipFuelCurrently + FuelLastFound, _MaxShipFuel) - fuelSpentThisMove,
                FuelLastFound = fuelTankedHere,
                Speed = (isTurbo && !Visited.Contains(last.Position)) ? Math.Min(Speed + 1, _MaxShipSpeed) : Speed,
                Visited = visited,
                _FuelTanked = new Dictionary<Pos, int>(_FuelTanked),
                _Parent = this
            };
            if (isPlanet)
                result._FuelTanked[last.Position] = planetFuelTanked;
            return result;
        }

        private int FuelTankedInPreviousStops(Item target)
        {
            _FuelTanked.TryGetValue(target.Position, out int tanked);
            return tanked;
        }

        public override string ToString()
        {
            string planets = string.Join(" - ", Nodes.Select(x => $"{x.Name}({x.Position.X},{x.Position.Y})"));
            return $"{planets} | FΔ={TotalFuelTanked - TotalFuelSpent}, F={ShipFuelCurrently}, S={Steps}";
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
            return ShipFuelCurrently >= 0 && (ShipFuelCurrently + FuelLastFound <= _MaxShipFuel);
        }

        /// <summary>
        /// Path is complete = has fuel + has Waldo + reached Base
        /// </summary>
        internal bool IsComplete()
        {
            return Last.Type == ItemType.Base && HasWaldo && IsValid();
        }
    }
}
