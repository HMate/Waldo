using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;



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
