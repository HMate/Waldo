using NLog;
using System.Reflection.Metadata.Ecma335;

namespace Waldolaw
{
    public enum CommandType
    {
        None = 0,
        Forward,
        Turn,
        Dock,
        Name
    }

    public class Commands
    {
        public void AddForward(int steps)
        {
            var command = new Command(CommandType.Forward, steps);
            commands.Add(command);
            _logger.Info($"New Command: {command.ToCommandString()}");
        }
        public void AddTurn(Direction direction)
        {
            if (direction != Direction.Left && direction != Direction.Right)
            {
                _logger.Warn($"Got invalid direction for turn: {direction}");
            }
            Command command = new Command(CommandType.Turn, direction == Direction.Left ? "LEFT" : "RIGHT");
            commands.Add(command);
            _logger.Info($"New Command: {command.ToCommandString()}");
        }
        public void AddDock(int durationMs)
        {
            Command command = new Command(CommandType.Dock, durationMs);
            commands.Add(command);
            _logger.Info($"New Command: {command.ToCommandString()}");
        }
        public void AddName(string name)
        {
            Command command = new Command(CommandType.Name, name);
            commands.Add(command);
            _logger.Info($"New Command: {command.ToCommandString()}");
        }

        public List<string> ToCommandList()
        {
            List<string> result = new();
            foreach (var command in commands)
            {
                result.Add(command.ToCommandString());
            }
            return result;
        }

        private struct Command
        {
            public CommandType Type;
            public int IntParam;
            public string StringParam;

            public Command(CommandType type, int intParam)
            {
                Type = type;
                IntParam = intParam;
                StringParam = "";
            }

            public Command(CommandType type, string stringParam)
            {
                Type = type;
                IntParam = 0;
                StringParam = stringParam;
            }

            public string ToCommandString()
            {
                return Type switch
                {
                    CommandType.Forward => $"FORWARD {IntParam}",
                    CommandType.Turn => $"TURN {StringParam}",
                    CommandType.Dock => $"DOCK {IntParam}",
                    CommandType.Name => $"NAME {StringParam}",
                    _ => throw new NotImplementedException(),
                };
            }
        }

        private List<Command> commands = new();

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    }
}