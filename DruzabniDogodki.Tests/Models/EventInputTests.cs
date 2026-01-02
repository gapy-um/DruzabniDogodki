using System;
using DruzabniDogodki.Pages.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DruzabniDogodki.Tests.Models
{
    [TestClass]
    public class EventInputTests
    {
        [TestMethod]
        public void Test_DefaultTitle_IsEmptyString()
        {
            var e = new CreateModel.EventInput();
            Assert.AreEqual("", e.Title);
        }

        [TestMethod]
        public void Test_DefaultEventDate_IsNotMinValue()
        {
            var e = new CreateModel.EventInput();
            Assert.IsTrue(e.EventDate > DateTime.MinValue);
        }

        [TestMethod]
        public void Test_CanSetBasicProperties()
        {
            var dt = new DateTime(2026, 1, 2, 12, 0, 0);

            var e = new CreateModel.EventInput
            {
                Title = "Koncert",
                Description = "Opis",
                EventDate = dt,
                Location = "Ljubljana"
            };

            Assert.AreEqual("Koncert", e.Title);
            Assert.AreEqual("Opis", e.Description);
            Assert.AreEqual(dt, e.EventDate);
            Assert.AreEqual("Ljubljana", e.Location);
        }
    }
}
