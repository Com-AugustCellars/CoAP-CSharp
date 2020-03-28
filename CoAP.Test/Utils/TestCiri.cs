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
        [DataRow("coap:", "[0, \"coap\"]")]
        [DataRow("coap://hostName", "[0, \"coap\", 1, \"hostName\"]")]
        [DataRow("coap://server:99", "[0, \"coap\", 1, \"server\", 3, 99]")]
        [DataRow("coap://1.2.3.4", "[0, \"coap\", 2, h'01020304']")]
        [DataRow("coap://[1::4]", "[0, \"coap\", 2, h'00010000000000000000000000000004']")]
        [DataRow("coap://[1::2]:99", "[0, \"coap\", 2, h'00010000000000000000000000000002', 3, 99]")]
        // [DataRow("coap://hostname:5683", "[0, \"coap\", 1, \"hostname\"]", "coap://hostname")]
        [DataRow("//server", "[1, \"server\"]")]
        [DataRow("coap:/path", "[0, \"coap\", 4, 0, 5, \"path\"]")]
        [DataRow("coap:path", "[0, \"coap\", 5, \"path\"]")]
        [DataRow("coap://testServer:99/alpha/beta?abc&def#frag", "[0, \"coap\", 1, \"testServer\", 3, 99, 5, \"alpha\", 5, \"beta\", 6, \"abc\", 6, \"def\", 7, \"frag\"]")]
        [DataRow("mailto:john.doe@example.com", "[0, \"mailto\", 5, \"john.doe@example.com\"]")]
        [DataRow("/alpha/beta", "[4, 0, 5, \"alpha\", 5, \"beta\"]")]
        [DataRow("", "[]")]
        [DataRow(".", "[4, 2]")]
        [DataRow("./", "[4, 2, 5, \"\"]")]
        [DataRow("..", "[4, 3]")]
        [DataRow("../", "[4, 3, 5, \"\"]")]
        [DataRow("../g", "[4, 3, 5, \"g\"]")]
        [DataRow("../..", "[4, 4]")]
        [DataRow("../../", "[4, 4, 5, \"\"]")]
        [DataRow("../../g", "[4, 4, 5, \"g\"]")]

        public void Parse(string url, string cbor)
        {
            Cori o = new Cori(url);
            Assert.AreEqual(cbor, o.Data.ToString());
            string rebuild = o.ToString();

            Assert.AreEqual(url, rebuild);
        }


        [TestMethod]
        [DataRow("http://a/b/c/d?p&q", "g:h", "[0, \"g\", 5, \"h\"]")]
        [DataRow("http://a/b/c/d?p&q", "g", "[0, \"http\", 1, \"a\", 5, \"b\", 5, \"c\", 5, \"g\"]")]
        [DataRow("http://a/b/c/d?p&q", "./g", "[0, \"http\", 1, \"a\", 5, \"b\", 5, \"c\", 5, \"g\"]")]
        [DataRow("http://a/b/c/d?p&q", "g/", "[0, \"http\", 1, \"a\", 5, \"b\", 5, \"c\", 5, \"g\", 5, \"\"]")]
        [DataRow("http://a/b/c/d?p&q", "/g", "[0, \"http\", 1, \"a\", 5, \"g\"]")]
        [DataRow("http://a/b/c/d?p&q", "//g", "[0, \"http\", 1, \"g\"]")]
        [DataRow("http://a/b/c/d?p&q", "?y", "[0, \"http\", 1, \"a\", 5, \"b\", 5, \"c\", 5, \"d\", 6, \"y\"]")]
        [DataRow("http://a/b/c/d?p&q", "g?y", "[0, \"http\", 1, \"a\", 5, \"b\", 5, \"c\", 5, \"g\", 6, \"y\"]")]
        [DataRow("http://a/b/c/d?p&q", "#s", "[0, \"http\", 1, \"a\", 5, \"b\", 5, \"c\", 5, \"d\", 6, \"p\", 6, \"q\", 7, \"s\"]")]
        [DataRow("http://a/b/c/d?p&q", "g#s", "[0, \"http\", 1, \"a\", 5, \"b\", 5, \"c\", 5, \"g\", 7, \"s\"]")]
        [DataRow("http://a/b/c/d?p&q", "g?y#s", "[0, \"http\", 1, \"a\", 5, \"b\", 5, \"c\", 5, \"g\", 6, \"y\", 7, \"s\"]")]
        [DataRow("http://a/b/c/d?p&q", "", "[0, \"http\", 1, \"a\", 5, \"b\", 5, \"c\", 5, \"d\", 6, \"p\", 6, \"q\"]")]
        // M00DONTFIX - There is a difference between URL resolve and CIRI resolve where a trailing empty directory is not always added
        //       This is per Klaus.   Personally I find it objectionable
        [DataRow("http://a/b/c/d?p&q", ".", "[0, \"http\", 1, \"a\", 5, \"b\", 5, \"c\"]")]
        [DataRow("http://a/b/c/d?p&q", "./", "[0, \"http\", 1, \"a\", 5, \"b\", 5, \"c\", 5, \"\"]")]
        [DataRow("http://a/b/c/d?p&q", "..", "[0, \"http\", 1, \"a\", 5, \"b\"]")]
        [DataRow("http://a/b/c/d?p&q", "../", "[0, \"http\", 1, \"a\", 5, \"b\", 5, \"\"]")]
        [DataRow("http://a/b/c/d?p&q", "../g", "[0, \"http\", 1, \"a\", 5, \"b\", 5, \"g\"]")]
        [DataRow("http://a/b/c/d?p&q", "../..", "[0, \"http\", 1, \"a\"]")]
        [DataRow("http://a/b/c/d?p&q", "../../", "[0, \"http\", 1, \"a\", 5, \"\"]")]
        [DataRow("http://a/b/c/d?p&q", "../../g", "[0, \"http\", 1, \"a\", 5, \"g\"]")]
        public void Resolve(string baseUri, string href, string cbor)
        {
            Cori baseCori = new Cori(baseUri);
            Cori hrefCori = new Cori(href);

            Console.WriteLine(baseCori.Data.ToString());
            Console.WriteLine(hrefCori.Data.ToString());

            Cori resolved = hrefCori.ResolveTo(baseCori);
            Assert.AreEqual(cbor, resolved.Data.ToString());
        }

#if false
        [TestMethod]
        [DynamicData(nameof(KlausData), DynamicDataSourceType.Method)]
        public void TestsFromKlaus(string line, string left, string middle, string right, string baseText, string relativeText, string resultText, string skip)
        {
            try {
                CBORObject cborLeft = CBORObject.DecodeFromBytes(HexToString(left));
                CBORObject cborRight = CBORObject.DecodeFromBytes(HexToString(right));
                CBORObject cborMiddle = CBORObject.DecodeFromBytes(HexToString(middle));

                // Console.WriteLine($"line # = {line}");
                // Console.WriteLine($"base = {cborLeft}");
                // Console.WriteLine($"href = {cborMiddle}");
                // Console.WriteLine($"result = {cborRight}");

                Cori coriBase = new Cori(cborLeft);
                Cori coriRight = new Cori(cborRight);
                Cori coriMiddle = new Cori(cborMiddle);

                Assert.IsTrue(coriBase.IsAbsolute());
                Assert.IsFalse(coriBase.IsRelative());
                Assert.IsTrue(coriRight.IsAbsolute());
                Assert.IsFalse(coriRight.IsRelative());
                Assert.IsTrue(coriMiddle.IsWellFormed());

                Cori result = coriMiddle.ResolveTo(coriBase);
                Assert.AreEqual(coriRight, result);

                if (cborMiddle.Count > 0 && cborMiddle[0].AsInt32() == 5 && cborMiddle[1].AsInt32() == 1) {
                    return;
                }

                Cori newHref = coriRight.MakeRelative(coriBase);
                // Console.WriteLine($"computed href = {newHref.Data}");

                Cori resolve2 = newHref.ResolveTo(coriBase);
                Assert.AreEqual(coriRight.Data.ToString(), resolve2.Data.ToString());

                if (skip != "0") {
                   // Assert.AreEqual(coriMiddle.Data.ToString(), newHref.Data.ToString());
                }

            }
            catch (Exception e) {
                Assert.Fail(e.ToString());
            }
        }
#endif

        private static IEnumerable<string[]> KlausData()
        {
            string fileName = Path.Combine("CoRAL", "Klaus.csv");
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

            Cori coriBase = new Cori(cborLeft);
            Cori ciriRight = new Cori(cborRight);
            Cori ciriMiddle = new Cori(cborMiddle);

            Cori newHref = ciriRight.MakeRelative(coriBase);
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
