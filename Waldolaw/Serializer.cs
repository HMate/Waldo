using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waldolaw
{
    public record DistancesJSON(float Distance, string SatelliteName);
    public record ItemJSON(string name, List<DistancesJSON> distances, int fuel);
    public record UserInputsJSON(
        int mapsize,
        int fuel,
        int speed,
        int max_speed,
        int max_fuel,
        List<ItemJSON> items
    );
    public record UserOutputJSON(List<string> Commands);

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
            return JsonConvert.DeserializeObject<UserInputsJSON>(File.ReadAllText(InputFile));
        }

        public void SaveOutputs()
        {
            var outputsJson = JsonConvert.DeserializeObject<UserOutputJSON>(File.ReadAllText(OutputFile));
            File.WriteAllText(OutputFile, JsonConvert.SerializeObject(outputsJson));
        }
    }
}
