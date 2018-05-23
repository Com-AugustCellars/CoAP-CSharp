using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using Com.AugustCellars.CoAP.EndPoint.Resources;
using Com.AugustCellars.CoAP.Server.Resources;

namespace Com.AugustCellars.CoAP
{
    [TestClass]
    public class ResourceTest
    {
        public ResourceTest()
        {
            Log.LogManager.Level = Log.LogLevel.Fatal;
        }

        [TestMethod]
        public void SimpleTest()
        {
            String input = "</sensors/temp>;ct=41;rt=\"TemperatureC\"";
            RemoteResource root = RemoteResource.NewRoot(input);

            RemoteResource res = root.GetResource("/sensors/temp");
            Assert.IsNotNull(res);
            Assert.AreEqual(res.Name, "/sensors/temp");
            List<string> contents = res.Attributes.GetContentTypes().ToList();
            Assert.AreEqual(1, contents.Count);
            Assert.AreEqual("41", contents[0]);
            Assert.AreEqual("TemperatureC", res.ResourceType);
        }

        [TestMethod]
        public void SimpleTest_CBOR()
        {
            byte[] input = new byte[] {
                0x81, 0xa3,
                0x01, 0x6d, 0x2f, 0x73, 0x65, 0x6e, 0x73, 0x6F, 0x72, 0x73, 0x2F, 0x74, 0x65, 0x6D, 0x70,
                0x0C, 0x62, 0x34, 0x31,
                0x09, 0x6C, 0x54, 0x65, 0x6D, 0x70, 0x65, 0x72, 0x61, 0x74, 0x75, 0x72, 0x65, 0x43
            };
            RemoteResource root = RemoteResource.NewRoot(input, MediaType.ApplicationLinkFormatCbor);

            RemoteResource res = root.GetResource("/sensors/temp");
            Assert.IsNotNull(res);
            Assert.AreEqual(res.Name, "/sensors/temp");
            List<string> contents = res.Attributes.GetContentTypes().ToList();
            Assert.AreEqual(1, contents.Count);
            Assert.AreEqual("41", contents[0]);
            Assert.AreEqual("TemperatureC", res.ResourceType);
        }

        [TestMethod]
        public void SimpleTest_JSON()
        {
            String input = "[{\"href\":\"/sensors/temp\",\"ct\":\"41\",\"rt\":\"TemperatureC\"}]";
            RemoteResource root = RemoteResource.NewRoot(input, MediaType.ApplicationLinkFormatJson);

            RemoteResource res = root.GetResource("/sensors/temp");
            Assert.IsNotNull(res);
            Assert.AreEqual(res.Name, "/sensors/temp");
            List<string> contents = res.Attributes.GetContentTypes().ToList();
            Assert.AreEqual(1, contents.Count);
            Assert.AreEqual("41", contents[0]);
            Assert.AreEqual("TemperatureC", res.ResourceType);
        }


#if false
        // We have flattened out the tree because of full URIs so this test makes no sense any more
        [TestMethod]
        public void ExtendedTest()
        {
            String input = "</my/Päth>;rt=\"MyName\";if=\"/someRef/path\";ct=42;obs;sz=10";
            RemoteResource root = RemoteResource.NewRoot(input);

            RemoteResource my = new RemoteResource("my");
            my.ResourceType = "replacement";
            root.AddSubResource(my);

            RemoteResource res = root.GetResource("/my/Päth");
            Assert.IsNotNull(res);
            res = root.GetResource("my/Päth");
            Assert.IsNotNull(res);
            res = root.GetResource("my");
            res = res.GetResource("Päth");
            Assert.IsNotNull(res);
            res = res.GetResource("/my/Päth");
            Assert.IsNotNull(res);

            Assert.AreEqual(res.Name, "Päth");
            Assert.AreEqual(res.Path, "/my/Päth");
            Assert.AreEqual(res.ResourceType, "MyName");
            Assert.AreEqual(res.InterfaceDescriptions[0], "/someRef/path");
            Assert.AreEqual(1, res.Attributes.GetContentTypes().Count());
            Assert.AreEqual("42", res.Attributes.GetContentTypes().First());
            Assert.AreEqual(10, res.MaximumSizeEstimate);
            Assert.AreEqual(true, res.Observable);

            res = root.GetResource("my");
            Assert.IsNotNull(res);
            Assert.AreEqual("replacement", res.ResourceTypes[0]);
        }
#endif

        [TestMethod]
        public void ConversionTest()
        {
            String link1 = "</myUri/something>;ct=42;if=\"/someRef/path\";obs;rt=\"MyName\";sz=10";
            String link2 = "</myUri>;rt=\"NonDefault\"";
            String link3 = "</a>";
            String format = link1 + "," + link2 + "," + link3;
            RemoteResource res = RemoteResource.NewRoot(format);
            String result = LinkFormat.Serialize(res);
            Assert.AreEqual(link3 + "," + link2 + "," + link1, result);
        }

        [TestMethod]
        public void ConcreteTest()
        {
            String link = "</careless>;rt=\"SepararateResponseTester\";title=\"This resource will ACK anything, but never send a separate response\",</feedback>;rt=\"FeedbackMailSender\";title=\"POST feedback using mail\",</helloWorld>;rt=\"HelloWorldDisplayer\";title=\"GET a friendly greeting!\",</image>;ct=21;ct=22;ct=23;ct=24;rt=\"Image\";sz=18029;title=\"GET an image with different content-types\",</large>;rt=\"block\";title=\"Large resource\",</large_update>;rt=\"block observe\";title=\"Large resource that can be updated using PUT method\",</mirror>;rt=\"RequestMirroring\";title=\"POST request to receive it back as echo\",</obs>;obs;rt=\"observe\";title=\"Observable resource which changes every 5 seconds\",</query>;title=\"Resource accepting query parameters\",</seg1/seg2/seg3>;title=\"Long path resource\",</separate>;title=\"Resource which cannot be served immediately and which cannot be acknowledged in a piggy-backed way\",</storage>;obs;rt=\"Storage\";title=\"PUT your data here or POST new resources!\",</test>;title=\"Default test resource\",</timeResource>;rt=\"CurrentTime\";title=\"GET the current time\",</toUpper>;rt=\"UppercaseConverter\";title=\"POST text here to convert it to uppercase\",</weatherResource>;rt=\"ZurichWeather\";title=\"GET the current weather in zurich\"";
            RemoteResource res = RemoteResource.NewRoot(link);
            String result = LinkFormat.Serialize(res);
            Assert.AreEqual(link, result);
        }

        [TestMethod]
        public void MatchTest()
        {
            String link1 = "</myUri/something>;ct=42;if=\"/someRef/path\";obs;rt=\"MyName\";sz=10";
            String link2 = "</myUri>;ct=50;rt=\"MyName\"";
            String link3 = "</a>;sz=10;rt=\"MyNope\"";
            String format = link1 + "," + link2 + "," + link3;
            RemoteResource res = RemoteResource.NewRoot(format);

            List<String> query = new List<string>();
            query.Add("rt=MyName");

            String queried = LinkFormat.Serialize(res, query);
            Assert.AreEqual(link2 + "," + link1, queried);
        }

        [TestMethod]
        public void ConversionTestsToCbor()
        {
            string link = "</sensors>;ct=40;title=\"Sensor Index\",</sensors/temp>;rt=\"temperature-c\";if=\"sensor\"," +
                          "</sensors/light>;rt=\"light-lux\";if=\"sensor\",<http://www.example.com/sensors/t123>;anchor=\"/sensors/temp\"" +
                          ";rel=\"describedby\",</t>;anchor=\"/sensors/temp\";rel=\"alternate\"";
            byte[] cborX = new byte[] {0x85,
                0xA3, 0x01, 0x68, 0x2F, 0x73, 0x65, 0x6E, 0x73, 0x6F, 0x72, 0x73, 0x0C, 0x62, 0x34, 0x30, 0x07, 0x6C, 0x53, 0x65, 0x6E, 0x73, 0x6F, 0x72, 0x20, 0x49, 0x6E, 0x64, 0x65, 0x78,
                0xA3, 0x01, 0x6E, 0x2F, 0x73, 0x65, 0x6E, 0x73, 0x6F, 0x72, 0x73, 0x2F, 0x6C, 0x69, 0x67, 0x68, 0x74, 0x0A, 0x66, 0x73, 0x65, 0x6E, 0x73, 0x6F, 0x72, 0x09, 0x69, 0x6C, 0x69, 0x67, 0x68, 0x74, 0x2D, 0x6C, 0x75, 0x78,
                0xA3, 0x01, 0x6D, 0x2F, 0x73, 0x65, 0x6E, 0x73, 0x6F, 0x72, 0x73, 0x2F, 0x74, 0x65, 0x6D, 0x70, 0x0A, 0x66, 0x73, 0x65, 0x6E, 0x73, 0x6F, 0x72, 0x09, 0x6D, 0x74, 0x65, 0x6D, 0x70, 0x65, 0x72, 0x61, 0x74, 0x75, 0x72, 0x65, 0x2D, 0x63,
                0xA3, 0x01, 0x62, 0x2F, 0x74, 0x03, 0x6D, 0x2F, 0x73, 0x65, 0x6E, 0x73, 0x6F, 0x72, 0x73, 0x2F, 0x74, 0x65, 0x6D, 0x70, 0x02, 0x69, 0x61, 0x6C, 0x74, 0x65, 0x72, 0x6E, 0x61, 0x74, 0x65,
                0xA3, 0x01, 0x78, 0x23, 0x68, 0x74, 0x74, 0x70, 0x3A, 0x2F, 0x2F, 0x77, 0x77, 0x77, 0x2E, 0x65, 0x78, 0x61, 0x6D, 0x70, 0x6C, 0x65, 0x2E, 0x63, 0x6F, 0x6D, 0x2F, 0x73, 0x65, 0x6E, 0x73, 0x6F, 0x72, 0x73, 0x2F, 0x74, 0x31, 0x32, 0x33, 0x03, 0x6D, 0x2F, 0x73, 0x65, 0x6E, 0x73, 0x6F, 0x72, 0x73, 0x2F, 0x74, 0x65, 0x6D, 0x70, 0x02, 0x6B, 0x64, 0x65, 0x73, 0x63, 0x72, 0x69, 0x62, 0x65, 0x64, 0x62, 0x79
            };
            RemoteResource res = RemoteResource.NewRoot(link);

            byte[] cborOut = LinkFormat.SerializeCbor(res, null);
            Assert.AreEqual(cborOut, cborX);
        }

        [TestMethod]
        public void ConversionTestsToJson()
        {
            string link = "</sensors>;ct=40;title=\"Sensor Index\"," +
                          "</sensors/light>;if=\"sensor\";rt=\"light-lux\"," +
                          "</sensors/temp>;if=\"sensor\";obs;rt=\"temperature-c\"," +
                          "</t>;anchor=\"/sensors/temp\";rel=\"alternate\"," +
                          "<http://www.example.com/sensors/t123>;anchor=\"/sensors/temp\";ct=4711;foo=\"bar\";foo=3;rel=\"describedby\"";
            string jsonX = "[{\"href\":\"/sensors\",\"ct\":\"40\",\"title\":\"Sensor Index\"}," +
                           "{\"href\":\"/sensors/light\",\"if\":\"sensor\",\"rt\":\"light-lux\"}," +
                           "{\"href\":\"/sensors/temp\",\"if\":\"sensor\",\"obs\":true,\"rt\":\"temperature-c\"}," +
                           "{\"href\":\"/t\",\"anchor\":\"/sensors/temp\",\"rel\":\"alternate\"}," +
                           "{\"href\":\"http://www.example.com/sensors/t123\",\"anchor\":\"/sensors/temp\",\"ct\":\"4711\",\"foo\":[\"bar\",\"3\"],\"rel\":\"describedby\"}]";
            RemoteResource res = RemoteResource.NewRoot(link);

            string jsonOut = LinkFormat.SerializeJson(res, null);
            Assert.AreEqual(jsonOut, jsonX);

            res = RemoteResource.NewRoot(jsonX, MediaType.ApplicationLinkFormatJson);
            jsonOut = LinkFormat.Serialize(res, null);
            Assert.AreEqual(jsonOut, link);
        }

        [TestMethod]
        public void ParseFailures()
        {
            string[] tests = new string[] {
                "/myuri;ct=42",
                "</myuri;ct=42",
                "</myrui>;x=\"This has one quote"
            };

            foreach (string test in tests) {
                ArgumentException e = Assert.Throws<ArgumentException>(
                    () => RemoteResource.NewRoot(test));
                Assert.AreEqual("Value does not fall within the expected range.", e.Message);
            }
        }
    }
}
