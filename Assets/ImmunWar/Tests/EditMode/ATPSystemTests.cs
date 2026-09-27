using NUnit.Framework;
using UnityEngine;
using ImmunWar.Battle.Placement;
using ImmunWar.Core.Config;

namespace ImmunWar.Tests
{
    public class ATPSystemTests
    {
        [Test]
        public void Property11_DensityLimitationEnforcement_LimitsEnergyCells()
        {
            var config = GridConfiguration.CreateDefault();
            var gridSystem = new GridSystem(config);
            var validationSystem = new ValidationSystem(gridSystem, config);

            // Place an Energy Cell at (5,5)
            var centerPos = new GridPosition(5, 5);
            gridSystem.TryOccupyPosition(centerPos, "EnergyCell_1");

            // Attempt to place another Energy Cell within the 3x3 area, e.g. at (4,4)
            var adjacentPos = new GridPosition(4, 4);
            bool isAllowed = validationSystem.ValidateDensityLimits("EnergyCell_2", adjacentPos);

            // Assert that placement is rejected due to density limits
            Assert.IsFalse(isAllowed, "Density limitation failed to prevent multiple Energy Cells in a 3x3 area.");

            // Attempt to place a non-Energy Cell, should be allowed
            bool isTCellAllowed = validationSystem.ValidateDensityLimits("TCell_1", adjacentPos);
            Assert.IsTrue(isTCellAllowed, "Density limitation incorrectly blocked a non-Energy Cell defender.");
        }
    }
}
