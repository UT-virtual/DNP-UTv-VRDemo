using System;
using System.Collections.Generic;
using Basis.BasisUI;
using DNPDemo.BasisPolicy;
using DNPDemo.Menu;
using NUnit.Framework;

namespace DNPDemo.Tests
{
    public class DemoBasisPolicyTests
    {
        [Test]
        public void SuppressesBasisMenuEntries()
        {
            List<Type> suppressed = DemoMenuPolicy.FindSuppressedTypes(AppDomain.CurrentDomain.GetAssemblies());
            CollectionAssert.Contains(suppressed, typeof(SettingsProvider));
            CollectionAssert.Contains(suppressed, typeof(MuteProvider));
            CollectionAssert.Contains(suppressed, typeof(RespawnProvider));
            CollectionAssert.Contains(suppressed, typeof(ExitProvider));
        }

        [Test]
        public void KeepsDemoMenuEntries()
        {
            List<Type> suppressed = DemoMenuPolicy.FindSuppressedTypes(AppDomain.CurrentDomain.GetAssemblies());
            foreach (DemoMenuProvider provider in DemoMenuPolicy.CreateDemoProviders())
            {
                CollectionAssert.DoesNotContain(suppressed, provider.GetType());
            }
        }

        [Test]
        public void IgnoresTypesThatCannotBeRegistered()
        {
            Assert.IsFalse(DemoMenuPolicy.ShouldSuppress(typeof(DemoMenuProvider)));
            Assert.IsFalse(DemoMenuPolicy.ShouldSuppress(typeof(BasisMenuActionProvider<BasisMainMenu>)));
            Assert.IsFalse(DemoMenuPolicy.ShouldSuppress(typeof(string)));
        }

        [Test]
        public void DemoMenuEntriesHaveDistinctTitlesAndOrders()
        {
            var titles = new HashSet<string>();
            var orders = new HashSet<int>();
            foreach (DemoMenuProvider provider in DemoMenuPolicy.CreateDemoProviders())
            {
                Assert.IsTrue(titles.Add(provider.Title), $"Duplicate title {provider.Title}");
                Assert.IsTrue(orders.Add(provider.Order), $"Duplicate order {provider.Order}");
            }
        }

        [Test]
        public void KeepsMenuButtonBound()
        {
            CollectionAssert.DoesNotContain(DemoInputPolicy.RemovedActions, BasisActionDriver.ActionId.ToggleHamburgerOnSecondaryRelease);
        }

#if !BASIS_DISABLE_MICROPHONE
        [Test]
        public void UnmuteIsIgnoredWhileLocked()
        {
            bool previousPaused = BasisLocalMicrophoneDriver.isPaused;
            bool previousLocked = BasisLocalMicrophoneDriver.IsUnmuteLocked;
            int pausedEvents = 0;
            void CountPausedEvents(bool paused) => pausedEvents++;
            BasisLocalMicrophoneDriver.OnPausedAction += CountPausedEvents;
            try
            {
                BasisLocalMicrophoneDriver.isPaused = true;
                BasisLocalMicrophoneDriver.SetUnmuteLocked(true);

                BasisLocalMicrophoneDriver.ToggleIsPaused();

                Assert.IsTrue(BasisLocalMicrophoneDriver.isPaused);
                Assert.AreEqual(0, pausedEvents, "Subscribers must not hear about an unmute that did not happen.");
            }
            finally
            {
                BasisLocalMicrophoneDriver.OnPausedAction -= CountPausedEvents;
                BasisLocalMicrophoneDriver.SetUnmuteLocked(previousLocked);
                BasisLocalMicrophoneDriver.isPaused = previousPaused;
            }
        }
#endif
    }
}
