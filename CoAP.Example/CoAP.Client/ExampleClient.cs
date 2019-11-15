﻿using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PeterO.Cbor;
using Com.AugustCellars.COSE;
using Com.AugustCellars.CoAP.Util;
using Com.AugustCellars.CoAP.DTLS;
using Com.AugustCellars.CoAP.Net;
using Com.AugustCellars.CoAP.OSCOAP;

namespace Com.AugustCellars.CoAP.Examples
{

    class ExampleClient
    {
        private static readonly CBORObject _UsageKey = CBORObject.FromObject("usage");

        public static void Main(String[] args)
        {
            String method = null;
            Uri uri = null;
            String payload = null;
            Boolean loop = false;
            Boolean byEvent = false;
            OneKey authKey = null;
            SecurityContext oscoap = null;
            SecurityContextSet contextSet = null;

            if (args.Length == 0)
                PrintUsage();

            Int32 index = 0;
            foreach (String arg in args)
            {
                if (arg[0] == '-')
                {
                    if (arg.Equals("-l"))
                        loop = true;
                    else if (arg.Equals("-e"))
                        byEvent = true;
                    else if (arg.StartsWith("-psk=")) {
                        if (authKey == null) {
                            authKey = new OneKey();
                            authKey.Add(COSE.CoseKeyKeys.KeyType, COSE.GeneralValues.KeyType_Octet);
                        }
                        authKey.Add(CoseKeyParameterKeys.Octet_k, CBORObject.FromObject(Encoding.UTF8.GetBytes(arg.Substring(5))));
                    }
                    else if (arg.StartsWith("-psk-id=")) {
                        if (authKey == null) {
                            authKey = new OneKey();
                            authKey.Add(COSE.CoseKeyKeys.KeyType, COSE.GeneralValues.KeyType_Octet);
                        }
                        authKey.Add(COSE.CoseKeyKeys.KeyIdentifier, CBORObject.FromObject(Encoding.UTF8.GetBytes(arg.Substring(8))));
                    }
                    else if (arg.StartsWith("-oscoap=")) {
                        if (contextSet == null) {
                            Console.WriteLine("Must have -oscoap-data before -oscoap");
                            Environment.Exit(1);
                        }

                        byte[] id = Encoding.UTF8.GetBytes(arg.Substring(8));

                        oscoap = contextSet.FindByGroupId(id).First();
                    }
                    else if (arg.StartsWith("-oscoap-data=")) {
                        contextSet = LoadContextSet(arg.Substring(13));
                    }
                    else Console.WriteLine("Unknown option: " + arg);
                }
                else
                {
                    switch (index)
                    {
                        case 0:
                            method = arg.ToUpper();
                            break;
                        case 1:
                            try
                            {
                                uri = new Uri(arg);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Failed parsing URI: " + ex.Message);
                                Environment.Exit(1);
                            }
                            break;
                        case 2:
                            payload = arg;
                            break;
                        default:
                            Console.WriteLine("Unexpected argument: " + arg);
                            break;
                    }
                    index++;
                }
            }

            if (method == null || uri == null)
                PrintUsage();

            Request request = NewRequest(method);
            if (request == null)
            {
                Console.WriteLine("Unknown method: " + method);
                Environment.Exit(1);
            }

            if ("OBSERVE".Equals(method))
            {
                request.MarkObserve();
                loop = true;
            }
            else if ("DISCOVER".Equals(method) &&
                (String.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath.Equals("/")))
            {
                uri = new Uri(uri, "/.well-known/core");
            }

            CoAPEndPoint ep = null;
            if (uri.Scheme == "coaps") {
                if (authKey == null) {
                    Console.WriteLine("Must use the -psk option to provide an authentication key");
                    return;
                }
                ep = new DTLSClientEndPoint(authKey);
                ep.Start();
                request.EndPoint = ep;
            }

            request.URI = uri;
            if (payload != null) {
                request.SetPayload(payload, MediaType.TextPlain);
            }
            if (oscoap != null) request.OscoreContext = oscoap;

            // uncomment the next line if you want to specify a draft to use
            // request.EndPoint = CoAP.Net.EndPointManager.Draft13;

            Console.WriteLine(Utils.ToString(request));

            try
            {
                if (byEvent)
                {
                    request.Respond += delegate(Object sender, ResponseEventArgs e)
                    {
                        Response response = e.Response;
                        if (response == null)
                        {
                            Console.WriteLine("Request timeout");
                        }
                        else
                        {
                            Console.WriteLine(Utils.ToString(response));
                            Console.WriteLine("Time (ms): " + response.RTT);
                        }
                        if (!loop) {
                            if (ep != null) ep.Stop();
                            Environment.Exit(0);
                        }
                    };
                    request.Send();
                    while (true)
                    {
                        Console.ReadKey();
                    }
                }
                else
                {
                    // uncomment the next line if you need retransmission disabled.
                    // request.AckTimeout = -1;

                    request.Send();

                    do
                    {
                        Console.WriteLine("Receiving response...");

                        Response response = null;
                        response = request.WaitForResponse();

                        if (response == null)
                        {
                            Console.WriteLine("Request timeout");
                            break;
                        }
                        else
                        {
                            Console.WriteLine(Utils.ToString(response));
                            Console.WriteLine("Time elapsed (ms): " + response.RTT);

                            if (response.ContentType == MediaType.ApplicationLinkFormat)
                            {
                                IEnumerable<WebLink> links = LinkFormat.Parse(response.PayloadString);
                                if (links == null)
                                {
                                    Console.WriteLine("Failed parsing link format");
                                    Environment.Exit(1);
                                }
                                else
                                {
                                    Console.WriteLine("Discovered resources:");
                                    foreach (var link in links)
                                    {
                                        Console.WriteLine(link);
                                    }
                                }
                            }
                        }
                    } while (loop);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed executing request: " + ex.Message);
                Console.WriteLine(ex);
                if (ep != null) ep.Stop();
                Environment.Exit(1);
            }
        }

        private static Request NewRequest(String method)
        {
            switch (method)
            {
                case "POST":
                    return Request.NewPost();
                case "PUT":
                    return Request.NewPut();
                case "DELETE":
                    return Request.NewDelete();
                case "GET":
                case "DISCOVER":
                case "OBSERVE":
                    return Request.NewGet();
                default:
                    return null;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("CoAP.NET Example Client");
            Console.WriteLine();
            Console.WriteLine("Usage: CoAPClient [-e] [-l] method uri [payload]");
            Console.WriteLine("  method  : { GET, POST, PUT, DELETE, DISCOVER, OBSERVE }");
            Console.WriteLine("  uri     : The CoAP URI of the remote endpoint or resource.");
            Console.WriteLine("  payload : The data to send with the request.");
            Console.WriteLine("Options:");
            Console.WriteLine("  -e      : Receives responses by the Responded event.");
            Console.WriteLine("  -l      : Loops for multiple responses.");
            Console.WriteLine("            (automatic for OBSERVE and separate responses)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  CoAPClient DISCOVER coap://localhost");
            Console.WriteLine("  CoAPClient POST coap://localhost/storage data");
            Environment.Exit(0);
        }

        private static SecurityContextSet LoadContextSet(string fileName)
        {
            if (fileName == null) fileName = "ServerKeys.cbor";
            KeySet keys = new KeySet();
            SecurityContextSet newSet = new SecurityContextSet();

            FileStream fs = new FileStream(fileName, FileMode.Open);
            using (BinaryReader reader = new BinaryReader(fs)) {
                byte[] data = reader.ReadBytes((int)fs.Length);
                CBORObject obj = CBORObject.DecodeFromBytes(data);
                for (int i = 0; i < obj.Count; i++) {
                    OneKey key = new OneKey(obj[i]);
                    string[] usages = key[_UsageKey].AsString().Split(' ');

                    foreach (String usage in usages) {
                        if (usage == "oscoap") {
                            SecurityContext ctx = SecurityContext.DeriveContext(
                                key[CoseKeyParameterKeys.Octet_k].GetByteString(),
                                null,
                                key[CBORObject.FromObject("RecipID")].GetByteString(),
                                key[CBORObject.FromObject("SenderID")].GetByteString(), null,
                                key[CoseKeyKeys.Algorithm]);
                            newSet.Add(ctx);
                            break;
                        }
                        else if (usage == "oscoap-group") {
                            SecurityContext ctx = SecurityContext.DeriveGroupContext(
                                key[CoseKeyParameterKeys.Octet_k].GetByteString(), key[CBORObject.FromObject(2)].GetByteString(), key[CBORObject.FromObject("SenderID")].GetByteString(),
                                null, null,
                                null, null, null, key[CoseKeyKeys.Algorithm]);
                            foreach (CBORObject recipient in key[CBORObject.FromObject("recipients")].Values) {
                                ctx.AddRecipient(recipient[CBORObject.FromObject("RecipID")].GetByteString(), new OneKey( recipient[CBORObject.FromObject("sign")]));
                            }
                            newSet.Add(ctx);
                        }
                    }

                    if ((usages.Length != 1) || (usages[0] != "oscoap")) {
                        keys.AddKey(key);
                    }
                }
                reader.Close();
            }

            //
            return newSet;

        }
    }
}
