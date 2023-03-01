using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waldolaw
{
    public record DistancesJSON(int Distance, string SatelliteName);
    public record ItemJSON(string name, List<DistancesJSON> distances, int fuel);
    public record UserInputsJSON(
        int mapsize,
        int fuel,
        int speed,
        int max_speed,
        int max_fuel,
        List<ItemJSON> items
    );

    public record UserOutputJSON
    {
        public List<string> Commands { get; set; } = new();
    }

    public class Serializer
    {
        private string InputFile { get; set; }
        private string OutputFile { get; set; }

        public Serializer(string inputFile, string outputFile)
        {
            InputFile = inputFile;
            OutputFile = outputFile;
        }

        public UserInputsJSON? LoadInputs()
        {
            string text = File.ReadAllText(InputFile);
            return JsonConvert.DeserializeObject<UserInputsJSON>(text);
        }

        public void SaveOutputs(List<string> commands)
        {
            var outputsJson = new UserOutputJSON()
            {
                Commands = commands
            };
            File.WriteAllText(OutputFile, JsonConvert.SerializeObject(outputsJson));
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    }
}
