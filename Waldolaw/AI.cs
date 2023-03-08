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

            //if (_game.Waldo.Position == new Pos(1, 2))
            //{
            //    result.AddForward(4);
            //    result.AddTurn(TurnDirection.Left);
            //    result.AddForward(2);
            //    result.AddTurn(TurnDirection.Left);
            //    result.AddForward(4);
            //    result.AddTurn(TurnDirection.Left);
            //    result.AddForward(2);
            //}
            Simulator sim = new(_game);

            Level level = _game.Level;
            Item ship = _game.Ship;
            PrecalcManhattanDistances(_game.Waldo.Position);
            level.PrintLevel(ship);
            level.PrintManhattanDistances();
            _estimatedStepsToFinish = level.GetGridCell(_game.Base.Position).manhattanDistance * 2;

            while (ship.Position != _game.Waldo.Position)
            {
                DoNextStep(sim, level, ship);
                level.PrintLevel(ship);
            }

            level.ClearGridDistances();
            PrecalcManhattanDistances(_game.Base.Position);
            level.PrintLevel(ship);
            level.PrintManhattanDistances();
            _estimatedStepsToFinish = level.GetGridCell(_game.Base.Position).manhattanDistance;

            while (ship.Position != _game.Base.Position)
            {
                DoNextStep(sim, level, ship);
                level.PrintLevel(ship);
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
                // TODO: IF planet has less fuel, calculate that. 
                var dockDuration = Math.Max(500, Simulator.CalcFuelCost(_estimatedStepsToFinish, ship.Speed));
                sim.DoCommandDock(dockDuration);
                _didDock = true;
                return;
            }

            List<(Pos, Direction)> nbs = level.GetNeighbours(ship.Position);
            (int cost, Pos pos, Direction dir) target = (1000000, ship.Position, ship.Direction);
            foreach ((Pos nb, Direction dir) in nbs)
            {
                if (level.GetGridCell(nb).manhattanDistance < 0)
                {
                    continue;
                }
                int stepCost = level.GetGridCell(nb).manhattanDistance + 1 + ship.Direction.CostTo(dir);
                if (stepCost < target.cost)
                {
                    target = (stepCost, nb, dir);
                }
            }

            // Turn if needed
            int steps = 1;
            if (target.dir != ship.Direction)
            {
                sim.DoCommandTurn(target.dir);
            }

            _didDock = false;
            _estimatedStepsToFinish -= steps;
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
                foreach (var (nb, nbDir) in nbs)
                {
                    Cell nbCell = level.GetGridCell(nb);
                    if (nbCell.manhattanDistance < 0 && IsPassable(nb))
                    {
                        int cellCost = 1;
                        if (nbCell.Items.ElementAtOrDefault(0)?.Type == ItemType.Planet)
                        {
                            cellCost = 3;
                        }
                        nbCell.manhattanDistance = currentDist + cellCost + dir.CostTo(nbDir);
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
        private int _estimatedStepsToFinish = 0;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    }
}
