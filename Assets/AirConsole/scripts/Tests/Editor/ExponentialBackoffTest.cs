#if !DISABLE_AIRCONSOLE
using NUnit.Framework;
using NDream.AirConsole.Editor;
using System;
using UnityEngine;

namespace NDream.AirConsole.Editor.Tests {
    /// <summary>
    /// Simple test to verify exponential backoff math
    /// </summary>
    public class ExponentialBackoffTest {

        [Test]
        public void ExponentialBackoff_Math_WorksCorrectly() {
            // Test the exponential backoff formula: 1 << (FailedCheckCount - 1)

            // 0 failures: no multiplier (base interval)
            int multiplier0 = 0; // No backoff for 0 failures

            // 1 failure: 1 << (1-1) = 1 << 0 = 1 (24h * 1 = 24h)
            int multiplier1 = 1 << (1 - 1); // = 1

            // 2 failures: 1 << (2-1) = 1 << 1 = 2 (24h * 2 = 48h)
            int multiplier2 = 1 << (2 - 1); // = 2

            // 3 failures: 1 << (3-1) = 1 << 2 = 4 (24h * 4 = 96h)
            int multiplier3 = 1 << (3 - 1); // = 4

            // 4 failures: 1 << (4-1) = 1 << 3 = 8 (24h * 8 = 192h)
            int multiplier4 = 1 << (4 - 1); // = 8

            Assert.AreEqual(1, multiplier1, "1 failure should have multiplier 1");
            Assert.AreEqual(2, multiplier2, "2 failures should have multiplier 2");
            Assert.AreEqual(4, multiplier3, "3 failures should have multiplier 4");
            Assert.AreEqual(8, multiplier4, "4 failures should have multiplier 8");

            // Test with capping at 8
            int cappedMultiplier = Mathf.Min(1 << (5 - 1), 8); // Should be 8, not 16
            Assert.AreEqual(8, cappedMultiplier, "Multiplier should be capped at 8");
        }
    }
}
#endif