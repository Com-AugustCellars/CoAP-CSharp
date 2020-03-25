using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Com.AugustCellars.CoAP.Codec
{
    [TestClass]
    public class MessageTest
    {
        [TestMethod]
        public void TestMessage()
        {
            Message msg = new Request(Method.GET, true);

            msg.ID = 12345;
            msg.Payload = System.Text.Encoding.UTF8.GetBytes("payload");

            byte[] data = Spec.Encode(msg);
            Message convMsg = Spec.Decode(data);

            Assert.AreEqual(msg.Code, convMsg.Code);
            Assert.AreEqual(msg.Type, convMsg.Type);
            Assert.AreEqual(msg.ID, convMsg.ID);
            Assert.AreEqual(msg.GetOptions().Count(), convMsg.GetOptions().Count());
            Assert.IsTrue(msg.Payload.SequenceEqual(convMsg.Payload));
        }

        [TestMethod]
        public void TestMessageWithOptions()
        {
            Message msg = new Request(Method.GET, true);

            msg.ID = 12345;
            msg.Payload = System.Text.Encoding.UTF8.GetBytes("payload");
            msg.AddOption(Option.Create(OptionType.ContentType, "text/plain"));
            msg.AddOption(Option.Create(OptionType.MaxAge, 30));

            byte[] data = Spec.Encode(msg);
            Message convMsg = Spec.Decode(data);

            Assert.AreEqual(msg.Code, convMsg.Code);
            Assert.AreEqual(msg.Type, convMsg.Type);
            Assert.AreEqual(msg.ID, convMsg.ID);
            Assert.AreEqual(msg.GetOptions().Count(), convMsg.GetOptions().Count());
            Assert.IsTrue(msg.GetOptions().SequenceEqual(convMsg.GetOptions()));
            Assert.IsTrue(msg.Payload.SequenceEqual(convMsg.Payload));
        }

        [TestMethod]
        public void TestMessageWithExtendedOption()
        {
            Message msg = new Request(Method.GET, true);

            msg.ID = 12345;
            msg.AddOption(Option.Create((OptionType)12, "a"));
            msg.AddOption(Option.Create((OptionType)197, "extend option"));
            msg.Payload = System.Text.Encoding.UTF8.GetBytes("payload");

            byte[] data = Spec.Encode(msg);
            Message convMsg = Spec.Decode(data);

            Assert.AreEqual(msg.Code, convMsg.Code);
            Assert.AreEqual(msg.Type, convMsg.Type);
            Assert.AreEqual(msg.ID, convMsg.ID);
            Assert.AreEqual(msg.GetOptions().Count(), convMsg.GetOptions().Count());
            Assert.IsTrue(msg.GetOptions().SequenceEqual(convMsg.GetOptions()));
            Assert.IsTrue(msg.Payload.SequenceEqual(convMsg.Payload));

            Option extendOpt = convMsg.GetFirstOption((OptionType)197);
            Assert.IsNotNull(extendOpt);
            Assert.AreEqual(extendOpt.StringValue, "extend option");
        }

        [TestMethod]
        public void TestRequestParsing()
        {
            Message request = new Request(Method.POST, false);
            request.ID = 7;
            request.Token = new byte[] { 11, 82, 165, 77, 3 };
            request.AddIfMatch(new byte[] { 34, 239 })
                .AddIfMatch(new byte[] { 88, 12, 254, 157, 5 });
            request.ContentType = 40;
            request.Accept = 40;

            byte[] bytes = Spec.NewMessageEncoder().Encode(request);
            IMessageDecoder decoder = Spec.NewMessageDecoder(bytes);
            Assert.IsTrue(decoder.IsRequest);

            Request result = decoder.DecodeRequest();
            Assert.AreEqual(request.ID, result.ID);
            Assert.IsTrue(request.Token.SequenceEqual(result.Token));
            Assert.IsTrue(request.GetOptions().SequenceEqual(result.GetOptions()));
        }

        [TestMethod]
        public void TestResponseParsing()
        {
            Message response = new Response(StatusCode.Content);
            response.Type = MessageType.NON;
            response.ID = 9;
            response.Token = new byte[] { 22, 255, 0, 78, 100, 22 };
            response.AddETag(new byte[] { 1, 0, 0, 0, 0, 1 })
                                .AddLocationPath("/one/two/three/four/five/six/seven/eight/nine/ten")
                                .AddOption(Option.Create((OptionType)57453, "Arbitrary".GetHashCode()))
                                .AddOption(Option.Create((OptionType)19205, "Arbitrary1"))
                                .AddOption(Option.Create((OptionType)19205, "Arbitrary2"))
                                .AddOption(Option.Create((OptionType)19205, "Arbitrary3"));

            byte[] bytes = Spec.NewMessageEncoder().Encode(response);

            IMessageDecoder decoder = Spec.NewMessageDecoder(bytes);
            Assert.IsTrue(decoder.IsResponse);

            Message result = decoder.Decode();
            Assert.AreEqual(response.ID, result.ID);
            Assert.IsTrue(response.Token.SequenceEqual(result.Token));
            Assert.IsTrue(response.GetOptions().SequenceEqual(result.GetOptions()));
        }

        [TestMethod]
        public void TestSignalParsing()
        {
            Message signal = new SignalMessage(SignalCode.CSM);
            signal.Type = MessageType.NON;
            signal.ID = 15;
            signal.Token = new byte[] {33, 3, 5, 0, 39, 40};

            byte[] bytes = Spec.NewMessageEncoder().Encode(signal);

            IMessageDecoder decoder = Spec.NewMessageDecoder(bytes);
            Assert.IsTrue(decoder.IsSignal);

            Message result = decoder.Decode();
            Assert.AreEqual(signal.ID, result.ID);
            Assert.IsTrue(signal.Token.SequenceEqual(result.Token));
            Assert.IsTrue(signal.GetOptions().SequenceEqual(result.GetOptions()));

        }

        [TestMethod]
        public void TestEmptyParsing()
        {
            Message signal = new EmptyMessage(MessageType.RST);
            signal.Type = MessageType.NON;
            signal.ID = 15;
            signal.Token = new byte[] { 33, 3, 5, 0, 39, 40 };

            byte[] bytes = Spec.NewMessageEncoder().Encode(signal);

            IMessageDecoder decoder = Spec.NewMessageDecoder(bytes);
            Assert.IsTrue(decoder.IsEmpty);

            Message result = decoder.Decode();
            Assert.AreEqual(signal.ID, result.ID);
            Assert.IsTrue(signal.Token.SequenceEqual(result.Token));
            Assert.IsTrue(signal.GetOptions().SequenceEqual(result.GetOptions()));

        }

    }
}
