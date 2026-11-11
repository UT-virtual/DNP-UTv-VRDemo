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
    }
}
