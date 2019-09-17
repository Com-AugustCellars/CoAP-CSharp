using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Com.AugustCellars.CoAP;
using Com.AugustCellars.COSE;
using Com.AugustCellars.CoAP.DTLS;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.Server;
using Com.AugustCellars.CoAP.Server.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Utilities.Encoders;
using PeterO.Cbor;
using static Com.AugustCellars.CoAP.DTLS.TlsEvent.EventCode;


namespace Com.AugustCellars.CoAP.DTLS
{
    [TestClass]
    public class DtlsX509
    {
        private static TlsKeyPair X509Key;
        private static TlsKeyPair X509Client;
        private CoapServer _server;
        private Resource _resource;
        private int _serverPort;


        [ClassInitialize]
        public static void ClassSetup(TestContext ctx)
        {
            byte[] cert = Base64.Decode("MIIBHDCBz6ADAgECAhRzRMjlxi8nPr0B6DoN7e4sxwyb6jAFBgMrZXAwGDEWMBQGA1UEAwwNQ09TRSBDQSBUaHJlZTAeFw0xOTA3MDgwMTAyMjBaFw0yNzA5MjQwMTAyMjBaMBYxFDASBgNVBAMMC0NPU0UgRUUgU2l4MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEo0gYfGOYqcwGVra0OEiE0XXST/W4pTJ/HdTXZ7Ek/ycJZZn0jkHNQ9UCP7NJ16LOcZLUofev7OMHxct5DvuaPjAFBgMrZXADQQAqm5No83WC9W7tOkicP9wGu1HSdGCOR0CVjjzHfCfzRQkuSW2tRLBlstxzpqY6yrIuccMifhcrCdMe3fsPPS8G");
            CBORObject objKey = CBORObject.NewMap();
            objKey[CoseKeyKeys.KeyType] = GeneralValues.KeyType_EC;
            objKey[CoseKeyKeys.Algorithm] = AlgorithmValues.ECDSA_256;
            objKey[CoseKeyParameterKeys.EC_Curve] = GeneralValues.P256;
            objKey[CoseKeyParameterKeys.EC_D] = CBORObject.FromObject(Hex.Decode("7D29C4C7CDCBB2209CAD01F3BB4C9009782F66C2E1A6C592DF838A0795A6D87B"));
            objKey[CoseKeyParameterKeys.EC_X] = CBORObject.FromObject(Hex.Decode("A348187C6398A9CC0656B6B4384884D175D24FF5B8A5327F1DD4D767B124FF27"));
            objKey[CoseKeyParameterKeys.EC_Y] = CBORObject.FromObject(Hex.Decode("096599F48E41CD43D5023FB349D7A2CE7192D4A1F7AFECE307C5CB790EFB9A3E"));

            OneKey key = new OneKey(objKey);

            X509Key = new TlsKeyPair(cert, key);

            cert = Hex.Decode("3082011D3081D0A00302010202147344C8E5C62F273EBD01E83A0DEDEE2CC70C9BE9300506032B657030183116301406035504030C0D434F5345204341205468726565301E170D3139303730383031303231395A170D3237303932343031303231395A30173115301306035504030C0C434F534520454520466976653059301306072A8648CE3D020106082A8648CE3D03010703420004E8D9873804129C0A11238675C144CF00A7AF0E1A8ACF54A87BE76B9A0F2DBADF5384966EFD9C05B3DCEB3C074CF32410F033D962620C41F3892C0B94E3955D77300506032B65700341007BB0A5201998E6FC120364B96DD88211DD1FC907888C380E5A186BCD2F6A4CB238853EC22E5537CBCE5C157E59B4AEE71383372345932D0D40260857053FA608");
            objKey = CBORObject.NewMap();
            objKey[CoseKeyKeys.KeyType] = GeneralValues.KeyType_EC;
            objKey[CoseKeyKeys.Algorithm] = AlgorithmValues.ECDSA_256;
            objKey[CoseKeyParameterKeys.EC_Curve] = GeneralValues.P256;
            objKey[CoseKeyParameterKeys.EC_D] = CBORObject.FromObject(Hex.Decode("019F4FD19429DE078B2A013F5218CD64C24FABA1F6F0BE924E628E63BC67A8AC"));
            objKey[CoseKeyParameterKeys.EC_X] = CBORObject.FromObject(Hex.Decode("E8D9873804129C0A11238675C144CF00A7AF0E1A8ACF54A87BE76B9A0F2DBADF"));
            objKey[CoseKeyParameterKeys.EC_Y] = CBORObject.FromObject(Hex.Decode("5384966EFD9C05B3DCEB3C074CF32410F033D962620C41F3892C0B94E3955D77"));

            X509Client = new TlsKeyPair(cert, new OneKey(objKey));
        }

        [TestInitialize]
        public void SetupServer()
        {
            Com.AugustCellars.CoAP.Log.LogManager.Level = Com.AugustCellars.CoAP.Log.LogLevel.Fatal;
            CreateServer();
        }

        [TestCleanup]
        public void ShutdownServer()
        {
            _server.Dispose();
        }

        [TestMethod]
        public void TestX509()
        {
            Uri uri = new Uri($"coaps://localhost:{_serverPort}/Hello1");
            DTLSClientEndPoint client = new DTLSClientEndPoint(X509Client);
            client.TlsEventHandler += ClientTlsEvents;
            client.Start();

            Request req = new Request(Method.GET)
            {
                URI = uri,
                EndPoint = client
            };

            req.Send();
            String txt = req.WaitForResponse(50000).ResponseText;
            Assert.AreEqual("Hello from CN=COSE EE Five", txt);
            client.Stop();

            Thread.Sleep(5000);

        }

        private void CreateServer()
        {
            TlsKeyPairSet allKeys = new TlsKeyPairSet();
            allKeys.AddKey(X509Key);
            DTLSEndPoint endpoint = new DTLSEndPoint(allKeys, new KeySet(), 0);
            endpoint.TlsEventHandler += ServerTlsEvents;

            _resource = new DtlsX509.HelloResource("Hello1");
            _server = new CoapServer();
            _server.Add(_resource);

            _server.AddEndPoint(endpoint);
            _server.Start();
            _serverPort = ((System.Net.IPEndPoint)endpoint.LocalEndPoint).Port;
        }

        static void ServerTlsEvents(Object o, TlsEvent e)
        {
            if (e.Code == ClientCertificate) {
                e.Processed = true;
            }
        }

        static void ClientTlsEvents(Object o, TlsEvent e)
        {
            if (e.Code == ServerCertificate)
            {
                e.Processed = true;
            }
        }

        class HelloResource : Resource
        {
            public HelloResource(String name) : base(name)
            {

            }

            protected override void DoGet(CoapExchange exchange)
            {
                String content = $"Hello from ";

                if (exchange.Request.TlsContext.AuthenticationKey != null) {
                    content += Encoding.UTF8.GetString(exchange.Request.TlsContext.AuthenticationKey[CoseKeyKeys.KeyIdentifier].GetByteString());
                }
                else {
                    content += exchange.Request.TlsContext.AuthenticationCertificate.GetCertificateAt(0).Subject.ToString();
                }

                exchange.Respond(content);
            }
        }

    }
}
