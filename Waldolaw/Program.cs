using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;


namespace Waldolaw
{
    class Program
    {
        static void Main(string[] args)
        {
            var inputsPath = args[0];
            var outputPath = args[1];
            try
            {
                Logger logger = LogManager.GetCurrentClassLogger();
                logger.Info("Waldolaw started");

                Serializer serializer = new Serializer(inputsPath, outputPath);
                UserInputsJSON? input = serializer.LoadInputs();
                if (input == null)
                {
                    Console.WriteLine("User inputs is null. Exiting.");
                    return;
                }
                GameBuilder builder = new();
                Game game = builder.Build(input);
                foreach (var item in game.Items)
                {
                    logger.Info($"{item}");
                }

                // TODO: calculate commands
                //outputsJson?.Commands.Add("FORWARD 1");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
