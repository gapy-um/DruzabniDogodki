using DruzabniDogodki.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DruzabniDogodki.Tests.Helpers
{
    [TestClass]
    public class SloveniaLocationsTests
    {
        [TestMethod]
        public void Test_All_IsNotNull()
        {
            Assert.IsNotNull(SloveniaLocations.All);
        }

        [TestMethod]
        public void Test_All_ContainsLjubljana()
        {
            Assert.IsTrue(SloveniaLocations.All.Contains("Ljubljana"));
        }

        [TestMethod]
        public void Test_All_HasNoDuplicates()
        {
            var distinctCount = SloveniaLocations.All.Distinct().Count();
            Assert.AreEqual(distinctCount, SloveniaLocations.All.Count);
        }
    }
}

/*
Katere teste imamo in kaj preverjajo
1️⃣ SloveniaLocations testi

Preverjajo:

da seznam krajev ni prazen

da vsebuje Ljubljano

da ni podvojenih krajev

👉 s tem dokazujemo, da je seznam lokacij pravilen

2️⃣ EventInput testi

Preverjajo:

da ima nov dogodek privzete vrednosti

da lahko nastavimo naslov, opis, datum in lokacijo

da so podatki shranjeni pravilno

👉 s tem dokazujemo, da model dogodka deluje

3️⃣ CommentItem testi

Preverjajo:

da se komentar pravilno ustvari

da se pravilno nastavi uporabnik in vsebina

da se pravilno nastavi datum komentarja

👉 s tem dokazujemo, da komentarji delujejo
*/