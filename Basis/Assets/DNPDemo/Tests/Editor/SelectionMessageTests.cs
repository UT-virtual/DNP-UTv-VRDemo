using System;
using System.Collections.Generic;
using DNPDemo.Interaction.Selection;
using NUnit.Framework;
using UnityEngine;

namespace DNPDemo.Tests
{
    public class SelectionMessageTests
    {
        static List<SelectionSample> SampleSelection()
        {
            var samples = new List<SelectionSample>();
            for (int i = 0; i < 20; i++)
            {
                samples.Add(new SelectionSample(new Vector3(-0.3f + i * 0.0137f, 1.6f, 2.25f - i * 0.003f), 0.025f + i * 0.0123f));
            }
            return samples;
        }

        [Test]
        public void RoundTripsWithinQuantization()
        {
            List<SelectionSample> samples = SampleSelection();
            byte[] buffer = SelectionMessage.Serialize(samples);
            Assert.AreEqual(SelectionMessage.HeaderSize + samples.Count * SelectionMessage.BytesPerSample, buffer.Length);

            var parsed = new List<SelectionSample>();
            Assert.IsTrue(SelectionMessage.TryParse(new ArraySegment<byte>(buffer), parsed));
            Assert.AreEqual(samples.Count, parsed.Count);
            for (int i = 0; i < samples.Count; i++)
            {
                Assert.LessOrEqual(Vector3.Distance(samples[i].Position, parsed[i].Position), SelectionMessage.PositionStep, $"position {i}");
                Assert.AreEqual(samples[i].Radius, parsed[i].Radius, SelectionMessage.RadiusStep, $"radius {i}");
            }
        }

        [Test]
        public void ParsesFromAnOffsetSegment()
        {
            byte[] payload = SelectionMessage.Serialize(SampleSelection());
            var wrapped = new byte[payload.Length + 6];
            Buffer.BlockCopy(payload, 0, wrapped, 6, payload.Length);

            var parsed = new List<SelectionSample>();
            Assert.IsTrue(SelectionMessage.TryParse(new ArraySegment<byte>(wrapped, 6, payload.Length), parsed));
            Assert.AreEqual(20, parsed.Count);
        }

        [Test]
        public void MaxSelectionFitsInAFewKilobytes()
        {
            var samples = new List<SelectionSample>();
            for (int i = 0; i < SelectionSampler.DefaultMaxSamples; i++)
            {
                samples.Add(new SelectionSample(new Vector3(i * 0.01f, 0f, 0f), 0.05f));
            }
            Assert.LessOrEqual(SelectionMessage.Serialize(samples).Length, 4200);
        }

        [Test]
        public void EmptySelectionRoundTrips()
        {
            var parsed = new List<SelectionSample> { new(Vector3.one, 1f) };
            Assert.IsTrue(SelectionMessage.TryParse(new ArraySegment<byte>(SelectionMessage.Serialize(new List<SelectionSample>())), parsed));
            Assert.AreEqual(0, parsed.Count);
        }

        [Test]
        public void RejectsMalformedPayloads()
        {
            byte[] valid = SelectionMessage.Serialize(SampleSelection());
            var parsed = new List<SelectionSample>();

            Assert.IsFalse(SelectionMessage.TryParse(new ArraySegment<byte>(valid, 0, valid.Length - 1), parsed), "truncated");
            Assert.IsFalse(SelectionMessage.TryParse(new ArraySegment<byte>(new byte[3]), parsed), "shorter than the header");

            var wrongVersion = (byte[])valid.Clone();
            wrongVersion[0] = 99;
            Assert.IsFalse(SelectionMessage.TryParse(new ArraySegment<byte>(wrongVersion), parsed), "unknown version");

            var nanOrigin = (byte[])valid.Clone();
            byte[] nan = BitConverter.GetBytes(float.NaN);
            Buffer.BlockCopy(nan, 0, nanOrigin, 3, 4);
            Assert.IsFalse(SelectionMessage.TryParse(new ArraySegment<byte>(nanOrigin), parsed), "NaN origin");
            Assert.AreEqual(0, parsed.Count);
        }
    }
}
