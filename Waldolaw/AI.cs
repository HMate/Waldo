using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static Waldolaw.AI;

namespace Waldolaw
{
    public class AI
    {
        public AI(Game game, Timer timer)
        {
            _game = game;
            _timer = timer;
        }

        public Commands CalculatePathToWaldo()
        {
            _logger.Debug($"Waldo is at {_game.Waldo.Position}");

            //{
            //    Commands result = new Commands();
            //    result.AddForward(4);
            //    result.AddTurn(TurnDirection.Left);
            //    result.AddForward(2);
            //    result.AddTurn(TurnDirection.Left);
            //    result.AddForward(4);
            //    result.AddTurn(TurnDirection.Left);
            //    result.AddForward(2);
            //    return result;
            //}

            //{
            //    Commands result = new Commands();
            //    result.AddForward(3);
            //    result.AddDock(500);
            //    result.AddForward(1);
            //    result.AddTurn(Direction.Left);
            //    result.AddTurn(Direction.Left);
            //    result.AddForward(1);
            //    result.AddDock(500);
            //    result.AddForward(3);
            //    return result;
            //}
            Game currentGame = _game.Copy();

            List<Path> completePaths = CalculatePathToTargets(currentGame);
            _logger.Info($"Found {completePaths.Count} complete paths in {_timer.TimeMs()} ms");

            List<Item> targets = completePaths[0].Nodes.Skip(1).ToList(); // First is Base itself

            Simulator sim = new(currentGame);

            //List<Item> targets = new() { currentGame.Waldo, currentGame.Base };

            Level level = currentGame.Level;
            Item ship = currentGame.Ship;

            foreach (Item target in targets)
            {
                _logger.Debug($"Going to target {target.Name} at {target.Position}");
                level.ClearGridDistances();
                CalcStepDistancesToTarget(currentGame.Level, target.Position);
                level.PrintLevel(ship);
                level.PrintStepDistances();

                int deb = 7;
                while (ship.Position != target.Position)
                {
                    DoNextStep(sim, level, ship);
                    level.PrintLevel(ship);
                }
            }

            bool isPathGood = TellIfPathHaveEnoughFuel(sim, currentGame, ship);

            return sim.GenerateCommands();
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

            List<Pos> possibleTargets = game.Items.Where(it =>
                    it.Type == ItemType.Base ||
                    it.Type == ItemType.Waldo ||
                    (it.Type == ItemType.Planet && it.Fuel > 0) ||
                    it.Type == ItemType.Turbo)
                .Select(it => it.Position)
                .ToList();
            var distances = CalculateTargetDistances(game, possibleTargets);

            int waldoToBaseHeuristic = distances.DistancesTable[game.Waldo.Position][game.Base.Position].steps;


            List<Path> completePaths = new();
            SortedList<int, Path> pathQueue = new(new DuplicateKeyComparer<int>()) {
                { 0, new Path(game.Base, 0, game.Ship.Fuel, game.Ship.Speed, Direction.Top) }
            };
            while (completePaths.Count == 0 && pathQueue.Count > 0)
            {
                Path prevPath = pathQueue.GetValueAtIndex(0);
                pathQueue.RemoveAt(0);

                var others = distances.OrderedDistances[prevPath.Last.Position];
                foreach (var node in others)
                {
                    if (prevPath.Contains(node.pos) && !prevPath.StillWorthVisit(node.pos))
                    {
                        continue;
                    }
                    int addedSteps = prevPath.Facing.CostTo(node.startFacing) + node.steps;
                    Path path = prevPath.Extend(game.Level.ItemAt(node.pos)!, addedSteps, node.endFacing);

                    if (path.IsValid())
                    {
                        int remainingHeuristic = 0;
                        if (path.Last.Position != game.Base.Position)
                        {
                            remainingHeuristic = (path.HasWaldo) ?
                                distances.DistancesTable[path.Last.Position][game.Base.Position].steps
                                : distances.DistancesTable[path.Last.Position][game.Waldo.Position].steps + waldoToBaseHeuristic;
                        }

                        int heuristic = path.Steps + remainingHeuristic;
                        pathQueue.Add(heuristic, path);
                        if (path.IsComplete())
                        {
                            _logger.Info($"Complete path: {path}, H: {heuristic}");
                            completePaths.Add(path);
                        }
                        else
                        {
                            _logger.Info($"Valid path: {path}, H: {heuristic}");
                        }
                        if (_timer.TimeMs() > 3950)
                        {
                            _logger.Error($"Failed to calc path before timeout. Took {_timer.TimeMs()} ms");
                            completePaths.Add(path);
                            return completePaths;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

            }
            // completePaths = completePaths.OrderBy(p => p.Steps).ToList();
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

        private void DoNextStep(Simulator sim, Level level, Item ship)
        {
            var posItems = level.GetGridCell(ship.Position).Items;
            if (posItems.Count > 1 && posItems[0].Type == ItemType.Planet && !_didDock)
            {
                // TODO: We have to foresee how many planets are on our way.
                // If this is the last, we should tank up as man fuel as possible. If that is enough to finish the level, OK.
                // If that is not enough, we have to compute path to next planet. - case if that is before/after waldo.
                // Have to compute how much fuel we need until finish. For that we want to have full route to waldo and back.
                // Separate DecideGoal+CalcNextStep action and DoNextStep actions.
                sim.DoCommandDock(500); // Do minimal, calc actual values later once we know the route
                _didDock = true;
                return;
            }

            List<(Pos, Direction)> nbs = level.GetNeighbours(ship.Position);
            (int cost, Pos pos, Direction dir) target = (1000000, ship.Position, ship.Direction);
            foreach ((Pos nb, Direction dir) in nbs)
            {
                var cell = level.GetGridCell(nb);
                if (cell.StepDistance < 0)
                {
                    continue;
                }
                int manhattanCost = cell.StepDistance + cell.LastStepDirection.Reverse().CostTo(dir);
                int stepCost = manhattanCost + 1 + ship.Direction.CostTo(dir);
#if DEBUG
                _logger.Debug($"Step cost to {dir} is {stepCost}");
#endif
                if (stepCost < target.cost)
                {
                    target = (stepCost, nb, dir);
                }
            }

            if (target.dir != ship.Direction)
            {
                sim.DoCommandTurn(target.dir);
            }

            _didDock = false;
            sim.DoCommandForward(target.pos);
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

        private bool TellIfPathHaveEnoughFuel(Simulator sim, Game game, Item ship)
        {
            int accountedFuel = 0;
            _logger.Warn($"Ship fuel deficit: {ship.Fuel}");
            if (ship.Fuel < 0)
            {
                List<Simulator.SimulatedCommandDock> docks = sim.GetDockCommands();

                foreach (var stop in docks)
                {
                    // Lets set every dock to 0 for cleaner calculation
                    stop.Undo();
                }
                _logger.Warn($"Ship fuel deficit after dock resets: {ship.Fuel}");

                int index = 0;
                while (ship.Fuel < 0)
                {
                    if (index >= docks.Count)
                    {
                        _logger.Warn("Spent too much fuel in run!!");
                        return false;
                    }

                    Simulator.SimulatedCommandDock dock = docks[index];

                    int shipFuelAtStop = dock.ShipFuelWhenArrived + accountedFuel;
                    if (shipFuelAtStop < 0)
                    {
                        _logger.Warn($"Underestimated fuel consumption until dock {index}");
                        return false;
                    }
                    int maxDurAtStop = Math.Min(dock.Planet.Fuel, game.MaxFuel - shipFuelAtStop);

                    if (maxDurAtStop >= (-ship.Fuel))
                    {
                        dock.PostAlterDuration(-ship.Fuel);
                        accountedFuel += -ship.Fuel;
                        dock.Planet.Fuel -= -ship.Fuel;
                        ship.Fuel = 0;
                    }
                    else
                    {
                        dock.PostAlterDuration(maxDurAtStop);
                        ship.Fuel += maxDurAtStop;
                        dock.Planet.Fuel -= maxDurAtStop;
                        accountedFuel += maxDurAtStop - 500;
                    }
                    index++;
                }
                for (; index < docks.Count; index++)
                {
                    Simulator.SimulatedCommandDock dock = docks[index];
                    dock.PostAlterDuration(500); // Have to do minimum duration rest of stops
                }
            }
            _logger.Warn($"Accounted fuel: {accountedFuel}");
            return true;
        }

        private Game _game;
        private Timer _timer;
        private bool _didDock = false;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    }
}
