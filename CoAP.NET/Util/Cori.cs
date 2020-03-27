using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Util
{
    public class Cori
    {
        public CBORObject Data { get; }

        public Cori(CBORObject uri)
        {
            Data = uri;
        }

        public Cori(string uri)
        {
            List<object> data = new List<object>();
            char[] characters = uri.ToCharArray();
            int iStart = 0;
            int i = 0;
            int len = characters.Length;
            string scheme = null;

            //  Look for a scheme
            while (i < len && characters[i] != ':') {
                if (characters[i] == '/' || characters[i] == '?' || characters[i] == '#') {
                    break;
                }

                i += 1;
            }

            if (i < len && characters[i] == ':') {
                scheme = uri.Substring(iStart, i - iStart);
                data.Add(OptionScheme);
                data.Add(scheme);
                iStart = i + 1;
                i++;
            }
            else {
                i = 0;
            }

            // Look for a host name/address
            if (i < len - 1 && characters[i] == '/' && characters[i + 1] == '/') {
                iStart += 2;
                i += 2;
                if (i < len && characters[i] == '[') {
                    while (i < len && characters[i] != ']') {
                        i += 1;
                    }

                    i += 1;

                    IPAddress ipv6 = IPAddress.Parse(uri.Substring(iStart, i - iStart));
                    data.Add(OptionHostIp);
                    data.Add(CBORObject.FromObject(ipv6.GetAddressBytes()));
                }
                else {
                    while (i < len && !"/:?#".Contains(characters[i])) {
                        i += 1;
                    }

                    string s = uri.Substring(iStart, i - iStart);
                    if (char.IsDigit(s[0])) {
                        IPAddress ipv4 = IPAddress.Parse(s);
                        data.Add(OptionHostIp);
                        data.Add(ipv4.GetAddressBytes());
                    }
                    else {
                        data.Add(OptionHostName);
                        data.Add(s);
                    }

                }

                iStart = i + 1;

                //  Look for the port
                if (i < len && characters[i] == ':') {
                    i += 1;
                    iStart = i;
                    while (i < len && characters[i] != '/') {
                        i += 1;
                    }

                    data.Add(OptionPort);
                    data.Add(int.Parse(uri.Substring(iStart, i - iStart)));

                    i += 1;
                    iStart = i;
                }
                else {
                    if (scheme != null && UriInformation.UriDefaults.ContainsKey(scheme)) {
                        data.Add(OptionPort);
                        data.Add(UriInformation.UriDefaults[scheme].DefaultPort);
                    }
                }

                if (i < len && characters[i] == '/') {
                    i += 1;
                    iStart = i;
                }
            }

            if (i < len && characters[i] == '/') {
                data.Add(OptionPathType);
                data.Add(0);
                i += 1;
                iStart = i;
            }

            //  Path
            int pathType = 0;
            while (i < len && characters[i] != '?' && characters[i] != '#') {
                if (i < len && characters[i] == '/') {
                    i += 1;
                }

                iStart = i;
                while (i < len && !"/?#".Contains(characters[i])) {
                    i += 1;
                }

                string s = uri.Substring(iStart, i - iStart);
                if (s == ".") {
                    if (pathType != 0) {
                        throw new Exception("Error?");
                    }

                    pathType = 2;
                }
                else if (s == "..") {
                    if (pathType >= 2) {
                        pathType += 1;
                    }
                    else if (pathType == 0) {
                        pathType = 3;
                    }
                    else {
                        throw new Exception("Error?");
                    }
                }
                else {
                    if (pathType > 0) {
                        data.Add(OptionPathType);
                        data.Add(pathType);
                        pathType = -2;
                    }

                    data.Add(OptionPath);
                    data.Add(uri.Substring(iStart, i - iStart));
                }


                iStart = i;
            }

            if (pathType > 0) {
                data.Add(OptionPathType);
                data.Add(pathType);
            }

            // Query

            if (i < len && characters[i] == '?') {
                i += 1;
                iStart = i;

                while (i < len && characters[i] != '#') {
                    i += 1;
                }

                string[] queries = uri.Substring(iStart, i - iStart).Split(new char[] {'&', ';'});
                foreach (string s in queries) {
                    data.Add(OptionQuery);
                    data.Add(s);
                }

                iStart = i;
            }

            // Fragment

            if (i < len && characters[i] == '#') {
                data.Add(OptionFragment);
                data.Add(uri.Substring(iStart + 1, len - (iStart + 1)));
                iStart = len;
            }

            if (iStart < len) {
                data.Add(OptionPath);
                data.Add(uri.Substring(iStart, len - iStart));
            }

            CBORObject d = CBORObject.NewArray();
            foreach (object o in data) {
                d.Add(o);
            }

            Data = d;
        }

        public static CBORObject ToCbor(Uri uri)
        {
            CBORObject ciri = CBORObject.NewArray();

            if (uri.IsAbsoluteUri) {
                ciri.Add(OptionScheme);
                ciri.Add(uri.Scheme);
                switch (uri.HostNameType) {
                case UriHostNameType.Dns:
                case UriHostNameType.Basic:
                case UriHostNameType.Unknown:
                    ciri.Add(OptionHostName);
                    ciri.Add(uri.Host);
                    break;

                case UriHostNameType.IPv4:
                    ciri.Add(OptionHostIp);
                    ciri.Add(IPAddress.Parse(uri.Host).GetAddressBytes());
                    break;

                case UriHostNameType.IPv6:
                    ciri.Add(OptionHostIp);
                    ciri.Add(IPAddress.Parse(uri.Host).GetAddressBytes());
                    break;
                }

                if (!uri.IsDefaultPort) {
                    ciri.Add(OptionPort);
                    ciri.Add(uri.Port);
                }
            }
            else {

            }


            string[] pathStrings = uri.Segments;
            foreach (string s in pathStrings) {
                if (s.Length == 0) continue;
                ciri.Add(6);
                ciri.Add(s);
            }

            if (!string.IsNullOrEmpty(uri.Query)) {
                string[] options = uri.Query.Trim('?').Split('&');
                foreach (string s in options) {
                    ciri.Add(OptionQuery);
                    ciri.Add(s);
                }
            }

            if (!string.IsNullOrEmpty(uri.Fragment)) {
                ciri.Add(OptionFragment);
                ciri.Add(uri.Fragment.Trim('#'));
            }


            return ciri;
        }

        public static CBORObject ToCbor(string localPath)
        {
            CBORObject ciri = CBORObject.NewArray();

            string[] pathStrings = localPath.Split('/');
            foreach (string s in pathStrings) {
                if (s.Length == 0) continue;
                ciri.Add(6);
                ciri.Add(s);
            }

            return ciri;
        }

        private const int OptionBegin = -1;
        private const int OptionScheme = 0;
        private const int OptionHostName = 1;
        private const int OptionHostIp = 2;
        private const int OptionPort = 3;
        private const int OptionPathType = 4;
        private const int OptionPath = 5;
        private const int OptionQuery = 6;
        private const int OptionFragment = 7;
        private const int OptionEnd = 8;

        private const int PathTypeAbsolutePath = 0;
        private const int PathTypeAppendRelation = 1;
        private const int PathTypeAppendPath = 2;
        private const int PathTypeRelativePath = 3;
        private const int PathTypeRelativePath1Up = 4;
        private const int PathTypeRelativePath2Up = 5;
        private const int PathTypeRelativePath3Up = 6;
        private const int PathTypeRelativePath4Up = 7;


        private static readonly int[][] transistions = new[] {
            new int[] {OptionScheme, OptionHostName, OptionHostIp, OptionPathType, OptionPath, OptionQuery, OptionFragment, OptionEnd}, // Begin
            new int[] {OptionHostName, OptionHostIp, OptionPath, OptionQuery, OptionFragment, OptionEnd}, // Schema
            new int[] {OptionPort, OptionPath, OptionQuery, OptionFragment, OptionEnd}, // Host Name
            new int[] {OptionPort, OptionPath, OptionQuery, OptionFragment, OptionEnd}, // Host IP
            new int[] {OptionPath, OptionQuery, OptionFragment, OptionEnd}, // Port
            new int[] {OptionPath, OptionQuery, OptionFragment, OptionEnd}, // PathType
            new int[] {OptionPath, OptionQuery, OptionFragment, OptionEnd}, // Path
            new int[] {OptionQuery, OptionFragment, OptionEnd}, // Query
            new int[] {OptionEnd}, // Fragment
            new int[] {OptionEnd} 
        };

        public bool IsWellFormed()
        {
            return IsWellFormed(Data);
        }

        public static bool IsWellFormed(CBORObject root)
        {
            int previous = OptionBegin;
            if (root.Type != CBORType.Array) return false;

            for (int i = 0; i < root.Count; i += 2) {
                int option = root[i].AsInt32();
                if (!transistions[previous+1].Contains(option)) {
                    return false;
                }

                previous = option;
            }

            return transistions[previous+1].Contains(OptionEnd);
        }

        public bool IsAbsolute()
        {
            return IsAbsolute(Data);
        }


    public static bool IsAbsolute(CBORObject root)
        {
            return root.Values.Count != 0 && root.Type == CBORType.Array &&root[0].AsInt32() == OptionScheme && IsWellFormed(root);
        }

    public bool IsRelative()
    {
        return IsRelative(Data);
    }

        public static bool IsRelative(CBORObject root)
        {
            return root.Values.Count == 0 || (root.Type == CBORType.Array && root[0].AsInt32() != OptionScheme && IsWellFormed(root));
        }

        /// <summary>
        /// Resolve the current CRI relative to a provided CRI value
        /// </summary>
        /// <param name="baseRef">CRI to resolve relative to</param>
        /// <returns></returns>
        public Cori ResolveTo(Cori baseRef)
        {
            CBORObject o = Resolve(baseRef.Data, Data);
            if (o == null) {
                throw new CoAPException("Unable to resolve the two URIs.");
            }
            return new Cori(o);

        }


        private static readonly int[] resolveT = new int[]{0, 0, 0, -1, -2, 1, 0, 0, 0};
        private static readonly int[] resolveE = new int[] {0, 1, 1, -1, -2, 6, 6, 7, 7};

        /// <summary>
        /// Resolve one CRI relative to a base CRI to form an absolute CRI
        /// </summary>
        /// <param name="baseRefIn">The CRI to resolve to</param>
        /// <param name="hrefIn">The "relative" CRI to resolve</param>
        /// <returns></returns>

        public static CBORObject Resolve(CBORObject baseRefIn, CBORObject hrefIn)
        {
            if (!IsAbsolute(baseRefIn) || !IsWellFormed(hrefIn)) {
                return null;
            }

            List<CBORObject> result = new List<CBORObject>();
            List<CBORObject> href = new List<CBORObject>(hrefIn.Values);
            List<CBORObject> baseRef = new List<CBORObject>(baseRefIn.Values);

            int t = hrefIn.Count > 0 ? resolveT[href[0].AsInt32()] : 0;
            int e = hrefIn.Count > 0 ? resolveE[href[0].AsInt32()] : 7;

            if (t == -2) {
                t = href[1].AsInt32() - 1;
                e = (t < 0) ? 5 : 6;
            }

            for (int i = 0; i < baseRef.Count; i += 2) {
                if (baseRef[i].AsInt32() >= e) {
                    break;
                }

                result.Add(baseRef[i]);
                result.Add(baseRef[i + 1]);
            }

            for (; t > 0; t--) {
                if (result[result.Count - 2].AsInt32() != OptionPath) {
                    break;
                }
                result.RemoveRange(result.Count - 2, 2);
            }

            for (int i = 0; i < hrefIn.Count; i += 2) {
                if (hrefIn[i].AsInt32() == OptionPathType) {
                    continue;
                }

                result.Add(hrefIn[i]);
                result.Add(hrefIn[i + 1]);
            }

            return ArrayToCbor(result);
        }

        private static CBORObject ArrayToCbor(List<CBORObject> result)
        {
            CBORObject returnValue = CBORObject.NewArray();
            foreach (CBORObject o in result) {
                returnValue.Add(o);
            }

            return returnValue;
        }


        /// <inheritdoc />
    public override string ToString()
        {
            return AsString(Data);
        }

        public static string AsString(CBORObject cbor)
        {
            StringBuilder sb = new StringBuilder();
            bool emitSlash = false;
            int index = 0;
            int option = 0;
            bool seenQuery = false;
            string scheme = null;

            foreach (CBORObject o in cbor.Values) {
                if (index % 2 == 0) {
                    option = o.AsInt32();
                }
                else {
                    switch (option) {
                    case OptionBegin:
                        break;

                    case OptionScheme:
                        sb.Append(o.AsString());
                        sb.Append(":");
                        scheme = o.AsString();
                        break;

                    case OptionHostName:
                        sb.Append("//");
                        sb.Append(o.AsString());
                        emitSlash = true;
                        break;

                    case OptionHostIp:
                        sb.Append("//");
                        IPAddress ipAddr = new IPAddress(o.GetByteString());
                        if (ipAddr.AddressFamily == AddressFamily.InterNetworkV6) {
                            sb.Append($"[{ipAddr}]");
                        }
                        else {
                            sb.Append(ipAddr);
                        }
                        ipAddr = null;
                        break;

                    case OptionPort:
                        if (scheme == null || !UriInformation.UriDefaults.ContainsKey(scheme) || o.AsInt32() != UriInformation.UriDefaults[scheme].DefaultPort) {
                            sb.Append(":");
                            sb.Append(o.AsInt32());
                        }

                        emitSlash = true;
                        break;

                    case OptionPathType:
                        switch (o.AsInt32()) {
                                case 0:
                                    sb.Append("/");
                                    break;

                                case 1:
                                    sb.Append("||1||");
                                    break;

                                case 2:
                                    sb.Append(".");
                                    emitSlash = true;
                                    break;

                                default:
                                    int count = o.AsInt32();
                                    if (count <= 127) {
                                        count = count - 3;
                                        for (int i = 0; i < count; i++) {
                                            sb.Append("../");
                                        }

                                        sb.Append("..");
                                        emitSlash = true;
                                    }
                                    else {
                                        sb.Append($"||{o.AsInt32()}||");
                                    }

                                    break;
                        }
                        break;

                    case OptionPath:
                        if (emitSlash) sb.Append("/");
                        sb.Append(o.AsString());
                        emitSlash = true;
                        break;

                    case OptionQuery:
                        sb.Append(seenQuery ? "&" : "?");
                        sb.Append(o.AsString());
                        seenQuery = true;
                        break;

                    case OptionFragment:
                        sb.Append("#");
                        sb.Append(o.AsString());
                        break;

                    case OptionEnd:
                        break;
                    }
                }

                index += 1;
            }

            return sb.ToString();
        }

        public Cori MakeRelative(Cori baseHref)
        {
            if (!baseHref.IsAbsolute() || !IsAbsolute()) {
                throw new ArgumentException("Must be absolute URIs");
            } 

            if (this.Equals(baseHref)) {
                return new Cori(CBORObject.NewArray());
            }

            List<CBORObject> baseUri = new List<CBORObject>(baseHref.Data.Values);
            List<CBORObject> href = new List<CBORObject>(Data.Values);

            //  Strip leading matches

            int lastOption = OptionBegin;
            while (baseUri.Count >= 2 && href.Count >= 2 && baseUri[0].Equals(href[0]) && baseUri[1].Equals(href[1])) {
                lastOption = baseUri[0].AsInt32();
                baseUri.RemoveRange(0, 2);
                href.RemoveRange(0, 2);
            }

            bool removedTail = false;
            //  Strip trailing matches
            if (baseUri.Count > 0) {
                int opt = OptionEnd;
                if (lastOption >= OptionPort) {
                    opt = lastOption;
                }

                if (opt > OptionPath && href.Count > 0) {
                    int t = href[href.Count - 2].AsInt32();
                    if (t < opt && t >= OptionPath) {
                        opt = t;
                    }
                }

                if (opt > OptionPath && href.Count > 2) {
                    int t = href[href.Count - 4].AsInt32();
                    if (t < opt && t >= OptionPath) {
                        opt = t;
                    }
                }

                if (opt <= OptionPath) {
                    opt = OptionQuery;
                }

                while (baseUri.Count > 0 && baseUri[baseUri.Count - 2].AsInt32() >= opt) {
                    baseUri.RemoveRange(baseUri.Count - 2, 2);
                    removedTail = true;
                }
            }

            if (removedTail && lastOption == OptionPort) {
                if (baseUri.Count == 0) {
                    href.Insert(0, CBORObject.FromObject(PathTypeAbsolutePath));
                    href.Insert(0, CBORObject.FromObject(OptionPathType));
                }
                else if (baseUri.Count == 2) {
                    href.Insert(0, CBORObject.FromObject(PathTypeRelativePath));
                    href.Insert(0, CBORObject.FromObject(OptionPathType));
                }
                else {
                    href.Insert(0, CBORObject.FromObject(PathTypeRelativePath1Up + baseUri.Count - 1));
                    href.Insert(0, CBORObject.FromObject(OptionPathType));
                }
                return new Cori(ArrayToCbor(href));
            }


            if (removedTail && lastOption == OptionPath) {
                if (baseUri.Count == 0) {
                    href.Insert(0, CBORObject.FromObject(PathTypeAppendPath));
                    href.Insert(0, CBORObject.FromObject(OptionPathType));
                }
                else if (baseUri.Count == 2) {
                    href.Insert(0, CBORObject.FromObject(PathTypeRelativePath));
                    href.Insert(0, CBORObject.FromObject(OptionPathType));
                }
                else {
                    href.Insert(0, CBORObject.FromObject(PathTypeRelativePath1Up + baseUri.Count - 1));
                    href.Insert(0, CBORObject.FromObject(OptionPathType));
                }
                return new Cori(ArrayToCbor(href));
            }

            if (baseUri.Count == 0 && href.Count == 0) {
                // Not sure how we got here as this should have been dealt with already.
                return new Cori(CBORObject.NewArray());
            }

            if (baseUri.Count == 0 || baseUri[0].AsInt32() <= OptionPort) {
                //  Ran the entire base set - what is left is the relative URI
                //  If we did not get down to the port option, then what is left is the relative URI
                if (lastOption == OptionPath) {
                    href.Insert(0, CBORObject.FromObject(PathTypeAppendPath));
                    href.Insert(0, CBORObject.FromObject(OptionPathType));
                }
                return new Cori(ArrayToCbor(href));
            }

            if (href.Count == 0) {
                if (lastOption == OptionPort) {
                    href.Add(CBORObject.FromObject( OptionPathType));
                    href.Add(CBORObject.FromObject(PathTypeAbsolutePath));
                    return new Cori( ArrayToCbor(href));
                }
                else if (baseUri.Count == 2) {
                    href.Add(CBORObject.FromObject(OptionPathType));
                    href.Add(CBORObject.FromObject(PathTypeRelativePath));
                    return new Cori(ArrayToCbor(href));
                }
                else {
                    href.Add(CBORObject.FromObject(OptionPathType));
                    href.Add(CBORObject.FromObject(PathTypeRelativePath + baseUri.Count/2 - 1));
                    return new Cori(ArrayToCbor(href));
                }
            }

            if (baseUri.Count > 0 && lastOption >= OptionPath) {
                href.Insert(0, CBORObject.FromObject(PathTypeRelativePath + baseUri.Count/2  - 1));
                href.Insert(0, CBORObject.FromObject(OptionPathType));
                return new Cori(ArrayToCbor(href));
            }

            if (lastOption == OptionPort) {
                href.Insert(0, CBORObject.FromObject(PathTypeAbsolutePath));
                href.Insert(0, CBORObject.FromObject(OptionPathType));
                return new Cori(ArrayToCbor(href));
            }

            return new Cori(ArrayToCbor(href));
        }

        private static CBORObject Array2ToCbor(List<object> result)
        {
            CBORObject returnValue = CBORObject.NewArray();
            foreach (object o in result) {
                returnValue.Add(o);
            }

            return returnValue;

        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is Cori)) {
                return false;
            }

            Cori right = (Cori) obj;
            if (this == right) {
                return true;
            }

            if (right == null) {
                return false;
            }

            if (!IsAbsolute() || !right.IsAbsolute()) {
                throw new ArgumentException("Can only compare absolute CoRI values");
            }

            if (Data.Count != right.Data.Count) {
                return false;
            }

            for (int i = 0; i < Data.Count; i++) {
                if (!Data[i].Equals(right.Data[i])) {
                    return false;
                }
            }

            return true;
        }
    }
}
