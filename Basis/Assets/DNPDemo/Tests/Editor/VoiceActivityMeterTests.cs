using DNPDemo.Interaction.Voice;
using NUnit.Framework;

namespace DNPDemo.Tests
{
    public class VoiceActivityMeterTests
    {
        const float Threshold = 0.01f;

        [Test]
        public void CountsOnlyTimeAboveTheThreshold()
        {
            var meter = new VoiceActivityMeter();
            meter.Feed(0.02f, Threshold, 0.5f);
            meter.Feed(0.005f, Threshold, 0.5f);
            meter.Feed(Threshold, Threshold, 0.5f);

            Assert.AreEqual(0.5f, meter.VoicedSeconds, 1e-5f, "A level at the threshold is not sound.");
        }

        [Test]
        public void PausesBetweenWordsDoNotStartOver()
        {
            var meter = new VoiceActivityMeter();
            meter.Feed(0.05f, Threshold, 0.4f);
            meter.Feed(0f, Threshold, 0.3f);
            meter.Feed(0.05f, Threshold, 0.4f);

            Assert.AreEqual(0.8f, meter.VoicedSeconds, 1e-5f);
        }

        [Test]
        public void ThresholdIsReadPerFrame()
        {
            var meter = new VoiceActivityMeter();
            meter.Feed(0.02f, 0.03f, 0.5f);
            meter.Feed(0.02f, 0.01f, 0.5f);

            Assert.AreEqual(0.5f, meter.VoicedSeconds, 1e-5f);
        }

        [Test]
        public void ResetClearsTimeAndPeak()
        {
            var meter = new VoiceActivityMeter();
            meter.Feed(0.2f, Threshold, 1f);
            Assert.AreEqual(0.2f, meter.PeakLevel, 1e-6f);

            meter.Reset();

            Assert.AreEqual(0f, meter.VoicedSeconds);
            Assert.AreEqual(0f, meter.PeakLevel);
        }

        [Test]
        public void IgnoresInvalidFrames()
        {
            var meter = new VoiceActivityMeter();
            meter.Feed(float.NaN, Threshold, 1f);
            meter.Feed(0.5f, Threshold, 0f);
            meter.Feed(0.5f, Threshold, -1f);

            Assert.AreEqual(0f, meter.VoicedSeconds);
            Assert.AreEqual(0f, meter.PeakLevel);
        }

        [Test]
        public void DecibelsRoundTrip()
        {
            Assert.AreEqual(0f, AudioLevel.ToDecibels(1f), 1e-4f);
            Assert.AreEqual(-40f, AudioLevel.ToDecibels(0.01f), 1e-3f);
            Assert.AreEqual(0.01f, AudioLevel.FromDecibels(-40f), 1e-6f);
            Assert.AreEqual(AudioLevel.SilenceDecibels, AudioLevel.ToDecibels(0f));
            Assert.AreEqual(AudioLevel.SilenceDecibels, AudioLevel.ToDecibels(1e-9f));
        }

        [Test]
        public void ThresholdIsKeptInItsRange()
        {
            Assert.AreEqual(VoiceCommandSettings.MinThresholdDecibels, VoiceCommandSettings.Clamp(-200f));
            Assert.AreEqual(VoiceCommandSettings.MaxThresholdDecibels, VoiceCommandSettings.Clamp(10f));
            Assert.AreEqual(VoiceCommandSettings.DefaultThresholdDecibels, VoiceCommandSettings.Clamp(float.NaN));
            Assert.AreEqual(-35f, VoiceCommandSettings.Clamp(-35f));
        }
    }
}
