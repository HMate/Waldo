//#define PATHPRINT
using C5;
using NLog;
using System.IO;
using System.Linq;

namespace Waldolaw
{
    public class AI
    {
        public AI(Game<Cell> game, Timer timer)
        {
            _game = game;
            _timer = timer;
        }

        public Commands? CalculatePathToWaldo()
        {
            _logger.Debug($"Waldo is at {_game.Waldo.Position}");
            _logger.Debug($"Level size: {_game.Level.Size}");


#if DEBUG
            long timeout = 4000;
#else
            const long timeout = 3600;
#endif
#if DEBUG
            long lastCallTimeout = 4700;
#else
            const long lastCallTimeout = 3700;
#endif
            long appTimeout = 3800;

            List<Path> completePaths;
            Game<DetailedCell> currentGame = _game.Copy<DetailedCell>();
            completePaths = CalculatePathToTargets(currentGame, timeout, lastCallTimeout);

            Commands? bestCommands = EvaluatePaths(completePaths, appTimeout);
            return bestCommands;
        }

        private Commands? EvaluatePaths(List<Path> completePaths, long timeout)
        {
            Commands? bestCommands = null;
            float bestScore = 10000000;
            int pathIndex = 0;
            int bestIndex = -1;
            foreach (var path in completePaths)
            {
                pathIndex++;
#if !(DEBUG)
                if (_timer.TimeMs() > timeout)
                {
                    break;
                }
#endif
                // _logger.Debug($"Simulating path #{pathIndex} {path}");
                Game<Cell> currentGame = _game.Copy<Cell>();
                Simulator sim = new(currentGame);
                Level<Cell> level = currentGame.Level;
                Item ship = currentGame.Ship;

                SimulateStepsFromPath(sim, path, level, ship);

                //SimulateStepsToTargets(sim, path, currentGame, level, ship);

                bool isPathGood = AllocateFuelAmongDocks(sim, currentGame, ship);
                if (bestCommands == null)
                {
                    FixFuels(sim);
                    bestCommands = sim.GenerateCommands().commands;
                    bestIndex = pathIndex;
                }
                if (isPathGood)
                {
                    (Commands commands, float timeCost) = sim.GenerateCommands();
                    float score = commands.Count + timeCost;
#if PATHPRINT
                    _logger.Info($"Path #{pathIndex} has score {score}={commands.Count}+{timeCost}. {path}");
#endif
                    if (score < bestScore)
                    {
                        bestCommands = commands;
                        bestScore = score;
                        bestIndex = pathIndex;
#if PATHPRINT
                        _logger.Info($"Path #{pathIndex} has score {score}={commands.Count}+{timeCost}. New best score. {path}");
#endif
                    }
                }
            }
            _logger.Info($"Using path #{bestIndex} with score {bestScore}. Checked {pathIndex}/{completePaths.Count} paths");
            return bestCommands;
        }

        private static bool IsTarget(Item it)
        {
            return it.Type == ItemType.Base ||
                    it.Type == ItemType.Waldo ||
                    (it.Type == ItemType.Planet && it.Fuel > 0) ||
                    it.Type == ItemType.Turbo;
        }

        private List<Path> CalculatePathToTargets(Game<DetailedCell> game, long timeout, long lastCallTimeout)
        {
            /**
            - Run A* on targets.
            - early stop criterions:
                - If path does not contain enough fuel, we can discard it. So only keep and build on paths with enough fuel
                - Order target distances by stepcount, so once we reach target out of reach, we can prune further targets on that path.
                - Base becomes viable target on a path only after Waldo is reached.
                - Once an iteration is complete, and we have a valid path, stop.
            - If there are multiple valid paths in an iteration, lower step count wins.
             */

            List<Pos> possibleTargets = game.Items.Where(IsTarget)
                .Select(it => it.Position)
                .ToList();

            var distances = CalculateStrictTargetDistances(game, possibleTargets);
            var waldoDistances = CalculateTargetDistances(game, possibleTargets);
            int waldoToBaseHeuristic = waldoDistances.DistancesTable[game.Waldo.Position][game.Base.Position].steps;

            List<Path> completePaths = new();
            IPriorityQueue<Prio<Path>> pathQueue = new IntervalHeap<Prio<Path>>() {
                new Prio<Path>(0,new Path(game.Base, 0, game.Ship.Fuel, game.MaxFuel, game.Ship.Speed, game.MaxSpeed, Direction.Top, new()))
            };

            int checkedPaths = 0;
            while (pathQueue.Count > 0 &&
                ((completePaths.Count > 0 && _timer.TimeMs() < timeout) || (completePaths.Count == 0)))
            {
                var current = pathQueue.FindMin();
#if PATHPRINT
                _logger.Info($"Checking candidate H: {current.Priority}");
#endif
                Path prevPath = current.Data;
                pathQueue.DeleteMin();

                var others = distances.OrderedSteps[(prevPath.Last.Position, prevPath.Facing)];
                foreach (TargetStepsStore.Entry target in others)
                {
                    if (!prevPath.HasWaldo && target.Pos == game.Base.Position)
                    {
                        continue;
                    }
                    if (target.Steps.Count == 0) // Steps.Count==0 means we dont have path to that target from this pos.
                    {
                        continue;
                    }
                    var stepsToTarget = target.Steps;
                    Direction lastDir = prevPath.Facing;
                    int addedSteps = 0;
                    int commandCount = 0;
                    for (int i = 0; i < stepsToTarget.Count; i++)
                    {
                        int turns = lastDir.CostTo(stepsToTarget[i]);
                        if (turns != 0)
                        {
                            commandCount += turns + 1;
                        }
                        else if (commandCount == 0)
                        {
                            commandCount = 1;
                        }
                        addedSteps += 1 + turns;
                        lastDir = stepsToTarget[i];
                    }
                    Path path = prevPath.Extend(game.Level.ItemAt(target.Pos)!, addedSteps, stepsToTarget, commandCount);

                    if (path.IsValid())
                    {
                        int remainingHeuristic = 0;
                        if (path.Last.Position != game.Base.Position)
                        {
                            remainingHeuristic = (path.HasWaldo) ?
                                waldoDistances.DistancesTable[path.Last.Position][game.Base.Position].steps
                                : waldoDistances.DistancesTable[path.Last.Position][game.Waldo.Position].steps + waldoToBaseHeuristic;
                        }

                        double heuristic = path.StepsValue + Simulator.CalcValue((remainingHeuristic * 1.1), remainingHeuristic, path.Speed);
                        if (path.IsComplete())
                        {
#if PATHPRINT
                            _logger.Info($"Complete path: #{checkedPaths} {path}, H: {heuristic}");
#endif
                            completePaths.Add(path);
                        }
                        else
                        {
                            checkedPaths++;
                            pathQueue.Add(new Prio<Path>(heuristic, path));
#if PATHPRINT
                            _logger.Info($"Valid path: #{checkedPaths} {path}, H: {heuristic}");
#endif
                        }
#if !(DEBUG)
                        if (_timer.TimeMs() > lastCallTimeout)
                        {
                            _logger.Error($"Failed to calc path before timeout. Took {_timer.TimeMs()} ms, checked {checkedPaths} paths");
                            completePaths.Add(path);
                            return completePaths;
                        }
#endif
                    }
                    else
                    {
                        // Targest are oreder by distance. Non Valid is outside fuel range,
                        // so next targets are outside range too, so we can skip them
                        break;
                    }
                }

            }
            _logger.Info($"Found {completePaths.Count}/{checkedPaths} complete paths in {_timer.TimeMs()} ms");
            return completePaths;
        }

        internal struct Prio<D> : IComparable<Prio<D>> where D : class
        {
            public D Data { get; }
            public double Priority { get; }

            public Prio(double priority, D data)
            {
                Data = data;
                Priority = priority;
            }

            public int CompareTo(Prio<D> that)
            {
                return Priority.CompareTo(that.Priority);
            }

            public bool Equals(Prio<D> that)
            {
                // float ==, but okay because here we use it almost as a hash
                return Priority == that.Priority;
            }

            public static Prio<D> operator +(Prio<D> tp, int delta)
            {
                return new Prio<D>(tp.Priority + delta, tp.Data);
            }

            public static Prio<D> operator -(Prio<D> tp, int delta)
            {
                return new Prio<D>(tp.Priority - delta, tp.Data);
            }

            public override string ToString()
            {
                return string.Format("{0}[{1}]", Data, Priority);
            }
        }

        class TargetDistancesStore
        {
            public record struct DistanceEntry(Pos pos, int steps, Direction startFacing, Direction endFacing);
            public Dictionary<Pos, Dictionary<Pos, DistanceEntry>> DistancesTable = new();
        }

        class TargetUnorientedStepsStore
        {
            public record struct Entry(Pos Pos, List<Direction> Steps, int Value);
            public Dictionary<Pos, List<Entry>> OrderedSteps = new();
        }

        class TargetStepsStore
        {
            public record struct Entry(Pos Pos, List<Direction> Steps, int Value);
            public Dictionary<(Pos, Direction), List<Entry>> OrderedSteps = new();
        }

        /// <summary>
        /// Calculate step counts from all possible targets to all other possible targets, and gives the detailed route between targets.
        /// First dict contains starting positions, inner list contains end positions and steps until that pos ordered by lowest steps first.
        /// </summary>
        private TargetStepsStore CalculateStrictTargetDistances(Game<DetailedCell> game, List<Pos> possibleTargets)
        {
            TargetStepsStore result = new();

            Level<DetailedCell> level = game.Level;
            _logger.Debug($"calculating strict target distances for {possibleTargets.Count} targets");
            level.PrintLevel(game.Waldo);
            foreach (Pos target in possibleTargets)
            {
                level.ClearGridDistances();
                CalcStrictStepDistancesFromTarget(level, target);
                //level.PrintStepDistances();

                foreach (var direction in DirectionExtensions.MAIN_DIRECTIONS)
                    result.OrderedSteps[(target, direction)] = new();
                foreach (Pos otherTarget in possibleTargets)
                {
                    if (target == otherTarget) continue;
                    var cell = level.GetGridCell(otherTarget);
                    int valueForPlanet = (cell.Items[0].Type == ItemType.Planet) ? 1 : 0;
                    foreach (var direction in DirectionExtensions.MAIN_DIRECTIONS)
                    {
                        var entry = new TargetStepsStore.Entry(otherTarget, cell.Steps[direction], cell.ValueForDir[direction] + valueForPlanet);
                        result.OrderedSteps[(target, direction)].Add(entry);
                    }
                }
                foreach (var direction in DirectionExtensions.MAIN_DIRECTIONS)
                {
                    result.OrderedSteps[(target, direction)] =
                        result.OrderedSteps[(target, direction)].OrderBy(x => x.Steps.Count).ToList();
                }
            }
            return result;
        }

        private void CalcStrictStepDistancesFromTarget(Level<DetailedCell> level, Pos target)
        {
            Queue<(Pos next, Direction dir, int stepDist)> nextCells = new();

            foreach (Direction direction in DirectionExtensions.MAIN_DIRECTIONS)
            {
                level.GetGridCell(target).ValueForDir[direction] = 0;
                nextCells.Enqueue((target, direction, 1));

                while (nextCells.Any())
                {
                    (Pos pos, Direction dir, int currentVal) = nextCells.Dequeue();
                    List<(Pos, Direction)> nbs = level.GetNeighbours(pos);
                    DetailedCell curCell = level.GetGridCell(pos);
                    foreach (var (nb, nbDir) in nbs)
                    {
                        DetailedCell nbCell = level.GetGridCell(nb);
                        int stepVal = dir.CostTo(nbDir) + 1;
                        if (stepVal == 1)
                        {
                            stepVal = 0;
                        }
                        int pathVal = stepVal + currentVal;
                        if (Simulator.IsPassable(level, nb) &&
                            (nbCell.ValueForDir[direction] < 0 || pathVal < nbCell.ValueForDir[direction]))
                        {
                            nbCell.ValueForDir[direction] = pathVal;
                            nbCell.Steps[direction] = curCell.Steps[direction].Append(nbDir).ToList();
                            if (nbCell.Items.Count == 0 || !IsTarget(nbCell.Items[0]))
                            {
                                nextCells.Enqueue((nb, nbDir, pathVal));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculate step counts from all possible targets to all other possible targets, and gives the detailed route between targets.
        /// The orientation of the ship from the targets are not taken into account for faster computation.
        /// First dict contains starting positions, inner list contains end positions and steps until that pos ordered by lowest steps first.
        /// </summary>
        private TargetUnorientedStepsStore CalculateUnorientedTargetDistances(Game<UnorientedCell> game, List<Pos> possibleTargets)
        {
            TargetUnorientedStepsStore result = new();

            Level<UnorientedCell> level = game.Level;
            _logger.Debug($"calculating strict target distances for {possibleTargets.Count} targets");
            level.PrintLevel(game.Waldo);
            foreach (Pos target in possibleTargets)
            {
                level.ClearGridDistances();
                CalcUnorientedStepDistancesFromTarget(level, target);
                //level.PrintStepDistances();

                result.OrderedSteps[target] = new();
                foreach (Pos otherTarget in possibleTargets)
                {
                    if (target == otherTarget) continue;
                    var cell = level.GetGridCell(otherTarget);
                    int valueForPlanet = (cell.Items[0].Type == ItemType.Planet) ? 1 : 0;
                    foreach (var direction in DirectionExtensions.MAIN_DIRECTIONS)
                    {
                        var entry = new TargetUnorientedStepsStore.Entry(otherTarget, cell.Steps, cell.Value + valueForPlanet);
                        result.OrderedSteps[target].Add(entry);
                    }
                }
                foreach (var direction in DirectionExtensions.MAIN_DIRECTIONS)
                {
                    result.OrderedSteps[target] =
                        result.OrderedSteps[target].OrderBy(x => x.Steps.Count).ToList();
                }
            }
            return result;
        }

        private void CalcUnorientedStepDistancesFromTarget(Level<UnorientedCell> level, Pos target)
        {
            Queue<(Pos next, Direction dir, int stepDist)> nextCells = new();

            level.GetGridCell(target).Value = 0;
            nextCells.Enqueue((target, Direction.None, 1));

            while (nextCells.Any())
            {
                (Pos pos, Direction dir, int currentVal) = nextCells.Dequeue();
                List<(Pos, Direction)> nbs = level.GetNeighbours(pos);
                UnorientedCell curCell = level.GetGridCell(pos);
                foreach (var (nb, nbDir) in nbs)
                {
                    UnorientedCell nbCell = level.GetGridCell(nb);
                    int stepVal = dir.CostTo(nbDir) + 1;
                    if (stepVal == 1)
                    {
                        stepVal = 0;
                    }
                    int pathVal = stepVal + currentVal;
                    if (Simulator.IsPassable(level, nb) &&
                        (nbCell.Value < 0 || pathVal < nbCell.Value))
                    {
                        nbCell.Value = pathVal;
                        nbCell.Steps = curCell.Steps.Append(nbDir).ToList();
                        if (nbCell.Items.Count == 0 || !IsTarget(nbCell.Items[0]))
                        {
                            nextCells.Enqueue((nb, nbDir, pathVal));
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Calculate step counts from all possible targets to all other possible targets.
        /// First dict contains starting positions, inner list contains end positions and steps until that pos ordered by lowest steps first.
        /// </summary>
        private TargetDistancesStore CalculateTargetDistances<CellType>(Game<CellType> game, List<Pos> possibleTargets)
            where CellType : PathCalcCell, new()
        {
            TargetDistancesStore result = new();

            Level<CellType> level = game.Level;
            _logger.Debug($"calculating target distances for {possibleTargets.Count} targets");
            level.PrintLevel(game.Waldo);
            foreach (Pos target in possibleTargets)
            {
                level.ClearGridDistances();
                CalcStepDistancesToTarget(level, target);
                //level.PrintStepDistances();
                //level.PrintFirstStepDirections();

                result.DistancesTable[target] = new();
                foreach (Pos otherTarget in possibleTargets)
                {
                    if (target == otherTarget) continue;
                    var cell = level.GetGridCell(otherTarget);
                    var entry = new TargetDistancesStore.DistanceEntry(
                            otherTarget, cell.StepDistance, cell.FirstStepDirection, cell.LastStepDirection);
                    result.DistancesTable[target].Add(otherTarget, entry);
                }
            }
            return result;
        }

        private void CalcStepDistancesToTarget<CellType>(Level<CellType> level, Pos target)
            where CellType : PathCalcCell, new()
        {
            level.GetGridCell(target).StepDistance = 0;
            Queue<(Pos, Direction)> nextCells = new();
            nextCells.Enqueue((target, Direction.None));

            while (nextCells.Any())
            {
                (Pos pos, Direction dir) = nextCells.Dequeue();
                List<(Pos, Direction)> nbs = level.GetNeighbours(pos);
                int currentDist = level.GetGridCell(pos).StepDistance;
                Direction firstDir = level.GetGridCell(pos).FirstStepDirection;
                foreach (var (nb, nbDir) in nbs)
                {
                    CellType nbCell = level.GetGridCell(nb);
                    if (nbCell.StepDistance < 0 && Simulator.IsPassable(level, nb))
                    {
                        int cellCost = 1;
                        nbCell.StepDistance = currentDist + cellCost + dir.CostTo(nbDir);
                        nbCell.LastStepDirection = nbDir;
                        nbCell.FirstStepDirection = (firstDir != Direction.None) ? firstDir : nbDir;
                        nextCells.Enqueue((nb, nbDir));
                    }
                }
            }
        }

        private void SimulateStepsFromPath(Simulator sim, Path path, Level<Cell> level, Item ship)
        {
            foreach (Direction targetDirection in path.StepsList)
            {
                var posItems = level.GetGridCell(ship.Position).Items;
                if (posItems.Count > 1 && posItems[0].Type == ItemType.Planet && !_didDock)
                {
                    sim.DoCommandDock(500); // TODO: Could do precise fuel calc here now.
                    _didDock = true;
                }

                if (targetDirection != ship.Direction)
                {
                    sim.DoCommandTurn(targetDirection);
                }

                _didDock = false;
                sim.DoCommandForward(ship.Position + targetDirection);
            }
        }

        private bool AllocateFuelAmongDocks(Simulator sim, Game<Cell> game, Item ship)
        {
            int accountedFuel = 0;

            if (ship.Fuel < 0)
            {
                List<Simulator.SimulatedCommandDock> docks = sim.GetDockCommands();

                foreach (var stop in docks)
                {
                    // Lets set every dock to 0 for cleaner calculation
                    stop.Undo();
                }

                int index = 0;
                while (ship.Fuel < 0)
                {
                    if (index >= docks.Count)
                    {
                        _logger.Warn("Spent too much fuel in run, path not good!");
                        return false;
                    }

                    Simulator.SimulatedCommandDock dock = docks[index];

                    int shipFuelAtStop = dock.ShipFuelWhenArrived + accountedFuel;
                    if (shipFuelAtStop < 0)
                    {
                        _logger.Warn($"Underestimated fuel consumption until dock {index}, path not good!");
                        return false;
                    }
                    int maxFuelAtStop = Math.Min(dock.Planet.Fuel, game.MaxFuel - shipFuelAtStop);

                    if (maxFuelAtStop >= (-ship.Fuel))
                    {
                        int duration = -ship.Fuel;
                        int fuel = -ship.Fuel;
                        if (duration < 500)
                        {
                            duration = 500;
                            fuel = Math.Min(maxFuelAtStop, 500);
                        }
                        dock.PostAlterDuration(duration);
                        accountedFuel += fuel;
                        dock.Planet.Fuel -= fuel;
                        ship.Fuel += fuel;
                    }
                    else
                    {
                        int duration = maxFuelAtStop;
                        int fuel = maxFuelAtStop;
                        if (duration < 500)
                        {
                            duration = 500;
                        }
                        dock.PostAlterDuration(duration);
                        ship.Fuel += fuel;
                        dock.Planet.Fuel -= fuel;
                        accountedFuel += fuel - 500;
                    }
                    index++;
                }
                for (; index < docks.Count; index++)
                {
                    Simulator.SimulatedCommandDock dock = docks[index];
                    dock.PostAlterDuration(500); // Have to do minimum duration rest of stops
                }
            }
            return true;
        }

        private void FixFuels(Simulator sim)
        {
            List<Simulator.SimulatedCommandDock> docks = sim.GetDockCommands();

            foreach (var stop in docks)
            {
                if (stop.DockDuration < 500)
                {
                    stop.PostAlterDuration(500);
                }
            }
        }

        private Game<Cell> _game;
        private Timer _timer;
        private bool _didDock = false;
        private readonly NLog.Logger _logger = LogManager.GetCurrentClassLogger();
    }
}
