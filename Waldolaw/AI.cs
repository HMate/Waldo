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
            Simulator sim = new(_game);

            Level level = _game.Level;
            Item ship = _game.Ship;
            PrecalcManhattanDistances(_game.Waldo.Position);
            level.PrintLevel(ship);
            level.PrintManhattanDistances();

            while (ship.Position != _game.Waldo.Position)
            {
                DoNextStep(sim, level, ship);
                level.PrintLevel(ship);
            }

            level.ClearGridDistances();
            PrecalcManhattanDistances(_game.Base.Position);
            level.PrintLevel(ship);
            level.PrintManhattanDistances();

            while (ship.Position != _game.Base.Position)
            {
                DoNextStep(sim, level, ship);
                level.PrintLevel(ship);
            }

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
                        break;
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
                        ship.Fuel = 0;
                    }
                    else
                    {
                        dock.PostAlterDuration(dur);
                        ship.Fuel += (dur - 500);
                        index++;
                    }
                }
            }

            return sim.GenerateCommands();
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
                if (cell.manhattanDistance < 0)
                {
                    continue;
                }
                int manhattanCost = cell.manhattanDistance + cell.manhattanDirection.CostTo(dir);
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

        private void PrecalcManhattanDistances(Pos target)
        {
            Level level = _game.Level;

            level.GetGridCell(target).manhattanDistance = 0;
            Queue<(Pos, Direction)> nextCells = new();
            nextCells.Enqueue((target, Direction.None));

            while (nextCells.Any())
            {
                (Pos pos, Direction dir) = nextCells.Dequeue();
                List<(Pos, Direction)> nbs = level.GetNeighbours(pos);
                int currentDist = level.GetGridCell(pos).manhattanDistance;
                Direction currentDir = level.GetGridCell(pos).manhattanDirection;
                foreach (var (nb, nbDir) in nbs)
                {
                    Cell nbCell = level.GetGridCell(nb);
                    if (nbCell.manhattanDistance < 0 && IsPassable(nb))
                    {
                        int cellCost = 1;
                        if (nbCell.Items.ElementAtOrDefault(0)?.Type == ItemType.Planet)
                        {
                            cellCost = 2;
                        }
                        nbCell.manhattanDistance = currentDist + cellCost + dir.CostTo(nbDir);
                        nbCell.manhattanDirection = nbDir.Reverse();
                        nextCells.Enqueue((nb, nbDir));
                    }
                }
            }
        }

        private bool IsPassable(Pos pos)
        {
            return !_game.Level.GetGridCell(pos).Items
                .Exists(it =>
                it.Type == ItemType.Satellite || it.Type == ItemType.Asteroid);
        }

        private Game _game;
        private bool _didDock = false;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    }
}
