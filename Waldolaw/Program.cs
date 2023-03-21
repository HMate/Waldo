
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;


namespace Waldolaw
{
    class Program
    {
        static void Main(string[] args)
        {
            Timer timer = new();

            var inputsPath = args[0];
            var outputPath = args[1];

            string layout = "${longdate}|${level:uppercase=true}|${logger}| ${message:withException=true}";
            Logger logger = NLog.LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().WriteToConsole(layout, writeBuffered: true);
                builder.ForLogger().WriteToDebugConditional(layout);
                builder.ForLogger().WriteToFile(fileName: "waldolaw.log", layout, archiveAboveSize: 10000000, maxArchiveFiles: 5);
            }).GetCurrentClassLogger();

            try
            {
                logger.Info("😎😎😎 Waldolaw started 🌍🌍🌍");

                Serializer serializer = new Serializer(inputsPath, outputPath);
                UserInputsJSON? input = serializer.LoadInputs();
                if (input == null)
                {
                    logger.Warn("User inputs is null. Exiting.");
                    return;
                }
                GameBuilder builder = new();
                Game<Cell> game = builder.Build(input);

                Commands? commands = new AI(game, timer).CalculatePathToWaldo();

                if (commands != null)
                    serializer.SaveOutputs(commands.ToCommandList());
                else
                    logger.Error("Couldn't find a solution :(");

            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

            logger.Info("Waldolaw took {} ms to run", timer.TimeMs());
        }
    }
}
