using System;
using System.Collections.Generic;
using System.Text;
using Com.AugustCellars.CoAP.Coral;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoAP.Test.Std10.CoRAL
{
    [TestClass]
    public class TestCoralUsing
    {
        static CoralUsing testDictionary = new CoralUsing() {
            {"reef", "http://example.org/reef#" },
            {"", "http://example.org/null#" }
        }; 

        [TestMethod]
        public void AbbreviateTests()
        {
            string s = testDictionary.Abbreviate("http://s1.org/s2/s3");
            Assert.AreEqual("<http://s1.org/s2/s3>", s);

            s = testDictionary.Abbreviate("http://example.org/reef#left");
            Assert.AreEqual("reef:left", s);

            s = testDictionary.Abbreviate("http://example.org/null#right");
            Assert.AreEqual("right", s);

            s = testDictionary.Abbreviate("http://exmple.org/null#lef right");
            Assert.AreEqual("<http://exmple.org/null#lef right>", s);
        }

        [TestMethod]
        public void AddItems()
        {
            CoralUsing test = new CoralUsing();

            test.Add("", "http://example.org/ABC");

            Assert.ThrowsException<ArgumentException>(
                () => test.Add("", "http://example.org/DEFG")
            );

            Assert.ThrowsException<ArgumentException>(() => test.Add("key1", "http://example.org/ABC"));

            test.Add("key2", "http://exmple.org/ABC/DEF#");

            Assert.ThrowsException<ArgumentException>(() => test.Add("key2", "http://example.org/ABCD#"));

            Assert.ThrowsException<ArgumentException>(()=> test.Add("key3", "http://exmple.org/ABC/DEF#"));
        }
    }
}
