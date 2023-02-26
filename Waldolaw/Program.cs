using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
                ILogger<Program> logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger<Program>();
                logger.LogInformation("Waldolaw started");

                Serializer serializer = new Serializer(inputsPath, outputPath);
                var data = serializer.LoadInputs();
                if (data == null)
                {
                    Console.WriteLine("User inputs is null. Exiting.");
                    return;
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
