using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Waldolaw;
using Xunit;

namespace Waldest
{
    public class GameBuilderTest
    {
        [Fact]
        public void level_builder_loads_items()
        {
            UserInputsJSON input = new(mapsize: 7, fuel: 200, speed: 1, max_fuel: 10000, max_speed: 6,
                items: new List<ItemJSON> {
                    AddItem("Waldo_23", -1, dist0: 3, 7, 5, 9),
                    AddItem("PLANET_f2", 5000, dist0: 6, 8, 4, 6),
                }
            );
            // NLog unittest logging examples:
            // https://github.com/NLog/NLog/blob/43eca983676d87f1d9d9f28872304236393827ba/tests/NLog.UnitTests/Config/TargetConfigurationTests.cs
            GameBuilder builder = new();
            Game game = builder.Build(input);

            game.Items.Count.Should().Be(8);

            game.Waldo.Type.Should().Be(ItemType.Waldo);
            game.Base.Type.Should().Be(ItemType.Base);
            game.Ship.Type.Should().Be(ItemType.Ship);

            game.Waldo.Position.Should().Be(new Pos(1, 2));
            game.Base.Position.Should().Be(new Pos(3, 6));
            game.Ship.Position.Should().Be(new Pos(3, 6));
        }

        [Fact]
        public void level_builder_populates_level()
        {
            UserInputsJSON input = new(mapsize: 7, fuel: 200, speed: 1, max_fuel: 10000, max_speed: 6,
                items: new List<ItemJSON> {
                    AddItem("Waldo_23", -1, dist0: 3, 7, 5, 9),
                    AddItem("PLANET_f2", 5000, dist0: 6, 8, 4, 6),
                }
            );

            GameBuilder builder = new();
            Game game = builder.Build(input);

            game.Level.Size.Should().Be(7);
            game.Level.ItemAt(new Pos(1, 2)).Should().Be(game.Waldo);
            game.Level.ItemAt(new Pos(3, 6)).Should().Be(game.Base);
            game.Level.ItemAt(new Pos(2, 4))!.Type.Should().Be(ItemType.Planet);

        }

        private ItemJSON AddItem(string name, int fuel, int dist0, int dist1, int dist2, int dist3)
        {
            return new ItemJSON(name: name, fuel: fuel,
                        distances: new List<DistancesJSON> {
                            new DistancesJSON(Distance: dist2, SatelliteName: "SAT_2"),
                            new DistancesJSON(Distance: dist3, SatelliteName: "SAT_3"),
                            new DistancesJSON(Distance: dist0, SatelliteName: "SAT_0"),
                            new DistancesJSON(Distance: dist1, SatelliteName: "SAT_1"),
                        });
        }
    }
}