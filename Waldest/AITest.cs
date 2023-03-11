using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Waldolaw;
using Xunit;

namespace Waldest
{
    public class AITest
    {
        [Fact]
        public void direction_costs()
        {
            Direction.Top.CostTo(Direction.Top).Should().Be(0);
            Direction.Top.CostTo(Direction.Right).Should().Be(1);
            Direction.Top.CostTo(Direction.Left).Should().Be(1);
            Direction.Top.CostTo(Direction.Bottom).Should().Be(2);
            Direction.Top.CostTo(Direction.None).Should().Be(0);

            Direction.Bottom.CostTo(Direction.Top).Should().Be(2);
            Direction.Bottom.CostTo(Direction.Right).Should().Be(1);
            Direction.Bottom.CostTo(Direction.Left).Should().Be(1);
            Direction.Bottom.CostTo(Direction.Bottom).Should().Be(0);
            Direction.Bottom.CostTo(Direction.None).Should().Be(0);

            Direction.Right.CostTo(Direction.Top).Should().Be(1);
            Direction.Right.CostTo(Direction.Right).Should().Be(0);
            Direction.Right.CostTo(Direction.Left).Should().Be(2);
            Direction.Right.CostTo(Direction.Bottom).Should().Be(1);
            Direction.Right.CostTo(Direction.None).Should().Be(0);

            Direction.Left.CostTo(Direction.Top).Should().Be(1);
            Direction.Left.CostTo(Direction.Right).Should().Be(2);
            Direction.Left.CostTo(Direction.Left).Should().Be(0);
            Direction.Left.CostTo(Direction.Bottom).Should().Be(1);
            Direction.Left.CostTo(Direction.None).Should().Be(0);

            Direction.None.CostTo(Direction.Top).Should().Be(0);
            Direction.None.CostTo(Direction.Right).Should().Be(0);
            Direction.None.CostTo(Direction.Bottom).Should().Be(0);
            Direction.None.CostTo(Direction.Left).Should().Be(0);
            Direction.None.CostTo(Direction.None).Should().Be(0);
        }

        [Fact]
        public void turn_direction()
        {
            Direction.Left.GetDirectionToTurn(Direction.Top).Should().Be(Direction.Right);
            Direction.Left.GetDirectionToTurn(Direction.Left).Should().Be(Direction.None);
            Direction.Left.GetDirectionToTurn(Direction.Right).Should().Be(Direction.Left);
            Direction.Left.GetDirectionToTurn(Direction.Bottom).Should().Be(Direction.Left);

            Direction.Right.GetDirectionToTurn(Direction.Top).Should().Be(Direction.Left);
            Direction.Right.GetDirectionToTurn(Direction.Left).Should().Be(Direction.Left);
            Direction.Right.GetDirectionToTurn(Direction.Right).Should().Be(Direction.None);
            Direction.Right.GetDirectionToTurn(Direction.Bottom).Should().Be(Direction.Right);

            Direction.Top.GetDirectionToTurn(Direction.Top).Should().Be(Direction.None);
            Direction.Top.GetDirectionToTurn(Direction.Left).Should().Be(Direction.Left);
            Direction.Top.GetDirectionToTurn(Direction.Right).Should().Be(Direction.Right);
            Direction.Top.GetDirectionToTurn(Direction.Bottom).Should().Be(Direction.Left);

            Direction.Bottom.GetDirectionToTurn(Direction.Top).Should().Be(Direction.Left);
            Direction.Bottom.GetDirectionToTurn(Direction.Left).Should().Be(Direction.Right);
            Direction.Bottom.GetDirectionToTurn(Direction.Right).Should().Be(Direction.Left);
            Direction.Bottom.GetDirectionToTurn(Direction.Bottom).Should().Be(Direction.None);
        }
    }
}
