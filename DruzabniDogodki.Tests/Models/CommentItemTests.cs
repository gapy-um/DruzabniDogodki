using System;
using DruzabniDogodki.Pages.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DruzabniDogodki.Tests.Models
{
    [TestClass]
    public class CommentItemTests
    {
        [TestMethod]
        public void Test_DefaultUserName_IsEmptyString()
        {
            var c = new DetailsModel.CommentItem();
            Assert.AreEqual("", c.UserName);
        }

        [TestMethod]
        public void Test_DefaultContent_IsEmptyString()
        {
            var c = new DetailsModel.CommentItem();
            Assert.AreEqual("", c.Content);
        }

        [TestMethod]
        public void Test_CanSetBasicProperties()
        {
            var dt = new DateTime(2026, 1, 2, 10, 0, 0);

            var c = new DetailsModel.CommentItem
            {
                Id = 1,
                UserId = 5,
                UserName = "tim",
                Content = "Super dogodek!",
                CreatedAt = dt,
                CanDelete = true
            };

            Assert.AreEqual(1, c.Id);
            Assert.AreEqual(5, c.UserId);
            Assert.AreEqual("tim", c.UserName);
            Assert.AreEqual("Super dogodek!", c.Content);
            Assert.AreEqual(dt, c.CreatedAt);
            Assert.IsTrue(c.CanDelete);
        }
    }
}
