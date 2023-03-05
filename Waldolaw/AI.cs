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

            var result = new Commands();
            result.AddName("MATE");
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

            Level level = _game.Level;
            Item ship = _game.Ship;
            PrecalcManhattanDistances(_game.Waldo.Position);
            level.PrintLevel();
            level.PrintManhattanDistances();
            while (ship.Position != _game.Waldo.Position)
            {
                CalcNextStep(result, level, ship);
                level.PrintLevel();
            }

            level.ClearGridDistances();
            PrecalcManhattanDistances(_game.Base.Position);
            level.PrintLevel();
            level.PrintManhattanDistances();
            while (ship.Position != _game.Base.Position)
            {
                CalcNextStep(result, level, ship);
                level.PrintLevel();
            }

            return result;
        }

        private static void CalcNextStep(Commands result, Level level, Item ship)
        {
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
            if (target.dir != ship.Direction)
            {
                if (ship.Direction.CostTo(target.dir) == 2)
                {
                    result.AddTurn(Direction.Left);
                    result.AddTurn(Direction.Left);
                }
                else
                {
                    Direction turn = ship.Direction.GetDirectionToTurn(target.dir);
                    result.AddTurn(turn);
                }
                ship.Direction = target.dir;
            }

            result.AddForward(1);
            level.MoveItem(ship, ship.Position, target.pos);
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

        /// <summary>
        /// Step forward cost 1 per distance. Turn left/right takes 1 step.
        /// </summary>
        private int CalcFuelCost(int dist, int speed)
        {
            return dist * (1100 - (speed * 100));
        }

        Game _game;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    }
}
