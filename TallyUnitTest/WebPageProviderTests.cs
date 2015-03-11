﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.Tests
{
    [TestClass()]
    public class WebPageProviderTests
    {
        static WebPageProvider pageProvider;
        static PrivateObject privateWeb;
        static IForumData forumData;


        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            forumData = new SVForumData();
            pageProvider = new WebPageProvider(forumData);
            privateWeb = new PrivateObject(pageProvider);
        }


        [TestMethod()]
        public void ClearPageCacheTest()
        {
            pageProvider.ClearPageCache();

            Dictionary<string, CachedPage> accessPageCache = (Dictionary<string, CachedPage>)privateWeb.GetField("pageCache");
            Assert.IsTrue(accessPageCache.Count == 0);

            Dictionary<string, int> accessLoadedPages = (Dictionary<string, int>)privateWeb.GetField("lastPageLoadedFor");
            Assert.IsTrue(accessLoadedPages.Count == 0);
        }

        [TestMethod()]
        public void CheckForLastThreadmarkTest()
        {
            pageProvider.CheckForLastThreadmark = true;
            Assert.AreEqual(true, pageProvider.CheckForLastThreadmark);
            pageProvider.CheckForLastThreadmark = false;
            Assert.AreEqual(false, pageProvider.CheckForLastThreadmark);
        }

        [TestMethod()]
        public void LoadPagesTest()
        {
        }


    }
}