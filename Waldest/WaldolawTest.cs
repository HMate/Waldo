using Waldolaw;
using Xunit;

namespace Waldest
{
    public class WaldolawTest
    {
        [Fact]
        public void LevelBuilderTest()
        {
            UserInputsJSON input = new(mapsize: 7, fuel: 200, speed: 1, max_fuel: 10000, max_speed: 6,
                items: new List<ItemJSON> {
                    new ItemJSON(name: "Waldo_23", fuel: -1,
                        distances: new List<DistancesJSON> {
                            new DistancesJSON(Distance: 5, SatelliteName: "SAT_2"),
                            new DistancesJSON(Distance: 9, SatelliteName: "SAT_3"),
                            new DistancesJSON(Distance: 3, SatelliteName: "SAT_0"),
                            new DistancesJSON(Distance: 7, SatelliteName: "SAT_1"),
                        } ),
                    new ItemJSON(name: "PLANET_f2", fuel: 5000,
                        distances: new List<DistancesJSON> {
                            new DistancesJSON(Distance: 4, SatelliteName: "SAT_2"),
                            new DistancesJSON(Distance: 6, SatelliteName: "SAT_3"),
                            new DistancesJSON(Distance: 6, SatelliteName: "SAT_0"),
                            new DistancesJSON(Distance: 8, SatelliteName: "SAT_1"),
                        } ),
                }
            );
            GameBuilder builder = new();
            Game game = builder.Build(input);

            Assert.Equal(7, game.Level.Size);
            Assert.Equal(8, game.Items.Count);
        }
    }
}