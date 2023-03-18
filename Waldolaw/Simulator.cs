using CommunityToolkit.Diagnostics;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
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
            public abstract int TimeCost();
            public abstract List<Commands.Command> ToCommands();
        }

        public Simulator(Game<Cell> game)
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

        public static bool IsPassable<CellT>(Level<CellT> level, Pos pos) where CellT : Cell, new()
        {
            return !level.GetGridCell(pos).Items
                .Exists(it =>
                it.Type == ItemType.Satellite || it.Type == ItemType.Asteroid);
        }

        public List<SimulatedCommandDock> GetDockCommands()
        {
            return _commands.Where(c => c.Type == CommandType.Dock).Select(c => (SimulatedCommandDock)c).ToList();
        }

        internal (Commands commands, float timeCost) GenerateCommands()
        {
            Commands commands = new Commands();
            commands.AddName("MATE");
            int timeCost = 0;

            int forwardCount = 0;
            foreach (var command in _commands)
            {
                timeCost += command.TimeCost();
                foreach (var cmd in command.ToCommands())
                {
                    if (cmd.Type != CommandType.Forward)
                    {
                        if (forwardCount > 0)
                        {
                            commands.AddCommand(Commands.CreateForward(forwardCount));
                            forwardCount = 0;
                        }
                        commands.AddCommand(cmd);
                    }
                    else
                    {
                        forwardCount++;
                    }
                }
            }
            if (forwardCount > 0)
            {
                commands.AddCommand(Commands.CreateForward(forwardCount));
                forwardCount = 0;
            }
            return (commands, timeCost / 1000.0f);
        }

        private List<SimulatedCommandBase> _commands = new();
        private Game<Cell> _gameState;



        public class SimulatedCommandForward : SimulatedCommandBase
        {
            public SimulatedCommandForward(Game<Cell> game, Pos targetPos)
            {
                Type = CommandType.Forward;
                _game = game;
                _targetPos = targetPos;
            }

            private Game<Cell> _game;
            private Pos _targetPos;
            private Pos _origPos;
            private int _steps = 0;
            private Item? _pickedTurbo = null;
            private int _timeCost = 0;

            public override void Do()
            {
                _steps = Pos.HammingDist(_targetPos, _game.Ship.Position);
                Guard.IsEqualTo(_steps, 1);

                _origPos = _game.Ship.Position;
                _game.Level.MoveItem(_game.Ship, _origPos, _targetPos);
                _game.Ship.Fuel -= CalcFuelCost(_steps, _game.Ship.Speed);
                _timeCost = CalcTimeCost(_steps, _game.Ship.Speed);

                // simulate pickup turbo
                var targetItems = _game.Level.GetGridCell(_targetPos).Items;
                if (targetItems.Count > 0
                    && targetItems[0].Type == ItemType.Turbo
                    && _game.Ship.Speed < _game.MaxSpeed)
                {
                    _pickedTurbo = targetItems[0];
                    _game.Ship.Speed += 1;
                    _game.Level.RemoveItem(_targetPos, targetItems[0]);
                }
            }

            public override void Undo()
            {
                if (_pickedTurbo != null)
                {
                    _game.Ship.Speed -= 1;
                    _game.Level.PlaceItem(_targetPos, _pickedTurbo);
                }
                _game.Level.MoveItem(_game.Ship, _targetPos, _origPos);
                _game.Ship.Fuel += CalcFuelCost(_steps, _game.Ship.Speed);
            }

            public override List<Commands.Command> ToCommands()
            {
                return new List<Commands.Command> { Commands.CreateForward(_steps) };
            }

            public override int TimeCost() => _timeCost;
        }

        public class SimulatedCommandTurn : SimulatedCommandBase
        {
            public SimulatedCommandTurn(Game<Cell> game, Direction targetDir)
            {
                Type = CommandType.Turn;
                _game = game;
                _targetDir = targetDir;
                _originalDir = _game.Ship.Direction;
            }

            private Game<Cell> _game;
            private Direction _targetDir;
            private Direction _originalDir;
            private Direction _turnDir = Direction.None;
            private int _steps = 0;
            private int _timeCost = 0;

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
                _game.Ship.Fuel -= CalcFuelCost(_steps, _game.Ship.Speed);
                _game.Ship.Direction = _targetDir;
                _timeCost = CalcTimeCost(_steps, _game.Ship.Speed);
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

            public override int TimeCost() => _timeCost;
        }

        public class SimulatedCommandDock : SimulatedCommandBase
        {
            public SimulatedCommandDock(Game<Cell> game, int dockDuration)
            {
                Type = CommandType.Dock;
                _game = game;
                DockDuration = dockDuration;
                Planet = _game.Level.GetGridCell(_game.Ship.Position).Items[0];
                Guard.IsEqualTo((int)Planet.Type, (int)ItemType.Planet);
                ShipFuelWhenArrived = game.Ship.Fuel;
            }

            public Item Planet { get; private set; }
            public int ShipFuelWhenArrived { get; private set; }
            public int DockDuration { get; private set; }

            private Game<Cell> _game;
            private int _timeCost = 0;

            public override void Do()
            {
                DockDuration = Math.Min(DockDuration, Planet.Fuel);
                Planet.Fuel -= DockDuration;
                _game.Ship.Fuel += DockDuration;
                _timeCost = DockDuration;
            }

            public override void Undo()
            {
                Planet.Fuel += DockDuration;
                _game.Ship.Fuel -= DockDuration;
            }

            /// <summary>
            /// Alter duration of docking without recalculating simluation costs.
            /// </summary>
            public void PostAlterDuration(int duration)
            {
                DockDuration = duration;
            }

            public override List<Commands.Command> ToCommands()
            {
                return new List<Commands.Command> { Commands.CreateDock(DockDuration) };
            }

            public override int TimeCost() => DockDuration;
        }
    }
}
