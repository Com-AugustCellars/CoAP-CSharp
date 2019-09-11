using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP.Util
{
    public class Ciri
    {
        public static CBORObject ToCbor(Uri uri)
        {
            CBORObject ciri = CBORObject.NewArray();

            if (uri.IsAbsoluteUri) {
                ciri.Add(1);
                ciri.Add(uri.Scheme);
                switch (uri.HostNameType) {
                    case UriHostNameType.Dns:
                    case UriHostNameType.Basic:
                    case UriHostNameType.Unknown:
                        ciri.Add(2);
                        ciri.Add(uri.Host);
                        break;

                    case UriHostNameType.IPv4:
                        ciri.Add(3);
                        ciri.Add(IPAddress.Parse(uri.Host).GetAddressBytes());
                        break;

                    case UriHostNameType.IPv6:
                        ciri.Add(3);
                        ciri.Add(IPAddress.Parse(uri.Host).GetAddressBytes());
                        break;
                }

                ciri.Add(4);
                ciri.Add(uri.Port);
            }
            else {

            }

            string[] pathStrings = uri.AbsolutePath.Split('/');
            foreach (string s in pathStrings) {
                if (s.Length == 0) continue;
                ciri.Add(6);
                ciri.Add(s);
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
    }
}
