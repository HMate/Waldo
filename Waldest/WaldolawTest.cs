using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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
            // NLog unittest logging examples:
            // https://github.com/NLog/NLog/blob/43eca983676d87f1d9d9f28872304236393827ba/tests/NLog.UnitTests/Config/TargetConfigurationTests.cs
            GameBuilder builder = new(NullLogger<GameBuilder>.Instance);
            Game game = builder.Build(input);

            game.Level.Size.Should().Be(7);
            game.Items.Count.Should().Be(8);

            game.Waldo.Type.Should().Be(ItemType.Waldo);
            game.Base.Type.Should().Be(ItemType.Base);
            game.Ship.Type.Should().Be(ItemType.Ship);

            game.Waldo.Position.Should().Be(new Pos(1, 2));
            game.Base.Position.Should().Be(new Pos(3, 6));
            game.Ship.Position.Should().Be(new Pos(3, 6));
        }
    }
}