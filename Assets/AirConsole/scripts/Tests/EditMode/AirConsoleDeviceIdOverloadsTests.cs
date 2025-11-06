#if !DISABLE_AIRCONSOLE
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NDream.AirConsole.EditMode.Tests {
    public class AirConsoleDeviceIdOverloadsTests {
        private AirConsoleTestRunner target;

        [TearDown]
        public void TearDown() {
            if (target != null) {
                UnityEngine.Object.DestroyImmediate(target.gameObject);
            }
        }

        private void SetupTarget() {
            target = new GameObject("Target").AddComponent<AirConsoleTestRunner>();
            target.Initialize();
        }

        #region RequestHighScores Tests

        [UnityTest]
        public IEnumerator RequestHighScores_WithEmptyDeviceIdsList_ThrowsArgumentException() {
            SetupTarget();
            List<int> deviceIds = new List<int>();

            Assert.Throws<ArgumentException>(() => 
                target.RequestHighScores("level", "1.0", deviceIds));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RequestHighScores_WithDeviceIdZero_ThrowsArgumentOutOfRangeException() {
            SetupTarget();
            List<int> deviceIds = new List<int> { 0 };

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                target.RequestHighScores("level", "1.0", deviceIds));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RequestHighScores_WithNegativeDeviceId_ThrowsArgumentOutOfRangeException() {
            SetupTarget();
            List<int> deviceIds = new List<int> { -1 };

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                target.RequestHighScores("level", "1.0", deviceIds));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RequestHighScores_WithDisconnectedDeviceId_ThrowsInvalidOperationException() {
            SetupTarget();
            List<int> deviceIds = new List<int> { 1 };

            Assert.Throws<InvalidOperationException>(() => 
                target.RequestHighScores("level", "1.0", deviceIds));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RequestHighScores_WithNullDeviceIds_DoesNotThrow() {
            SetupTarget();
            List<int> deviceIds = null;

            Assert.DoesNotThrow(() => 
                target.RequestHighScores("level", "1.0", deviceIds));

            yield return null;
        }

        #endregion

        #region StoreHighScore (Single Player) Tests

        [UnityTest]
        public IEnumerator StoreHighScore_SinglePlayer_WithDeviceIdZero_ThrowsArgumentOutOfRangeException() {
            SetupTarget();

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                target.StoreHighScore("level", "1.0", 100f, 0));

            yield return null;
        }

        [UnityTest]
        public IEnumerator StoreHighScore_SinglePlayer_WithNegativeDeviceId_ThrowsArgumentOutOfRangeException() {
            SetupTarget();

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                target.StoreHighScore("level", "1.0", 100f, -1));

            yield return null;
        }

        [UnityTest]
        public IEnumerator StoreHighScore_SinglePlayer_WithDisconnectedDeviceId_ThrowsInvalidOperationException() {
            SetupTarget();

            Assert.Throws<InvalidOperationException>(() => 
                target.StoreHighScore("level", "1.0", 100f, 1));

            yield return null;
        }

        #endregion

        #region StoreHighScore (Multi-Player) Tests

        [UnityTest]
        public IEnumerator StoreHighScore_MultiPlayer_WithNullDeviceIds_ThrowsArgumentNullException() {
            SetupTarget();
            List<int> deviceIds = null;

            Assert.Throws<ArgumentNullException>(() => 
                target.StoreHighScore("level", "1.0", 100f, deviceIds));

            yield return null;
        }

        [UnityTest]
        public IEnumerator StoreHighScore_MultiPlayer_WithEmptyDeviceIdsList_ThrowsArgumentException() {
            SetupTarget();
            List<int> deviceIds = new List<int>();

            Assert.Throws<ArgumentException>(() => 
                target.StoreHighScore("level", "1.0", 100f, deviceIds));

            yield return null;
        }

        [UnityTest]
        public IEnumerator StoreHighScore_MultiPlayer_WithDeviceIdZero_ThrowsArgumentOutOfRangeException() {
            SetupTarget();
            List<int> deviceIds = new List<int> { 0 };

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                target.StoreHighScore("level", "1.0", 100f, deviceIds));

            yield return null;
        }

        [UnityTest]
        public IEnumerator StoreHighScore_MultiPlayer_WithNegativeDeviceId_ThrowsArgumentOutOfRangeException() {
            SetupTarget();
            List<int> deviceIds = new List<int> { -1 };

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                target.StoreHighScore("level", "1.0", 100f, deviceIds));

            yield return null;
        }

        [UnityTest]
        public IEnumerator StoreHighScore_MultiPlayer_WithDisconnectedDeviceId_ThrowsInvalidOperationException() {
            SetupTarget();
            List<int> deviceIds = new List<int> { 1 };

            Assert.Throws<InvalidOperationException>(() => 
                target.StoreHighScore("level", "1.0", 100f, deviceIds));

            yield return null;
        }

        #endregion

        #region RequestPersistentData Tests

        [UnityTest]
        public IEnumerator RequestPersistentData_WithNullDeviceIds_ThrowsArgumentNullException() {
            SetupTarget();
            List<int> deviceIds = null;

            Assert.Throws<ArgumentNullException>(() => 
                target.RequestPersistentData(deviceIds));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RequestPersistentData_WithEmptyDeviceIdsList_ThrowsArgumentException() {
            SetupTarget();
            List<int> deviceIds = new List<int>();

            Assert.Throws<ArgumentException>(() => 
                target.RequestPersistentData(deviceIds));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RequestPersistentData_WithDeviceIdZero_ThrowsArgumentOutOfRangeException() {
            SetupTarget();
            List<int> deviceIds = new List<int> { 0 };

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                target.RequestPersistentData(deviceIds));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RequestPersistentData_WithNegativeDeviceId_ThrowsArgumentOutOfRangeException() {
            SetupTarget();
            List<int> deviceIds = new List<int> { -1 };

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                target.RequestPersistentData(deviceIds));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RequestPersistentData_WithDisconnectedDeviceId_ThrowsInvalidOperationException() {
            SetupTarget();
            List<int> deviceIds = new List<int> { 1 };

            Assert.Throws<InvalidOperationException>(() => 
                target.RequestPersistentData(deviceIds));

            yield return null;
        }

        #endregion

        #region StorePersistentData Tests

        [UnityTest]
        public IEnumerator StorePersistentData_WithDeviceIdZero_ThrowsArgumentOutOfRangeException() {
            SetupTarget();

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                target.StorePersistentData("key", JValue.CreateString("value"), 0));

            yield return null;
        }

        [UnityTest]
        public IEnumerator StorePersistentData_WithNegativeDeviceId_ThrowsArgumentOutOfRangeException() {
            SetupTarget();

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                target.StorePersistentData("key", JValue.CreateString("value"), -1));

            yield return null;
        }

        [UnityTest]
        public IEnumerator StorePersistentData_WithDisconnectedDeviceId_ThrowsInvalidOperationException() {
            SetupTarget();

            Assert.Throws<InvalidOperationException>(() => 
                target.StorePersistentData("key", JValue.CreateString("value"), 1));

            yield return null;
        }

        #endregion

        public class AirConsoleTestRunner : AirConsole {
            internal void Initialize() {
                androidGameVersion = "1";
                Awake();
                Start();
            }
        }
    }
}
#endif
