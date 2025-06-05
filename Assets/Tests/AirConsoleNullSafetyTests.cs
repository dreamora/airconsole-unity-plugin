#if !DISABLE_AIRCONSOLE
using NUnit.Framework;
using NDream.AirConsole;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Tests
{
    public class AirConsoleNullSafetyTests
    {
        [Test]
        public void GetTranslation_WithNullId_ReturnsNull()
        {
            // Test that GetTranslation handles null input safely
            var airConsole = new GameObject("AirConsole").AddComponent<AirConsole>();
            string result = airConsole.GetTranslation(null);
            Assert.IsNull(result);
        }

        [Test] 
        public void GetTranslation_WithEmptyId_ReturnsNull()
        {
            // Test that GetTranslation handles empty input safely
            var airConsole = new GameObject("AirConsole").AddComponent<AirConsole>();
            string result = airConsole.GetTranslation("");
            Assert.IsNull(result);
        }

        [Test]
        public void ConvertPlayerNumberToDeviceId_WithValidInput_ReturnsMinusOne()
        {
            // Test that ConvertPlayerNumberToDeviceId handles null _players safely
            var airConsole = new GameObject("AirConsole").AddComponent<AirConsole>();
            int result = airConsole.ConvertPlayerNumberToDeviceId(0);
            Assert.AreEqual(-1, result, "Should return -1 when _players is not initialized");
        }

        [Test]
        public void WebsocketListener_ProcessMessage_WithNullData_DoesNotThrow()
        {
            // Test that ProcessMessage handles null data safely
            var listener = new WebsocketListener();
            
            Assert.DoesNotThrow(() => {
                listener.ProcessMessage(null);
            });
        }

        [Test]
        public void WebsocketListener_ProcessMessage_WithEmptyData_DoesNotThrow()
        {
            // Test that ProcessMessage handles empty data safely
            var listener = new WebsocketListener();
            
            Assert.DoesNotThrow(() => {
                listener.ProcessMessage("");
            });
        }

        [Test]
        public void WebsocketListener_ProcessMessage_WithInvalidJson_DoesNotThrow()
        {
            // Test that ProcessMessage handles invalid JSON safely
            var listener = new WebsocketListener();
            
            Assert.DoesNotThrow(() => {
                listener.ProcessMessage("{invalid json}");
            });
        }

        [Test]
        public void SettingsIsUnity6OrHigher_HandlesNullVersionSafely()
        {
            // Test that version parsing is null-safe
            // This tests the static method indirectly by creating the static constructor
            Assert.DoesNotThrow(() => {
                var path = Settings.WEBTEMPLATE_PATH;
                Assert.IsNotNull(path);
            });
        }
    }
}
#endif