using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Waldolaw
{
    public class AI
    {
        public AI(Game game)
        {
            _game = game;
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

            List<Item> targets = CalculatePathToTargets(currentGame);

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

            bool isPathGood = TellIfPathHaveEnoughFuel(sim, ship);

            return sim.GenerateCommands();
        }

        private List<Item> CalculatePathToTargets(Game game)
        {
            /**
            - Run step weighted BFS on targets.
            - Algo iterations:
                - for every 1 target path, if valid -> OK
                - if not -> for every 2 target path .. etc
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

            int pathLength = 0;
            Path? completePath = null;
            List<Path> prevPaths = new() { new Path(game.Base, 0, game.Ship.Fuel, game.Ship.Speed, Direction.Top) };
            while (completePath == null)
            {
                List<Path> nextPaths = new();
                pathLength++;
                if (pathLength > possibleTargets.Count * 2)
                {
                    throw new Exception($"Failed to calc path before iteration {pathLength}");
                }

                foreach (Path prevPath in prevPaths)
                {
                    var others = distances[prevPath.Last.Position];
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
                            nextPaths.Add(path);
                            if (path.IsComplete())
                            {
                                _logger.Info($"Complete path: {path}");
                                if ((completePath == null || path.Steps < completePath.Steps))
                                {
                                    completePath = path;
                                }
                            }
                            else
                            {
                                _logger.Info($"Valid path: {path}");
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                prevPaths = nextPaths;
            }
            return completePath.Nodes;
        }

        /// <summary>
        /// Calculate step counts from all possible targets to all other possible targets.
        /// First dict contains starting positions, inner list contains end positions and steps until that pos ordered by lowest steps first.
        /// </summary>
        private Dictionary<Pos, List<(Pos pos, int steps, Direction startFacing, Direction endFacing)>>
            CalculateTargetDistances(Game game, List<Pos> possibleTargets)
        {
            Dictionary<Pos, List<(Pos pos, int steps, Direction startFacing, Direction endFacing)>> result = new();

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

                result[target] = new();
                foreach (Pos otherTarget in possibleTargets)
                {
                    if (target == otherTarget) continue;
                    var cell = level.GetGridCell(otherTarget);
                    result[target].Add((otherTarget, cell.StepDistance, cell.FirstStepDirection, cell.LastStepDirection));
                }
                result[target] = result[target].OrderBy(x => x.steps).ToList();
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
                    if (nbCell.StepDistance < 0 && IsPassable(level, nb))
                    {
                        int cellCost = 1;
                        // TODO: For now we dont care about time constraint. Planet does not increase fuel cost.
                        //if (nbCell.Items.ElementAtOrDefault(0)?.Type == ItemType.Planet)
                        //{
                        //    cellCost = 2;
                        //}
                        nbCell.StepDistance = currentDist + cellCost + dir.CostTo(nbDir);
                        nbCell.LastStepDirection = nbDir;
                        nbCell.FirstStepDirection = (firstDir != Direction.None) ? firstDir : nbDir;
                        nextCells.Enqueue((nb, nbDir));
                    }
                }
            }
        }


        private bool TellIfPathHaveEnoughFuel(Simulator sim, Item ship)
        {
            int accountedFuel = 0;
            if (ship.Fuel < 0)
            {
                List<Simulator.SimulatedCommandDock> docks = sim.GetDockCommands();
                HashSet<Pos> visitedDocks = new();
                int index = 0;
                while (ship.Fuel < 0)
                {
                    if (index >= docks.Count)
                    {
                        _logger.Warn("Spent too much fuel in run!!");
                        return false;
                    }

                    Simulator.SimulatedCommandDock dock = docks[index];
                    if (visitedDocks.Contains(dock.Planet.Position))
                    {
                        index++;
                        continue;
                    }
                    visitedDocks.Add(dock.Planet.Position);

                    int dur = dock.GetPossibleMaxDuration();
                    if ((dur - 500) > (-ship.Fuel))
                    {
                        dock.PostAlterDuration(-ship.Fuel + 500);
                        accountedFuel += (-ship.Fuel + 500);
                        ship.Fuel = 0;
                    }
                    else
                    {
                        dock.PostAlterDuration(dur);
                        ship.Fuel += (dur - 500);
                        accountedFuel += (dur - 500);
                        index++;
                    }
                }
            }
            _logger.Warn($"Accounted fuel: {accountedFuel}");
            return true;
        }

        private bool IsPassable(Level level, Pos pos)
        {
            return !level.GetGridCell(pos).Items
                .Exists(it =>
                it.Type == ItemType.Satellite || it.Type == ItemType.Asteroid);
        }

        private Game _game;
        private bool _didDock = false;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    }
}
