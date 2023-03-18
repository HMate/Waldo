using CommunityToolkit.Diagnostics;
using NLog;

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
        public int Count { get; private set; }

        public void AddForward(int steps)
        {
            AddCommand(CreateForward(steps));
        }
        public void AddTurn(Direction direction)
        {
            AddCommand(CreateTurn(direction));
        }
        public void AddDock(int durationMs)
        {
            AddCommand(CreateDock(durationMs));
        }
        public void AddName(string name)
        {
            AddCommand(new Command(CommandType.Name, name));
        }

        public void AddCommand(Command command)
        {
            commands.Add(command);
            if (command.Type != CommandType.Name)
            {
                Count += 1;
            }
        }

        public static Command CreateForward(int steps)
        {
            return new Command(CommandType.Forward, steps);
        }
        public static Command CreateTurn(Direction direction)
        {
            if (direction != Direction.Left && direction != Direction.Right)
            {
                new Commands()._logger.Warn($"Got invalid direction for turn: {direction}");
            }
            return new Command(CommandType.Turn, direction == Direction.Left ? "LEFT" : "RIGHT");
        }
        public static Command CreateDock(int durationMs)
        {
            Guard.IsGreaterThanOrEqualTo(durationMs, 500);
            return new Command(CommandType.Dock, durationMs);
        }

        public List<string> ToCommandList()
        {
            List<string> result = new();
            foreach (var command in commands)
            {
                result.Add(command.ToCommandString());
                _logger.Info($"New Command: {command.ToCommandString()}");
            }
            return result;
        }

        public struct Command
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