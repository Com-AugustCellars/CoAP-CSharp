/*
 * Copyright (c) 2011-2013, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// This class describes the CoAP Media Type Registry as defined in
    /// RFC 7252, Section 12.3.
    /// </summary>
    public class MediaType
    {
        /// <summary>
        /// undefined
        /// </summary>
        public const int Undefined = -1;
        /// <summary>
        /// text/plain; charset=utf-8
        /// </summary>
        public const int TextPlain = 0;
        /// <summary>
        /// text/xml
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int TextXml = 1;
        /// <summary>
        /// text/csv
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int TextCsv = 2;
        /// <summary>
        /// text/html
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int TextHtml = 3;
        /// <summary>
        /// Application/cose; cose-type="cose-encrypt0"
        /// </summary>
        public const int ApplicationCoseEncrypt0 = 16;
        /// <summary>
        /// Application/cose; cose-type="cose-mac0"
        /// </summary>
        public const int ApplicationCoseMac0 = 17;
        /// <summary>
        /// Application/cose; cose-type="cose-sign1"
        /// </summary>
        public const int ApplicationCoseSign1 = 18;
        /// <summary>
        /// image/gif
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int ImageGif = 21;
        /// <summary>
        /// image/jpeg
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int ImageJpeg = 22;
        /// <summary>
        /// image/png
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int ImagePng = 23;
        /// <summary>
        /// image/tiff
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int ImageTiff = 24;
        /// <summary>
        /// audio/raw
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int AudioRaw = 25;
        /// <summary>
        /// video/raw
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int VideoRaw = 26;
        /// <summary>
        /// application/link-format
        /// </summary>
        public const int ApplicationLinkFormat = 40;
        /// <summary>
        /// application/xml
        /// </summary>
        public const int ApplicationXml = 41;
        /// <summary>
        /// application/octet-stream
        /// </summary>
        public const int ApplicationOctetStream = 42;
        /// <summary>
        /// application/rdf+xml
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int ApplicationRdfXml = 43;
        /// <summary>
        /// application/soap+xml
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int ApplicationSoapXml = 44;
        /// <summary>
        /// application/atom+xml
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int ApplicationAtomXml = 45;
        /// <summary>
        /// application/xmpp+xml
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int ApplicationXmppXml = 46;
        /// <summary>
        /// application/exi
        /// </summary>
        public const int ApplicationExi = 47;
        /// <summary>
        /// application/fastinfoset
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int ApplicationFastinfoset = 48;
        /// <summary>
        /// application/soap+fastinfoset
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int ApplicationSoapFastinfoset = 49;
        /// <summary>
        /// application/json [RFC 7159]
        /// </summary>
        public const int ApplicationJson = 50;
        /// <summary>
        /// application/json-patch+json [RFC 6902]
        /// </summary>
        public const int ApplicationJsonPatchJson = 51;
        /// <summary>
        /// application/x-obix-binary
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int ApplicationXObixBinary = 51;
        /// <summary>
        /// application/merge-patch+json [RFC 7396]
        /// </summary>
        public const int ApplicationMergePatchJson = 52;
        /// <summary>
        /// application/cbor - [RFC 7049]
        /// </summary>
        public const int ApplicationCbor = 60;
        /// <summary>
        /// application/cwt [RFC 8392]
        /// </summary>
        public const int ApplicationCwt = 61;
        /// <summary>
        /// application/multipart-core [draft-ietf-core-multipart-ct]
        /// </summary>
        public const int ApplicationMultipartCore = 62;

#if false  // Work is dead?
        /// <summary>
        /// application/link-format+cbor - [RFC TBD]
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int ApplicationLinkFormatCbor = 64;
#endif

        public const int ApplicationCoseEncrypt = 96;
        public const int ApplicationCoseMac = 97;
        public const int ApplicationCoseSign = 98;
        public const int ApplicationCoseKey = 101;
        public const int ApplicationCoseKeySet = 102;

#if false
        /// <summary>
        /// application/link-format+json - [RFC TBD]
        /// </summary>
        [Obsolete("Media type was never registered")]
        public const int ApplicationLinkFormatJson = 504;
#endif

        /// <summary>
        /// application/ace+cbor - [draft-ietf-ace-authz]
        /// </summary>
        public const int ApplicationAceCbor = 65000;

        public const int ApplicationCoralReef = 65088;
        public const int Coral = 999;
        /// <summary>
        /// any
        /// </summary>
        public const int Any = -2;

        public class MediaTypeInfo
        {
            public string[] ContentType { get; }
            public bool IsText { get; }
            public bool IsCbor { get; }
            public MediaTypeInfo(string[] contentType, bool isText=false, bool isCbor=false)
            {
                ContentType = contentType;
                IsText = isText;
                IsCbor = isCbor;
            }
        }

        private static readonly Dictionary<int, MediaTypeInfo> registry = new Dictionary<int, MediaTypeInfo>();

        static MediaType()
        {
            registry.Add(TextPlain, new MediaTypeInfo(new string[] { "text/plain", "txt" }, true));
            registry.Add(TextXml, new MediaTypeInfo(new string[] { "text/xml", "xml" }, true));
            registry.Add(TextCsv, new MediaTypeInfo(new string[] { "text/csv", "csv" }, true));
            registry.Add(TextHtml, new MediaTypeInfo(new string[] { "text/html", "html" }, true));

            registry.Add(ImageGif, new MediaTypeInfo(new string[] { "image/gif", "gif" }));
            registry.Add(ImageJpeg, new MediaTypeInfo(new string[] { "image/jpeg", "jpg" }));
            registry.Add(ImagePng, new MediaTypeInfo(new string[] { "image/png", "png" }));
            registry.Add(ImageTiff, new MediaTypeInfo(new string[] { "image/tiff", "tif" }));
            registry.Add(AudioRaw, new MediaTypeInfo(new string[] { "audio/raw", "raw" }));
            registry.Add(VideoRaw, new MediaTypeInfo(new string[] { "video/raw", "raw" }));

            registry.Add(ApplicationLinkFormat, new MediaTypeInfo(new string[] { "application/link-format", "wlnk" }, true));
            registry.Add(ApplicationXml, new MediaTypeInfo(new string[] { "application/xml", "xml" }, true));
            registry.Add(ApplicationOctetStream, new MediaTypeInfo(new string[] { "application/octet-stream", "bin" }));
            registry.Add(ApplicationRdfXml, new MediaTypeInfo(new string[] {"application/rdf+xml", "rdf"}, true));
            registry.Add(ApplicationSoapXml, new MediaTypeInfo(new string[] {"application/soap+xml", "soap"}, true));
            registry.Add(ApplicationAtomXml, new MediaTypeInfo(new string[] {"application/atom+xml", "atom"}, true));
            registry.Add(ApplicationXmppXml, new MediaTypeInfo(new string[] {"application/xmpp+xml", "xmpp"}, true));
            registry.Add(ApplicationFastinfoset, new MediaTypeInfo(new string[] { "application/fastinfoset", "finf"}));
            registry.Add(ApplicationSoapFastinfoset, new MediaTypeInfo(new string[] {"application/soap+fastinfoset", "soap.finf"}));
            registry.Add(ApplicationXObixBinary, new MediaTypeInfo(new string[] { "application/x-obix-binary", "obix" }));
            registry.Add(ApplicationExi, new MediaTypeInfo(new string[] { "application/exi", "exi" }));
            registry.Add(ApplicationJson, new MediaTypeInfo(new string[] { "application/json", "json" }, false));  // Compressed w/ deflate

            registry.Add(Coral, new MediaTypeInfo(new string[] {"XX", "coral"}, false, true));
        }

        /// <summary>
        /// Checks whether the given media type is a type of image.
        /// </summary>
        /// <param name="mediaType">The media type to be checked</param>
        /// <returns>True iff the media type is a type of image</returns>
        public static Boolean IsImage(int mediaType)
        {
            return mediaType >= ImageGif && mediaType <= ImageTiff;
        }

        public static Boolean IsPrintable(int mediaType)
        {
            MediaTypeInfo val;
            if (!registry.TryGetValue(mediaType, out val)) {
                return false;
            }

            return val.IsText;
        }

        public static Boolean IsCbor(int mediaType)
        {
            MediaTypeInfo val;
            if (!registry.TryGetValue(mediaType, out val)) {
                return false;
            }

            return val.IsCbor;
        }

        /// <summary>
        /// Returns a string representation of the media type.
        /// </summary>
        /// <param name="mediaType">The media type to be described</param>
        /// <returns>A string describing the media type</returns>
        public static string ToString(int mediaType)
        {
            if (registry.ContainsKey(mediaType)) {
                return registry[mediaType].ContentType[0];
            }
            else {
                return "unknown/" + mediaType;
            }
        }

        /// <summary>
        /// Gets the file extension of the given media type.
        /// </summary>
        public static string ToFileExtension(int mediaType)
        {
            if (registry.ContainsKey(mediaType))
            {
                return registry[mediaType].ContentType[1];
            }
            else {
                return "unknown_" + mediaType;
            }
        }

        public static int NegotiationContent(int defaultContentType, IEnumerable<int> supported, IEnumerable<Option> accepted)
        {
            if (accepted == null)
                return defaultContentType;

            Boolean hasAccept = false;
            foreach (Option accept in accepted)
            {
                foreach (int ct in supported)
                {
                    if (ct == accept.IntValue)
                        return ct;
                }
                hasAccept = true;
            }
            return hasAccept ? Undefined : defaultContentType;
        }

        public static int Parse(string type)
        {
            if (type == null)
                return Undefined;

            foreach (KeyValuePair<int, MediaTypeInfo> pair in registry)
            {
                if (pair.Value.ContentType[0].Equals(type, StringComparison.OrdinalIgnoreCase))
                    return pair.Key;
            }

            return Undefined;
        }

        public static IEnumerable<int> ParseWildcard(string regex)
        {
            regex = regex.Trim().Substring(0, regex.IndexOf('*')).Trim() + ".*";
            Regex r = new Regex(regex);

            foreach (KeyValuePair<int, MediaTypeInfo> pair in registry)
            {
                string mime = pair.Value.ContentType[0];
                if (r.IsMatch(mime))
                    yield return pair.Key;
            }
        }
    }
}
