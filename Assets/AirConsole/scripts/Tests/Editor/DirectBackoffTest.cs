#if !DISABLE_AIRCONSOLE
using NUnit.Framework;
using NDream.AirConsole.Editor;
using System;
using UnityEngine;
using System.Reflection;

namespace NDream.AirConsole.Editor.Tests {
    /// <summary>
    /// Direct test of the exponential backoff logic using reflection
    /// </summary>
    public class DirectBackoffTest {
        private UpdateSettings _testSettings;

        [SetUp]
        public void SetUp() {
            _testSettings = ScriptableObject.CreateInstance<UpdateSettings>();
        }

        [TearDown]
        public void TearDown() {
            if (_testSettings != null) {
                UnityEngine.Object.DestroyImmediate(_testSettings);
            }
        }

        [Test]
        public void GetEffectiveCheckInterval_DirectTest() {
            // Use reflection to access the private GetEffectiveCheckInterval method
            var method = typeof(UpdateSettings).GetMethod("GetEffectiveCheckInterval",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(method, "GetEffectiveCheckInterval method should exist");

            // Test 0 failures - should return base interval (24)
            _testSettings.FailedCheckCount = 0;
            int interval0 = (int)method.Invoke(_testSettings, null);
            Assert.AreEqual(24, interval0, "0 failures should return base interval");

            // Test 1 failure - multiplier 1 (24 * 1 = 24)
            _testSettings.FailedCheckCount = 1;
            int interval1 = (int)method.Invoke(_testSettings, null);
            Assert.AreEqual(24, interval1, "1 failure should return 24h (multiplier 1)");

            // Test 2 failures - multiplier 2 (24 * 2 = 48)
            _testSettings.FailedCheckCount = 2;
            int interval2 = (int)method.Invoke(_testSettings, null);
            Assert.AreEqual(48, interval2, "2 failures should return 48h (multiplier 2)");

            // Test 3 failures - multiplier 4 (24 * 4 = 96)
            _testSettings.FailedCheckCount = 3;
            int interval3 = (int)method.Invoke(_testSettings, null);
            Assert.AreEqual(96, interval3, "3 failures should return 96h (multiplier 4)");

            // Test 4 failures - multiplier 8 (24 * 8 = 192)
            _testSettings.FailedCheckCount = 4;
            int interval4 = (int)method.Invoke(_testSettings, null);
            Assert.AreEqual(192, interval4, "4 failures should return 192h (multiplier 8)");

            // Test 5 failures - multiplier capped at 8 (24 * 8 = 192)
            _testSettings.FailedCheckCount = 5;
            int interval5 = (int)method.Invoke(_testSettings, null);
            Assert.AreEqual(192, interval5, "5 failures should be capped at 192h (multiplier 8)");
        }
    }
}
#endif