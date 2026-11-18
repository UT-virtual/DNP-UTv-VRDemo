using DNPDemo.Interaction.Voice;
using NUnit.Framework;

namespace DNPDemo.Tests
{
    public class MicrophoneSelectionTests
    {
        static readonly string[] Devices = { "マイク (Virtual Desktop Audio)", "マイク (USB Headset)", "Microphone (Camo)" };

        [Test]
        public void NextIsTheFollowingDeviceAndWrapsAround()
        {
            Assert.AreEqual("マイク (USB Headset)", MicrophoneSelection.Next(Devices, "マイク (Virtual Desktop Audio)"));
            Assert.AreEqual("マイク (Virtual Desktop Audio)", MicrophoneSelection.Next(Devices, "Microphone (Camo)"));
        }

        [Test]
        public void NextIsTheFirstDeviceWhenTheCurrentOneIsGone()
        {
            Assert.AreEqual("マイク (Virtual Desktop Audio)", MicrophoneSelection.Next(Devices, "unplugged"));
            Assert.AreEqual("マイク (Virtual Desktop Audio)", MicrophoneSelection.Next(Devices, null));
        }

        [Test]
        public void NextIsNullWithoutDevices()
        {
            Assert.IsNull(MicrophoneSelection.Next(new string[0], "x"));
            Assert.IsNull(MicrophoneSelection.Next(null, "x"));
        }
    }
}
