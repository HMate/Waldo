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
    public enum TurnDirection
    {
        Left = 0,
        Right,
    }

    public class Commands
    {
        public void AddForward(int steps)
        {
            commands.Add(new Command(CommandType.Forward, steps));
        }
        public void AddTurn(TurnDirection direction)
        {
            commands.Add(new Command(CommandType.Turn, direction == TurnDirection.Left ? "LEFT" : "RIGHT"));
        }
        public void AddDock(int durationMs)
        {
            commands.Add(new Command(CommandType.Dock, durationMs));
        }
        public void AddName(string name)
        {
            commands.Add(new Command(CommandType.Name, name));
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
    }
}