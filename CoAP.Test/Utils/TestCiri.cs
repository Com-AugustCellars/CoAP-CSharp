using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Com.AugustCellars.CoAP.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PeterO.Cbor;

namespace CoAP.Test.Std10.Utils
{
    [TestClass]
    public class TestCiri
    {
        [TestMethod]
        [DataRow("coap:", "[1, \"coap\"]")]
        [DataRow("coap://hostName", "[1, \"coap\", 2, \"hostName\"]")]
        [DataRow("coap://server:99", "[1, \"coap\", 2, \"server\", 4, 99]")]
        [DataRow("coap://1.2.3.4", "[1, \"coap\", 3, h'01020304']")]
        [DataRow("coap://[1::4]", "[1, \"coap\", 3, h'00010000000000000000000000000004']")]
        [DataRow("coap://[1::2]:99", "[1, \"coap\", 3, h'00010000000000000000000000000002', 4, 99]")]
        [DataRow("//server", "[2, \"server\"]")]
        [DataRow("coap:/path", "[1, \"coap\", 5, 0, 6, \"path\"]")]
        [DataRow("coap:path", "[1, \"coap\", 6, \"path\"]")]
        [DataRow("coap://testServer:99/alpha/beta?abc&def#frag", "[1, \"coap\", 2, \"testServer\", 4, 99, 6, \"alpha\", 6, \"beta\", 7, \"abc\", 7, \"def\", 8, \"frag\"]")]
        [DataRow("mailto:john.doe@example.com", "[1, \"mailto\", 6, \"john.doe@example.com\"]")]
        [DataRow("/alpha/beta", "[5, 0, 6, \"alpha\", 6, \"beta\"]")]
        [DataRow("", "[]")]
        [DataRow(".", "[5, 3]")]
        [DataRow("./", "[5, 3, 6, \"\"]")]
        [DataRow("..", "[5, 4]")]
        [DataRow("../", "[5, 4, 6, \"\"]")]
        [DataRow("../g", "[5, 4, 6, \"g\"]")]
        [DataRow("../..", "[5, 5]")]
        [DataRow("../../", "[5, 5, 6, \"\"]")]
        [DataRow("../../g", "[5, 5, 6, \"g\"]")]

        public void Parse(string url, string cbor)
        {
            Ciri o = new Ciri(url);
            Assert.AreEqual(cbor, o.Data.ToString());
            string rebuild = o.ToString();
            Assert.AreEqual(url, rebuild);
        }


        [TestMethod]
        [DataRow("http://a/b/c/d?p&q", "g:h", "[1, \"g\", 6, \"h\"]")]
        [DataRow("http://a/b/c/d?p&q", "g", "[1, \"http\", 2, \"a\", 6, \"b\", 6, \"c\", 6, \"g\"]")]
        [DataRow("http://a/b/c/d?p&q", "./g", "[1, \"http\", 2, \"a\", 6, \"b\", 6, \"c\", 6, \"g\"]")]
        [DataRow("http://a/b/c/d?p&q", "g/", "[1, \"http\", 2, \"a\", 6, \"b\", 6, \"c\", 6, \"g\", 6, \"\"]")]
        [DataRow("http://a/b/c/d?p&q", "/g", "[1, \"http\", 2, \"a\", 6, \"g\"]")]
        [DataRow("http://a/b/c/d?p&q", "//g", "[1, \"http\", 2, \"g\"]")]
        [DataRow("http://a/b/c/d?p&q", "?y", "[1, \"http\", 2, \"a\", 6, \"b\", 6, \"c\", 6, \"d\", 7, \"y\"]")]
        [DataRow("http://a/b/c/d?p&q", "g?y", "[1, \"http\", 2, \"a\", 6, \"b\", 6, \"c\", 6, \"g\", 7, \"y\"]")]
        [DataRow("http://a/b/c/d?p&q", "#s", "[1, \"http\", 2, \"a\", 6, \"b\", 6, \"c\", 6, \"d\", 7, \"p\", 7, \"q\", 8, \"s\"]")]
        [DataRow("http://a/b/c/d?p&q", "g#s", "[1, \"http\", 2, \"a\", 6, \"b\", 6, \"c\", 6, \"g\", 8, \"s\"]")]
        [DataRow("http://a/b/c/d?p&q", "g?y#s", "[1, \"http\", 2, \"a\", 6, \"b\", 6, \"c\", 6, \"g\", 7, \"y\", 8, \"s\"]")]
        [DataRow("http://a/b/c/d?p&q", "", "[1, \"http\", 2, \"a\", 6, \"b\", 6, \"c\", 6, \"d\", 7, \"p\", 7, \"q\"]")]
        // M00DONTFIX - There is a difference between URL resolve and CIRI resolve where a trailing empty directory is not always added
        //       This is per Klaus.   Personally I find it objectionable
        [DataRow("http://a/b/c/d?p&q", ".", "[1, \"http\", 2, \"a\", 6, \"b\", 6, \"c\"]")]
        [DataRow("http://a/b/c/d?p&q", "./", "[1, \"http\", 2, \"a\", 6, \"b\", 6, \"c\", 6, \"\"]")]
        [DataRow("http://a/b/c/d?p&q", "..", "[1, \"http\", 2, \"a\", 6, \"b\"]")]
        [DataRow("http://a/b/c/d?p&q", "../", "[1, \"http\", 2, \"a\", 6, \"b\", 6, \"\"]")]
        [DataRow("http://a/b/c/d?p&q", "../g", "[1, \"http\", 2, \"a\", 6, \"b\", 6, \"g\"]")]
        [DataRow("http://a/b/c/d?p&q", "../..", "[1, \"http\", 2, \"a\"]")]
        [DataRow("http://a/b/c/d?p&q", "../../", "[1, \"http\", 2, \"a\"]")]
        [DataRow("http://a/b/c/d?p&q", "../../g", "[1, \"http\", 2, \"a\", 6, \"g\"]")]
        public void Resolve(string baseUri, string href, string cbor)
        {
            Ciri baseCiri = new Ciri(baseUri);
            Ciri hrefCiri = new Ciri(href);

            Console.WriteLine(baseCiri.Data.ToString());
            Console.WriteLine(hrefCiri.Data.ToString());

            Ciri resolved = hrefCiri.ResolveTo(baseCiri);
            Assert.AreEqual(cbor, resolved.Data.ToString());
        }

        [TestMethod]
        [DeploymentItem("CoRAL\\Klaus.csv")]
        [DynamicData(nameof(KlausData), DynamicDataSourceType.Method)]
        public void TestsFromKlaus(string line, string left, string middle, string right, string baseText, string relativeText, string resultText, string skip)
        {
            try {
                CBORObject cborLeft = CBORObject.DecodeFromBytes(HexToString(left));
                CBORObject cborRight = CBORObject.DecodeFromBytes(HexToString(right));
                CBORObject cborMiddle = CBORObject.DecodeFromBytes(HexToString(middle));

                Console.WriteLine($"line # = {line}");
                Console.WriteLine($"base = {cborLeft}");
                Console.WriteLine($"href = {cborMiddle}");
                Console.WriteLine($"result = {cborRight}");

                Ciri ciriBase = new Ciri(cborLeft);
                Ciri ciriRight = new Ciri(cborRight);
                Ciri ciriMiddle = new Ciri(cborMiddle);

                Assert.IsTrue(ciriBase.IsAbsolute());
                Assert.IsFalse(ciriBase.IsRelative());
                Assert.IsTrue(ciriRight.IsAbsolute());
                Assert.IsFalse(ciriRight.IsRelative());
                Assert.IsTrue(ciriMiddle.IsWellFormed());

                Ciri result = ciriMiddle.ResolveTo(ciriBase, 9000);
                Assert.AreEqual(ciriRight, result);

                if (cborMiddle.Count > 0 && cborMiddle[0].AsInt32() == 5 && cborMiddle[1].AsInt32() == 1) {
                    return;
                }

                Ciri newHref = ciriRight.MakeRelative(ciriBase);
                Console.WriteLine($"computed href = {newHref.Data}");

                Ciri resolve2 = newHref.ResolveTo(ciriBase);
                Assert.AreEqual(ciriRight.Data.ToString(), resolve2.Data.ToString());

                if (skip != "0") {
                   // Assert.AreEqual(ciriMiddle.Data.ToString(), newHref.Data.ToString());
                }

            }
            catch (Exception e) {
                Assert.Fail(e.ToString());
            }
        }

        private static IEnumerable<string[]> KlausData()
        {
            string fileName = "CoRAL\\Klaus.csv";
            if (!File.Exists(fileName)) {
                fileName = "Klaus.csv";
            }
            IEnumerable<string> rows = System.IO.File.ReadAllLines(fileName);
            bool f = true;
            foreach (string row in rows) {
                if (f) {
                    f = false;
                    continue;
                }
                yield return SplitCsv(row);
            }
        }

        private static string[] SplitCsv(string input)
        {
            // old = /*"(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)"*/
            var csvSplit = new Regex("(?:^|,) *(?=[^\"]|(\")?)\"?((?(1)[^\"]*|[^,\"]*))\"?(?=,|$)", RegexOptions.Compiled);
            var list = new List<string>();
            foreach (Match match in csvSplit.Matches(input))
            {
                string value = match.Groups[2].Value;
                if (value.Length == 0)
                {
                    list.Add(string.Empty);
                }

                list.Add(value.TrimStart(','));
            }
            return list.ToArray();
        }

#if false
        [TestMethod]
        [DataRow(2091, "8c0166736368656d6503446970763404192a2a066470617468066470617468066470617468", "8405040663666f6f", "8a0166736368656d6503446970763404192a2a0664706174680663666f6f", "[1, 'scheme', 3, b'ipv4', 4, 10794, 6, 'path', 6, 'path', 6, 'path']", "[5, 4, 6, 'foo']", "[1, 'scheme', 3, b'ipv4', 4, 10794, 6, 'path', 6, 'foo']", 0)]
        
        public void DebugMe(int line, string left, string middle, string right, string baseText, string relativeText, string resultText, int skip)
        {
            CBORObject cborLeft = CBORObject.DecodeFromBytes(HexToString(left));
            CBORObject cborRight = CBORObject.DecodeFromBytes(HexToString(right));
            CBORObject cborMiddle = CBORObject.DecodeFromBytes(HexToString(middle));

            Console.WriteLine($"base = {cborLeft}");
            Console.WriteLine($"href = {cborMiddle}");
            Console.WriteLine($"result = {cborRight}");

            Ciri ciriBase = new Ciri(cborLeft);
            Ciri ciriRight = new Ciri(cborRight);
            Ciri ciriMiddle = new Ciri(cborMiddle);

            Ciri newHref = ciriRight.MakeRelative(ciriBase);
            Assert.AreEqual(ciriMiddle.Data.ToString(), newHref.Data.ToString());
        }
#endif


    private static byte[]  HexToString(string hex)
        {
            var buffer = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                string hexdec = hex.Substring(i, 2);
                buffer[i / 2] = byte.Parse(hexdec, NumberStyles.HexNumber);
            }

            return buffer;
        }
    }
}
