﻿using NLog;

namespace Waldolaw
{
    public class AI
    {
        public AI(Game game, Timer timer)
        {
            _game = game;
            _timer = timer;
        }

        public Commands? CalculatePathToWaldo()
        {
            _logger.Debug($"Waldo is at {_game.Waldo.Position}");

            Game currentGame = _game.Copy();

            List<Path> completePaths = CalculatePathToTargets(currentGame);
            _logger.Info($"Found {completePaths.Count} complete paths in {_timer.TimeMs()} ms");

            Commands? bestCommands = null;
            float bestScore = 10000000;
            int pathIndex = 0;
            int bestIndex = -1;

            foreach (var path in completePaths)
            {
                pathIndex++;
#if !(DEBUG)
                if (_timer.TimeMs() > 3850)
                {
                    break;
                }
#endif
                _logger.Debug($"Simulating path #{pathIndex} {path}");
                currentGame = _game.Copy();
                Simulator sim = new(currentGame);
                Level level = currentGame.Level;
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
                    if (score < bestScore)
                    {
                        bestCommands = commands;
                        bestScore = score;
                        bestIndex = pathIndex;
                        _logger.Info($"Path #{pathIndex} has score {score}={commands.Count}+{timeCost}. New best score.");
                    }
                    else
                    {
                        _logger.Info($"Path #{pathIndex} has score {score}={commands.Count}+{timeCost}.");
                    }
                }
            }
            _logger.Info($"Using path #{bestIndex} with score {bestScore}");
            return bestCommands;
        }

        private static bool IsTarget(Item it)
        {
            return it.Type == ItemType.Base ||
                    it.Type == ItemType.Waldo ||
                    (it.Type == ItemType.Planet && it.Fuel > 0) ||
                    it.Type == ItemType.Turbo;
        }

        private List<Path> CalculatePathToTargets(Game game)
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
            SortedList<int, Path> pathQueue = new(new DuplicateKeyComparer<int>()) {
                { 0, new Path(game.Base, 0, game.Ship.Fuel, game.MaxFuel, game.Ship.Speed, game.MaxSpeed, Direction.Top, new()) }
            };
#if DEBUG
            const long timeout = 4000;
#else
            const long timeout = 3000;
#endif
            const long lastCallTimeout = 3700;
            while (pathQueue.Count > 0 &&
                ((completePaths.Count > 0 && _timer.TimeMs() < timeout) || (completePaths.Count == 0)))
            {
                Path prevPath = pathQueue.GetValueAtIndex(0);
                pathQueue.RemoveAt(0);

                var others = distances.OrderedSteps[prevPath.Last.Position];
                foreach (TargetStepsStore.Entry target in others)
                {
                    if (target.Steps.Count == 0 || // Steps.Count==0 means we dont have path to that target from this pos.
                       (!prevPath.HasWaldo && target.Pos == game.Base.Position))
                    {
                        continue;
                    }
                    var stepsToTarget = target.Steps;
                    Direction lastDir = prevPath.Facing;
                    int addedSteps = 0; //prevPath.Facing.CostTo(stepsToTarget.First()) + stepsToTarget.Count;
                    for (int i = 0; i < stepsToTarget.Count; i++)
                    {
                        addedSteps += 1 + lastDir.CostTo(stepsToTarget[i]);
                        lastDir = stepsToTarget[i];
                    }
                    Path path = prevPath.Extend(game.Level.ItemAt(target.Pos)!, addedSteps, stepsToTarget);

                    if (path.IsValid())
                    {
                        int remainingHeuristic = 0;
                        if (path.Last.Position != game.Base.Position)
                        {
                            remainingHeuristic = (path.HasWaldo) ?
                                waldoDistances.DistancesTable[path.Last.Position][game.Base.Position].steps
                                : waldoDistances.DistancesTable[path.Last.Position][game.Waldo.Position].steps + waldoToBaseHeuristic;
                        }

                        int heuristic = path.Steps + remainingHeuristic;
                        if (path.IsComplete())
                        {
#if DEBUG
                            _logger.Info($"Complete path: {path}, H: {heuristic}");
#endif
                            completePaths.Add(path);
                        }
                        else
                        {
                            pathQueue.Add(heuristic, path);
#if DEBUG
                            _logger.Info($"Valid path: {path}, H: {heuristic}");
#endif
                        }
#if !(DEBUG)
                        if (_timer.TimeMs() > lastCallTimeout)
                        {
                            _logger.Error($"Failed to calc path before timeout. Took {_timer.TimeMs()} ms");
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
            return completePaths;
        }

        public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
        {
            public int Compare(TKey? x, TKey? y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }
                int result = x.CompareTo(y);

                if (result == 0)
                    return 1; // Handle equality as being greater. Note: this will break Remove(key) or
                else          // IndexOfKey(key) since the comparer never returns 0 to signal key equality
                    return result;
            }
        }

        class TargetDistancesStore
        {
            public record struct DistanceEntry(Pos pos, int steps, Direction startFacing, Direction endFacing);
            public Dictionary<Pos, List<DistanceEntry>> OrderedDistances = new();
            public Dictionary<Pos, Dictionary<Pos, DistanceEntry>> DistancesTable = new();
        }

        class TargetStepsStore
        {
            public record struct Entry(Pos Pos, List<Direction> Steps);
            public Dictionary<Pos, List<Entry>> OrderedSteps = new();
            public Dictionary<Pos, Dictionary<Pos, Entry>> StepsTable = new();
        }

        /// <summary>
        /// Calculate step counts from all possible targets to all other possible targets.
        /// First dict contains starting positions, inner list contains end positions and steps until that pos ordered by lowest steps first.
        /// </summary>
        private TargetStepsStore CalculateStrictTargetDistances(Game game, List<Pos> possibleTargets)
        {
            TargetStepsStore result = new();

            Level level = game.Level;
            _logger.Debug($"calculating target distances for {possibleTargets.Count} targets");
            level.PrintLevel(game.Waldo);
            foreach (Pos target in possibleTargets)
            {
                _logger.Debug($"calculating target distances from {target}");
                level.ClearGridDistances();
                CalcStrictStepDistancesFromTarget(level, target);
                //level.PrintStepDistances();

                result.OrderedSteps[target] = new();
                result.StepsTable[target] = new();
                foreach (Pos otherTarget in possibleTargets)
                {
                    if (target == otherTarget) continue;
                    var cell = level.GetGridCell(otherTarget);
                    var entry = new TargetStepsStore.Entry(otherTarget, cell.Steps);
                    result.OrderedSteps[target].Add(entry);
                    result.StepsTable[target].Add(otherTarget, entry);
                }
                result.OrderedSteps[target] = result.OrderedSteps[target].OrderBy(x => x.Steps.Count).ToList();
            }
            return result;
        }

        /// <summary>
        /// Calculate step counts from all possible targets to all other possible targets.
        /// First dict contains starting positions, inner list contains end positions and steps until that pos ordered by lowest steps first.
        /// </summary>
        private TargetDistancesStore CalculateTargetDistances(Game game, List<Pos> possibleTargets)
        {
            TargetDistancesStore result = new();

            Level level = game.Level;
            _logger.Debug($"calculating target distances for {possibleTargets.Count} targets");
            level.PrintLevel(game.Waldo);
            foreach (Pos target in possibleTargets)
            {
                _logger.Debug($"calculating target distances from {target}");
                level.ClearGridDistances();
                CalcStepDistancesToTarget(level, target);
                //level.PrintStepDistances();
                //level.PrintFirstStepDirections();

                result.OrderedDistances[target] = new();
                result.DistancesTable[target] = new();
                foreach (Pos otherTarget in possibleTargets)
                {
                    if (target == otherTarget) continue;
                    var cell = level.GetGridCell(otherTarget);
                    var entry = new TargetDistancesStore.DistanceEntry(
                            otherTarget, cell.StepDistance, cell.FirstStepDirection, cell.LastStepDirection);
                    result.OrderedDistances[target].Add(entry);
                    result.DistancesTable[target].Add(otherTarget, entry);
                }
                result.OrderedDistances[target] = result.OrderedDistances[target].OrderBy(x => x.steps).ToList();
            }
            return result;
        }

        private void SimulateStepsFromPath(Simulator sim, Path path, Level level, Item ship)
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

        private void CalcStrictStepDistancesFromTarget(Level level, Pos target)
        {
            level.GetGridCell(target).StepDistance = 0;
            Queue<(Pos next, Direction dir, int stepDist)> nextCells = new();
            nextCells.Enqueue((target, Direction.None, 0));

            while (nextCells.Any())
            {
                (Pos pos, Direction dir, int currentDist) = nextCells.Dequeue();
                List<(Pos, Direction)> nbs = level.GetNeighbours(pos);
                Cell curCell = level.GetGridCell(pos);
                foreach (var (nb, nbDir) in nbs)
                {
                    Cell nbCell = level.GetGridCell(nb);
                    int stepDist = 1 + currentDist + dir.CostTo(nbDir);
                    if (Simulator.IsPassable(level, nb) &&
                        (nbCell.StepDistance < 0 || stepDist < nbCell.StepDistance))
                    {
                        nbCell.StepDistance = stepDist;
                        nbCell.Steps = curCell.Steps.Append(nbDir).ToList();
                        if (nbCell.Items.Count == 0 || !IsTarget(nbCell.Items[0]))
                        {
                            nextCells.Enqueue((nb, nbDir, stepDist));
                        }
                    }
                }
            }
        }

        private void CalcStepDistancesToTarget(Level level, Pos target)
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
                    Cell nbCell = level.GetGridCell(nb);
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

        private bool AllocateFuelAmongDocks(Simulator sim, Game game, Item ship)
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

        private Game _game;
        private Timer _timer;
        private bool _didDock = false;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    }
}
