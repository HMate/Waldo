using CommunityToolkit.Diagnostics;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Waldolaw
{
    public class Simulator
    {
        public abstract class SimulatedCommandBase
        {
            public CommandType Type;
            public abstract void Do();
            public abstract void Undo();
            public abstract List<Commands.Command> ToCommands();
        }

        public Simulator(Game game)
        {
            _gameState = game;
        }


        public void DoCommandForward(Pos targetPos)
        {
            SimulatedCommandForward command = new(_gameState, targetPos);
            _commands.Add(command);
            command.Do();
        }

        public void DoCommandTurn(Direction targetDir)
        {
            SimulatedCommandTurn command = new(_gameState, targetDir);
            _commands.Add(command);
            command.Do();
        }

        public void DoCommandDock(int dockDuration)
        {

            SimulatedCommandDock command = new(_gameState, dockDuration);
            _commands.Add(command);
            command.Do();
        }

        public void UndoLastCommand()
        {

        }

        /// <summary>
        /// Step forward cost 1 per distance. Turn left/right takes 1 step.
        /// </summary>
        public static int CalcFuelCost(int step, int speed)
        {
            return speed * CalcTimeCost(step, speed);
        }

        /// <summary>
        /// 1 Step is going forward 1 tile, or turn left/right once.
        /// </summary>
        public static int CalcTimeCost(int step, int speed)
        {
            return step * (1100 - (speed * 100));
        }

        internal Commands GenerateCommands()
        {
            Commands commands = new Commands();
            commands.AddName("MATE");

            foreach (var command in _commands)
            {
                foreach (var cmd in command.ToCommands())
                {
                    commands.AddCommand(cmd);
                }
            }
            return commands;
        }

        private List<SimulatedCommandBase> _commands = new();
        private Game _gameState;



        public class SimulatedCommandForward : SimulatedCommandBase
        {
            public SimulatedCommandForward(Game game, Pos targetPos)
            {
                Type = CommandType.Forward;
                _game = game;
                _targetPos = targetPos;
            }

            private Game _game;
            private Pos _targetPos;
            private Pos _origPos;
            private int _steps = 0;

            public override void Do()
            {
                Item ship = _game.Ship;
                _steps = Pos.HammingDist(_targetPos, ship.Position);
                Guard.IsEqualTo(_steps, 1);

                _origPos = ship.Position;
                _game.Level.MoveItem(ship, _origPos, _targetPos);
                ship.Fuel -= CalcFuelCost(_steps, ship.Speed);

                // TODO: simulate pickup turbo
            }

            public override void Undo()
            {
                Item ship = _game.Ship;
                _game.Level.MoveItem(ship, _targetPos, _origPos);
                ship.Fuel += CalcFuelCost(_steps, ship.Speed);
            }

            public override List<Commands.Command> ToCommands()
            {
                return new List<Commands.Command> { Commands.CreateForward(_steps) };
            }
        }

        public class SimulatedCommandTurn : SimulatedCommandBase
        {
            public SimulatedCommandTurn(Game game, Direction targetDir)
            {
                Type = CommandType.Turn;
                _game = game;
                _targetDir = targetDir;
                _originalDir = _game.Ship.Direction;
            }

            private Game _game;
            private Direction _targetDir;
            private Direction _originalDir;
            private Direction _turnDir = Direction.None;
            private int _steps = 0;

            public override void Do()
            {
                if (_game.Ship.Direction.CostTo(_targetDir) == 2)
                {
                    _steps = 2;
                }
                else
                {
                    _turnDir = _game.Ship.Direction.GetDirectionToTurn(_targetDir);
                    _steps = 1;
                }
                _game.Ship.Direction = _targetDir;
            }

            public override void Undo()
            {
                _game.Ship.Direction = _originalDir;
            }

            public override List<Commands.Command> ToCommands()
            {
                if (_steps == 2)
                    return new List<Commands.Command> { Commands.CreateTurn(Direction.Left), Commands.CreateTurn(Direction.Left) };
                else
                    return new List<Commands.Command> { Commands.CreateTurn(_turnDir) };
            }
        }

        public class SimulatedCommandDock : SimulatedCommandBase
        {
            public SimulatedCommandDock(Game game, int dockDuration)
            {
                Type = CommandType.Dock;
                _game = game;
                _dockDuration = dockDuration;
            }

            private Game _game;
            private int _dockDuration;
            private int _realDockDuration;

            public override void Do()
            {
                Item ship = _game.Ship;
                Item planet = _game.Level.GetGridCell(ship.Position).Items[0];
                Guard.IsEqualTo((int)planet.Type, (int)ItemType.Planet);
                _realDockDuration = Math.Min(_dockDuration, planet.Fuel);
                planet.Fuel -= _realDockDuration;

                ship.Fuel += _realDockDuration;
            }

            public override void Undo()
            {
                Item ship = _game.Ship;
                Item planet = _game.Level.GetGridCell(ship.Position).Items[0];
                Guard.IsEqualTo((int)planet.Type, (int)ItemType.Planet);
                planet.Fuel += _realDockDuration;
                ship.Fuel -= _realDockDuration;
            }

            public override List<Commands.Command> ToCommands()
            {
                return new List<Commands.Command> { Commands.CreateDock(_dockDuration) };
            }
        }
    }
}
