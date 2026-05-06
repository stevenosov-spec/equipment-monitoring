using Xunit;
using System.Threading;
using EquipmentMonitoring.Integration.Simulation;

namespace EquipmentMonitoring.Tests.Integration
{
    public class SimulatedTagReaderTests
    {
        [Fact]
        public void Simulator_GeneratesEventsOnStart()
        {
            var reader = new SimulatedTagReader();
            int received = 0;
            reader.TagValueChanged += (s, e) => received++;
            reader.Start();
            Thread.Sleep(1500);
            reader.Stop();
            Assert.True(received > 0);
        }

        [Fact]
        public void Simulator_StopPreventsFurtherEvents()
        {
            var reader = new SimulatedTagReader();
            int count = 0;
            reader.TagValueChanged += (s, e) => count++;
            reader.Start();
            Thread.Sleep(500);
            reader.Stop();
            int afterStop = count;
            Thread.Sleep(1500);
            Assert.Equal(afterStop, count);
        }
    }
}