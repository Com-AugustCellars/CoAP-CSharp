﻿/*
 * Copyright (c) 2011-2014, Longxiang He <helongxiang@smeshlink.com>,
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
using System.Linq;
using System.Text;
using Com.AugustCellars.CoAP.Coral;
using Com.AugustCellars.CoAP.EndPoint.Resources;
using Com.AugustCellars.CoAP.Log;
using Com.AugustCellars.CoAP.Server.Resources;
using Com.AugustCellars.CoAP.Util;
using PeterO.Cbor;

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// This class provides link format definitions as specified in
    /// draft-ietf-core-link-format-06
    /// </summary>
    public static class LinkFormat
    {
        /// <summary>
        /// What is the set of attributes that have space separated values.
        /// Being on this list affects not only parsing but serialization as well.
        /// </summary>
        public static string[] SpaceSeparatedValueAttributes = new string[] {
            "rt", "rev", "if", "rel"
        };

        /// <summary>
        /// What is the set of attributes that must appear only once in a link format
        /// </summary>
        public static string[] SingleOccurenceAttributes = new string[] {
            "title",  "sz", "obs"
        };

        /// <summary>
        /// Should the parsing be strict or not.
        /// Enforces the Single Occurance rule.
        /// </summary>
        public static bool ParseStrictMode = false;

        /// <summary>
        /// Name of the attribute Resource Type
        /// </summary>
        public static readonly string ResourceType = "rt";

        /// <summary>
        /// Name of the attribute Interface Description
        /// </summary>
        public static readonly string InterfaceDescription = "if";

        /// <summary>
        /// Name of the attribute Content Type
        /// </summary>
        public static readonly string ContentType = "ct";

        /// <summary>
        /// Name of the attribute Max Size Estimate
        /// </summary>
        public static readonly string MaxSizeEstimate = "sz";

        /// <summary>
        /// Name of the attribute Title
        /// </summary>
        public static readonly string Title = "title";

        /// <summary>
        /// Name of the attribute Observable
        /// </summary>
        public static readonly string Observable = "obs";

        /// <summary>
        /// Name of the attribute link
        /// </summary>
        public static readonly string Link = "href";

#if false
        /// <summary>
        /// The string as the delimiter between resources
        /// </summary>
        public static readonly string Delimiter = ",";
#endif
        /// <summary>
        /// The string to separate attributes
        /// </summary>
        public static readonly string Separator = ";";

#if false
        public static readonly Regex DelimiterRegex = new Regex("\\s*" + Delimiter + "+\\s*");
        public static readonly Regex SeparatorRegex = new Regex("\\s*" + Separator + "+\\s*");

        public static readonly Regex ResourceNameRegex = new Regex("<[^>]*>");
        public static readonly Regex WordRegex = new Regex("\\w+");
        public static readonly Regex QuotedString = new Regex("\\G\".*?\"");
        public static readonly Regex Cardinal = new Regex("\\G\\d+");
#endif

        private static readonly ILogger _Log = LogManager.GetLogger(typeof(LinkFormat));

        //  Mapping defined in the RFC
        public static readonly Dictionary<string, CBORObject> CborAttributeKeys = new Dictionary<string, CBORObject>() {
            ["href"] = CBORObject.FromObject(1),
            ["rel"] = CBORObject.FromObject(2),
            ["anchor"] = CBORObject.FromObject(3),
            ["rev"] = CBORObject.FromObject(4),
            ["hreflang"] = CBORObject.FromObject(5),
            ["media"] = CBORObject.FromObject(6),
            ["title"] = CBORObject.FromObject(7),
            ["type"] = CBORObject.FromObject(8),
            ["rt"] = CBORObject.FromObject(9),
            ["if"] = CBORObject.FromObject(10),
            ["sz"] = CBORObject.FromObject(11),
            ["ct"] = CBORObject.FromObject(12),
            ["obs"] = CBORObject.FromObject(13)
        };

        public static readonly Dictionary<string, CBORObject> CborCoralKeys = new Dictionary<string, CBORObject>() {
            ["hreflang"] = CBORObject.FromObject(10),
            ["media"] = CBORObject.FromObject(11),
            ["title"] = CBORObject.FromObject(12),
            ["type"] = CBORObject.FromObject(13),
            ["rt"] = CBORObject.FromObject(14),
            ["if"] = CBORObject.FromObject(15),
            ["sz"] = CBORObject.FromObject(16),
            ["ct"] = CBORObject.FromObject(17),
            ["ct"] = CBORObject.FromObject(18),
            ["obs"] = CBORObject.FromObject(20)
        };

        public static readonly Dictionary<string, string> CoralsKeys = new Dictionary<string, string>() {
            ["ct"] = "http://coreapps.org/reef#ct",
            ["sz"] = "http://coreapps.org/reef#sz",
            ["if"] = "http://coreapps.org/reef#if",
            ["rt"] = "http://coreapps.org/reef#rt",
            ["type"] = "http://coreapps.org/coap#type",
            ["media"] = "http://coreapps.org/coap#media",
        };

        public static CoralDictionary ReefDictionary = new CoralDictionary() {
            {0, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type"},
            {1, "http://www.iana.org/assignments/relation/item"},
            {2, "http://www.iana.org/assignments/relation/collection"},
            {3, "http://coreapps.org/collections#create"},
            {4, "http://coreapps.org/base#update"},
            {5, "https://coreapps.org/collecitons#delete"},
            {6, "http://coreapps.org/base#search"},
            {7, "http://coreapps.org/coap#accept"},
            {8, "http://coreapps.org/reef#rd-unit"},
            {9, "http://coreapps.org/reef#rd-item"},
            {10, "http://coreapps.org/base#lang"},
            {11, "http://coreapps.org/reef#media"},
            {12, "http://coreapps.org/reef#title"},
            {13, "http://coreapps.org/reef#type"},
            {14, "http://coreapps.org/reef#rt"},
            {15, "http://coreapps.org/reef#if"},
            {16, "http://coreapps.org/reef#sz"},
            {17, "http://coreapps.org/reef#ct"},
            {18, "/.well-known/core"}

        };

        /// <summary>
        /// Serialize resources starting at a resource node into WebLink format
        /// </summary>
        /// <param name="root">resource to start at</param>
        /// <returns>web link format string</returns>
        public static string Serialize(IResource root)
        {
            return Serialize(root, null);
        }

        /// <summary>
        /// Serialize resources starting at a resource node into WebLink format
        /// </summary>
        /// <param name="root">resource to start at</param>
        /// <param name="queries">queries to filter the serialization</param>
        /// <returns>web link format string</returns>
        public static string Serialize(IResource root, IEnumerable<string> queries)
        {
            StringBuilder linkFormat = new StringBuilder();

            List<string> queryList = null;
            if (queries != null) queryList = queries.ToList();

            if (root.Children != null) {
                foreach (IResource child in root.Children) {
                    SerializeTree(child, queryList, linkFormat);
                }
            }

            if (linkFormat.Length > 1) linkFormat.Remove(linkFormat.Length - 1, 1);

            return linkFormat.ToString();
        }

#if false  // Work is dead?
        public static byte[] SerializeCbor(IResource root, IEnumerable<string> queries)
        {
            CBORObject linkFormat = CBORObject.NewArray();

            List<string> queryList = null;
            if (queries != null) queryList = queries.ToList();

            foreach (IResource child in root.Children) {
                SerializeTree(child, queryList, linkFormat, CborAttributeKeys);
            }

            return linkFormat.EncodeToBytes();
        }

        public static string SerializeJson(IResource root, IEnumerable<string> queries)
        {
            CBORObject linkFormat = CBORObject.NewArray();

            List<string> queryList = null;
            if (queries != null) queryList = queries.ToList();

            foreach (IResource child in root.Children) {
                SerializeTree(child, queryList, linkFormat, null);
            }

            return linkFormat.ToJSONString();
        }
#endif
        public static byte[] SerializeCoral(IResource root, IEnumerable<string> queries)
        {
            CoralDocument nodeRoot = new CoralDocument();
            

            List<string> queryList = null;
            if (queries != null) queryList = queries.ToList();

            foreach (IResource child in root.Children) {
                SerializeTreeInCoral(child, queryList, nodeRoot, CborAttributeKeys);
            }

            return nodeRoot.EncodeToBytes(null, ReefDictionary);
        }

        public static IEnumerable<WebLink> Parse(string linkFormat)
        {
            if (string.IsNullOrEmpty(linkFormat)) {
                yield break;
            }

            string[] resources = SplitOn(linkFormat, ',');

            foreach (string resource in resources) {
                string[] attributes = SplitOn(resource, ';');
                if (attributes[0][0] != '<' || attributes[0][attributes[0].Length - 1] != '>') {
                    throw new ArgumentException();
                }
                WebLink link = new WebLink(attributes[0].Substring(1, attributes[0].Length-2));

                for (int i = 1; i < attributes.Length; i++) {
                    int eq = attributes[i].IndexOf('=');
                    string name = eq == -1 ? attributes[i] : attributes[i].Substring(0, eq);

                    if (ParseStrictMode && SingleOccurenceAttributes.Contains(name)) {
                        throw new ArgumentException($"'{name}' occurs multiple times");
                    }


                    if (eq == -1) {
                        link.Attributes.Add(name);
                    }
                    else {
                        string value = attributes[i].Substring(eq + 1);
                        if (value[0] == '"') {
                            if (value[value.Length-1] != '"') throw new ArgumentException();
                            value = value.Substring(1, value.Length - 2);
                        }
                        link.Attributes.Set(name, value);
                    }
                }

                yield return link;
            }
        }

#if false  // Work is dead?
        public static IEnumerable<WebLink> ParseCbor(byte[] linkFormat)
        {
            CBORObject links = CBORObject.DecodeFromBytes(linkFormat);
            return ParseCommon(links, CborAttributeKeys);
        }

        public static IEnumerable<WebLink> ParseJson(string linkFormat)
        {
            CBORObject links = CBORObject.FromJSONString(linkFormat);
            return ParseCommon(links, null);
        }
#endif
        private static IEnumerable<WebLink> ParseCommon(CBORObject links, Dictionary<string, CBORObject> dictionary)
        {
            if (links.Type != CBORType.Array) throw new ArgumentException("Not an array");

            for (int i = 0; i < links.Count; i++) {
                CBORObject resource = links[i];
                if (resource.Type != CBORType.Map) throw new ArgumentException("Element not correctly formatted");

                string name;
                if (resource.ContainsKey("href")) name = resource["href"].AsString();
                else name = resource[CBORObject.FromObject(1)].AsString();

                WebLink link = new WebLink(name);

                foreach (CBORObject key in resource.Keys) {
                    string keyName = null;
                    if (dictionary != null && key.Type == CBORType.Integer) {
                        foreach (KeyValuePair<string, CBORObject> kvp in dictionary) {
                            if (key.Equals(kvp.Value)) {
                                keyName = kvp.Key;
                                break;
                            }
                        }
                    }
                    if (keyName == null) keyName = key.AsString();
                    if (keyName == "href") continue;

                    if (ParseStrictMode && SingleOccurenceAttributes.Contains(keyName)) {
                        throw new ArgumentException($"'{keyName}' occurs multiple times");
                    }

                    CBORObject value = resource[key];
                    if (value.Type == CBORType.Boolean) {
                        link.Attributes.Add(keyName);
                    }
                    else if (value.Type == CBORType.TextString) {
                        link.Attributes.Add(keyName, value.AsString());
                    }
                    else if (value.Type == CBORType.Array) {
                        for (int i1 = 0; i1 < value.Count; i1++) {
                            if (value[i1].Type == CBORType.Boolean) {
                                link.Attributes.Add(keyName);
                            }
                            else if (value[i1].Type == CBORType.TextString) {
                                link.Attributes.Add(keyName, value[i1].AsString());
                            }
                            else throw new ArgumentException("incorrect type");
                        }
                    }
                    else throw new ArgumentException("incorrect type");
                }

                yield return link;

            }
        }

        private static void SerializeTree(IResource resource, List<string> queries, StringBuilder sb)
        {
            if (resource.Visible && Matches(resource, queries)) {
                SerializeResource(resource, sb);
                sb.Append(",");
            }

            if (resource.Children == null) return;

            // sort by resource name
            List<IResource> children = new List<IResource>(resource.Children);
            children.Sort((r1, r2) => string.CompareOrdinal(r1.Name, r2.Name));

            foreach (IResource child in children) {
                SerializeTree(child, queries, sb);
            }
        }

#if false  // Work is dead?
        private static void SerializeTree(IResource resource, List<string> queries, CBORObject cbor, Dictionary<string, CBORObject> dictionary)
        {
            if (resource.Visible && Matches(resource, queries)) {
                SerializeResource(resource, cbor, dictionary);
            }

            if (resource.Children == null) return;

            // sort by resource name
            List<IResource> children = new List<IResource>(resource.Children);
            children.Sort((r1, r2) => string.CompareOrdinal(r1.Name, r2.Name));

            foreach (IResource child in children) {
                SerializeTree(child, queries, cbor, dictionary);
            }
        }
#endif

        private static void SerializeTreeInCoral(IResource resource, List<string> queries, CoralBody coral,
                                                 Dictionary<string, CBORObject> dictionary)
        {

            if (resource.Visible && Matches(resource, queries)) {
                SerializeResourceInCoral(resource, coral, dictionary);
            }

            if (resource.Children == null) return;

            //  sort by resource name
            List<IResource> children = new List<IResource>(resource.Children);
            children.Sort((r1, r2) => string.CompareOrdinal(r1.Name, r2.Name));

            foreach (IResource child in children) {
                SerializeTreeInCoral(child, queries, coral, dictionary);
            }
        }

        public static void SerializeResource(IResource resource, StringBuilder sb, ResourceAttributes otherAttributes = null,
                                             Uri uriRelative = null)
        {
            sb.Append("<");
            if (uriRelative != null) {
                sb.Append(new Uri(uriRelative, resource.Path + resource.Name).ToString());
            }
            else {
                sb.Append(resource.Path)
                    .Append(resource.Name);
            }
            sb.Append(">");
            SerializeAttributes(resource.Attributes, sb, uriRelative);
            if (otherAttributes != null) {
                SerializeAttributes(otherAttributes, sb, uriRelative);
            }
        }

#if false  // Work is dead?
        public static void SerializeResource(IResource resource, CBORObject cbor, Dictionary<string, CBORObject> dictionary,
                                             ResourceAttributes otherAttributes = null, Uri uriRelative = null)
        {
            CBORObject obj = CBORObject.NewMap();
            string href;
            if (uriRelative == null) {
                href = resource.Path + resource.Name;
            }
            else {
                href = new Uri(uriRelative, resource.Path + resource.Name).ToString();
            }

            if (dictionary == null) {
                obj.Add("href", href);
            }
            else {
                obj.Add(1, href);
            }
            SerializeAttributes(resource.Attributes, obj, dictionary, uriRelative);
            if (otherAttributes != null) {
                SerializeAttributes(otherAttributes, obj, dictionary, uriRelative);
            }

            cbor.Add(obj);
        }
#endif

        public static void SerializeResourceInCoral(IResource resource, CoralBody coral,
                                               Dictionary<string, CBORObject> dictionary,
                                               ResourceAttributes otherAttributes = null, Uri uriRelative = null,
                                               bool isEndPoint = false)
        {
            CBORObject obj = CBORObject.NewArray();
            CBORObject href;
            if (uriRelative == null) {
                href =Cori.ToCbor(resource.Path + resource.Name);
            }
            else {
                href = Cori.ToCbor(new Uri(uriRelative, resource.Path + resource.Name));
            }

            CoralBody body = new CoralBody();

            SerializeAttributesInCoral(resource.Attributes, body, dictionary, uriRelative);
            if (otherAttributes != null) {
                SerializeAttributesInCoral(otherAttributes, body, dictionary, uriRelative);
            }

            if (body.Length == 0) {
                body = null;
            }

            CoralItem item = new CoralLink(isEndPoint ? "http://coreapps.org/ref#rd-unit" : "http://coreapps.org/reef#rd-item", href.AsString(), body);
            coral.Add(item);
        }

        private static void SerializeAttributes(ResourceAttributes attributes, StringBuilder sb, Uri uriRelative)
        {
            List<string> keys = new List<string>(attributes.Keys);
            keys.Sort();
            foreach (string name in keys) {
                List<string> values = new List<string>(attributes.GetValues(name));
                if (values.Count == 0) {
                    continue;
                }

                if (uriRelative != null && name == "anchor") {
                    List<string> newValues = new List<string>();
                    foreach (string val in values) {
                        newValues.Add(new Uri(uriRelative, val).ToString());
                    }

                    values = newValues;
                }
                sb.Append(Separator);
                SerializeAttribute(name, values, sb);
            }
        }

#if false  // Work is dead?
        private static void SerializeAttributes(ResourceAttributes attributes, CBORObject cbor, Dictionary<string, CBORObject> dictionary, Uri uriRelative)
        {
            List<string> keys = new List<string>(attributes.Keys);
            keys.Sort();
            foreach (string name in keys) {
                List<string> values = new List<string>(attributes.GetValues(name));
                if (values.Count == 0) {
                    continue;
                }

                if (uriRelative != null && name == "anchor") {
                    List<string> newValues = new List<string>();
                    foreach (string val in values) {
                        newValues.Add(new Uri(uriRelative, val).ToString());
                    }

                    values = newValues;
                }

                SerializeAttribute(name, values, cbor, dictionary);
            }
        }
#endif

        private static void SerializeAttributesInCoral(ResourceAttributes attributes, CoralBody coral, Dictionary<string, CBORObject> dictionary, Uri uriRelative)
        {
            List<string> keys = new List<string>(attributes.Keys);
            keys.Sort();
            foreach (string name in keys) {
                if (!CoralsKeys.ContainsKey(name)) {
                    continue;
                }

                List<string> values = new List<string>(attributes.GetValues(name));
                if (values.Count == 0)
                {
                    continue;
                }

                if (uriRelative != null && name == "anchor")
                {
                    List<string> newValues = new List<string>();
                    foreach (string val in values)
                    {
                        newValues.Add(new Uri(uriRelative, val).ToString());
                    }

                    values = newValues;
                }

                SerializeAttributeInCoral(name, values, coral, null);
            }
        }

        private static void SerializeAttribute(string name, List<string> values, StringBuilder sb)
        {
            bool quotes = false;
            bool useSpace = SpaceSeparatedValueAttributes.Contains(name);
            bool first = true;

            foreach (string value in values) {
                if (first || !useSpace) {
                    sb.Append(name);
                }

                if (string.IsNullOrEmpty(value)) {
                    if (!useSpace) sb.Append(';');
                    first = false;
                    continue;
                }

                if (first || !useSpace) {
                    sb.Append('=');
                    if ((useSpace && values.Count > 1) || !IsNumber(value)) {
                        sb.Append('"');
                        quotes = true;
                    }
                }
                else {
                    sb.Append(' ');
                }

                sb.Append(value);

                if (!useSpace) {
                    if (quotes) {
                        sb.Append('"');
                        quotes = false;
                    }
                    sb.Append(';');
                }

                first = false;
            }
            if (quotes) {
                sb.Append('"');
            }

            if (!useSpace) {
                sb.Length = sb.Length - 1;
            }
        }

#if false  // Work is dead?
        private static void SerializeAttribute(string name, List<string> values, CBORObject cbor, Dictionary<string, CBORObject> dictionary)
        {
            bool useSpace = SpaceSeparatedValueAttributes.Contains(name);
            CBORObject result;

            if (useSpace && values.Count > 1) {
                StringBuilder sb = new StringBuilder();

                foreach (string value in values) {
                    sb.Append(value);
                    sb.Append(" ");
                }
                sb.Length = sb.Length - 1;

                result = CBORObject.FromObject(sb.ToString());
            }
            else if (values.Count == 1) {
                string value = values.First();
                if (string.IsNullOrEmpty(value)) result = CBORObject.True;
                else result = CBORObject.FromObject(values.First());
            }
            else {
                result = CBORObject.NewArray();
                foreach (string value in values) {
                    if (string.IsNullOrEmpty(value)) result.Add(CBORObject.True);
                    else result.Add(value);
                }
            }

            CBORObject pair = CBORObject.NewArray();
            pair.Add(name);
                pair.Add(result);
            cbor.Add(pair);
        }
#endif
        private static void SerializeAttributeInCoral(string name, List<string> values, CoralBody coral,
                                                      Dictionary<string, CBORObject> dictionary)
        {
            bool useSpace = SpaceSeparatedValueAttributes.Contains(name);
            CBORObject result;

            string nameX = CoralsKeys[name];

            if (useSpace && values.Count > 1) {
                StringBuilder sb = new StringBuilder();

                foreach (string value in values) {
                    sb.Append(value);
                    sb.Append(" ");
                }

                sb.Length = sb.Length - 1;

                result = CBORObject.FromObject(sb.ToString());
            }
            else if (values.Count == 1) {
                string value = values.First();
                result = string.IsNullOrEmpty(value) ? CBORObject.True : CBORObject.FromObject(values.First());
            }
            else {
                result = CBORObject.NewArray();
                foreach (string value in values) {
                    if (string.IsNullOrEmpty(value)) {
                        result.Add(CBORObject.True);
                    }
                    else {
                        result.Add(value);
                    }
                }
            }

            CoralLink link = new CoralLink(nameX, result);
            coral.Add(link);
        }

        private static bool IsNumber(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            foreach (char c in value) {
                if (!char.IsNumber(c)) return false;
            }
            return true;
        }

#if false
        public static string Serialize(Resource resource, IEnumerable<Option> query, bool recursive)
        {
            StringBuilder linkFormat = new StringBuilder();

            // skip hidden and empty root in recursive mode, always skip non-matching resources
            if ((!resource.Hidden && (resource.Name.Length > 0) || !recursive)
                && Matches(resource, query)) {
                linkFormat.Append("<")
                    .Append(resource.Path)
                    .Append(">");

                foreach (LinkAttribute attr in resource.LinkAttributes) {
                    linkFormat.Append(Separator);
                    attr.Serialize(linkFormat);
                }
            }

            if (recursive) {
                foreach (Resource sub in resource.GetSubResources()) {
                    string next = Serialize(sub, query, true);

                    if (next.Length > 0) {
                        if (linkFormat.Length > 3) linkFormat.Append(Delimiter);
                        linkFormat.Append(next);
                    }
                }
            }

            return linkFormat.ToString();
        }
#endif

        public static RemoteResource Deserialize(string linkFormat)
        {
            RemoteResource root = new RemoteResource(string.Empty);
            if (string.IsNullOrEmpty(linkFormat)) {
                return root;
            }

            string[] links = SplitOn(linkFormat, ',');

            foreach (string link in links) {
                string[] attributes = SplitOn(link, ';');
                if (attributes[0][0] != '<' || attributes[0][attributes[0].Length - 1] != '>') {
                    throw new ArgumentException();
                }

                RemoteResource resource = new RemoteResource(attributes[0].Substring(1, attributes[0].Length-2));

                for (int i = 1; i< attributes.Length; i++) {
                    int eq = attributes[i].IndexOf('=');
                    if (eq == -1) {
                        resource.Attributes.Add(attributes[i]);
                    }
                    else {
                        string value = attributes[i].Substring(eq + 1);
                        if (value[0] == '"') {
                            if (value[value.Length - 1] != '"') throw new ArgumentException();
                            value = value.Substring(1, value.Length - 2);
                        }
                        resource.Attributes.Add(attributes[i].Substring(0, eq), value);
                    }
                }

                root.AddSubResource(resource);
            }

            return root;
        }

#if false  // Work is dead?

        /// <summary>
        /// Parse a CBOR encoded link format structure
        /// </summary>
        /// <param name="linkFormat">link data</param>
        /// <returns>remote resource</returns>
        public static RemoteResource DeserializeCbor(byte[] linkFormat)
        {
            return DeserializeCbor(CBORObject.DecodeFromBytes(linkFormat));
        }

        /// <summary>
        /// Parse a JSON encoded link format structure
        /// </summary>
        /// <param name="linkFormat">link data</param>
        /// <returns>remote resource</returns>
        public static RemoteResource DeserializeJson(string linkFormat)
        {
            return DeserializeCbor(CBORObject.FromJSONString(linkFormat));
        }

        private static RemoteResource DeserializeCbor(CBORObject cbor)
        { 
            if (cbor.Type != CBORType.Array) throw new ArgumentException();

            RemoteResource root = new RemoteResource(string.Empty);

            for (int i = 0; i < cbor.Count; i++) {
                string href;
                if (cbor[i].ContainsKey("href")) {
                    href = cbor[i]["href"].AsString();
                }
                else {
                    href = cbor[i][CBORObject.FromObject(1)].AsString();
                }

                RemoteResource child = new RemoteResource(href);

                foreach (CBORObject key in cbor[i].Keys) {
                    string keyName;
                    if (key.Type == CBORType.Number) {
                        keyName = null;
                        foreach (KeyValuePair<string, CBORObject> kvp  in CborAttributeKeys) {
                            if (key.Equals(kvp.Value)) {
                                keyName = kvp.Key;
                                break;
                            }
                        }
                        if (keyName == null) throw new ArgumentException("Invalid numeric key");
                    }
                    else if (key.Type == CBORType.TextString) {
                        keyName = key.AsString();
                    }
                    else {
                        throw new ArgumentException("Unexpected key type found");
                    }

                    if (keyName == "href") continue;

                    CBORObject value = cbor[i][key];

                    if (value.Type == CBORType.TextString) {
                        child.Attributes.Add(keyName, value.AsString());
                    }
                    else if (value.Type == CBORType.Boolean) {
                        child.Attributes.Add(keyName);
                    }
                    else if (value.Type == CBORType.Array) {
                        for (int i1 = 0; i1 < value.Count; i1++) {
                            if (value[i1].Type == CBORType.TextString) {
                                child.Attributes.Add(keyName, value[i1].AsString());
                            }
                            else if (value[i1].Type == CBORType.Boolean) {
                                if (value[i1].AsBoolean() != true) throw new ArgumentException("false unexpectedly found");
                                child.Attributes.Add(keyName);
                            }
                            else throw new ArgumentException("Unexpected value type found");
                        }
                    }
                    else throw new ArgumentException("Unexpected value type found");
                }

                root.AddSubResource(child);
            }
            

            return root;
        }
#endif

#if false
        private static LinkAttribute ParseAttribute(Scanner scanner)
        {
            string name = scanner.Find(WordRegex);
            if (name == null) return null;
            else {
                object value = null;
                // check for name-value-pair
                if (scanner.Find(new Regex("="), 1) == null)
                    // flag attribute
                    value = true;
                else {
                    string s = null;
                    if ((s = scanner.Find(QuotedString)) != null)
                        // trim " "
                        value = s.Substring(1, s.Length - 2);
                    else if ((s = scanner.Find(Cardinal)) != null) value = int.Parse(s);
                    // TODO what if both pattern failed?
                }
                return new LinkAttribute(name, value);
            }
        }
#endif

#if false
        private static bool Matches(Resource resource, IEnumerable<Option> query)
        {
            if (resource == null) return false;

            if (query == null) return true;

            foreach (Option q in query) {
                string s = q.StringValue;
                int delim = s.IndexOf('=');
                if (delim == -1) {
                    // flag attribute
                    if (resource.GetAttributes(s).Count > 0) return true;
                }
                else {
                    string attrName = s.Substring(0, delim);
                    string expected = s.Substring(delim + 1);

                    if (attrName.Equals(LinkFormat.Link)) {
                        if (expected.EndsWith("*")) return resource.Path.StartsWith(expected.Substring(0, expected.Length - 1));
                        else return resource.Path.Equals(expected);
                    }

                    foreach (LinkAttribute attr in resource.GetAttributes(attrName)) {
                        string actual = attr.Value.ToString();

                        // get prefix length according to "*"
                        int prefixLength = expected.IndexOf('*');
                        if (prefixLength >= 0 && prefixLength < actual.Length) {
                            // reduce to prefixes
                            expected = expected.Substring(0, prefixLength);
                            actual = actual.Substring(0, prefixLength);
                        }

                        // handle case like rt=[Type1 Type2]
                        if (actual.IndexOf(' ') > -1) {
                            foreach (string part in actual.Split(' ')) {
                                if (part.Equals(expected)) return true;
                            }
                        }

                        if (expected.Equals(actual)) return true;
                    }
                }
            }

            return false;
        }
#endif

        private static bool Matches(IResource resource, List<string> query)
        {
            if (resource == null) return false;
            if (query == null) return true;

            using (IEnumerator<string> ie = query.GetEnumerator()) {
                if (!ie.MoveNext()) return true;

                ResourceAttributes attributes = resource.Attributes;
                string path = resource.Path + resource.Name;

                do {
                    string s = ie.Current;

                    int delim = s.IndexOf('=');
                    if (delim == -1) {
                        // flag attribute
                        if (attributes.Contains(s)) return true;
                    }
                    else {
                        string attrName = s.Substring(0, delim);
                        string expected = s.Substring(delim + 1);

                        if (attrName.Equals(LinkFormat.Link)) {
                            if (expected.EndsWith("*")) return path.StartsWith(expected.Substring(0, expected.Length - 1));
                            else return path.Equals(expected);
                        }
                        else if (attributes.Contains(attrName)) {
                            // lookup attribute value
                            foreach (string value in attributes.GetValues(attrName)) {
                                string actual = value;
                                // get prefix length according to "*"
                                int prefixLength = expected.IndexOf('*');
                                if (prefixLength >= 0 && prefixLength <= actual.Length) {
                                    // reduce to prefixes
                                    expected = expected.Substring(0, prefixLength);
                                    actual = actual.Substring(0, prefixLength);
                                }

                                // handle case like rt=[Type1 Type2]
                                if (actual.IndexOf(' ') > -1) {
                                    foreach (string part in actual.Split(' ')) {
                                        if (part.Equals(expected)) return true;
                                    }
                                }

                                if (expected.Equals(actual)) return true;
                            }
                        }
                    }
                } while (ie.MoveNext());
            }

            return false;
        }

        internal static bool AddAttribute(ICollection<LinkAttribute> attributes, LinkAttribute attrToAdd)
        {
            if (IsSingle(attrToAdd.Name)) {
                foreach (LinkAttribute attr in attributes) {
                    if (attr.Name.Equals(attrToAdd.Name)) {
                        if (_Log.IsDebugEnabled) _Log.Debug("Found existing singleton attribute: " + attr.Name);
                        return false;
                    }
                }
            }

            // special rules
            if (attrToAdd.Name.Equals(ContentType) && attrToAdd.IntValue < 0) return false;
            if (attrToAdd.Name.Equals(MaxSizeEstimate) && attrToAdd.IntValue < 0) return false;

            attributes.Add(attrToAdd);
            return true;
        }

        private static bool IsSingle(string name)
        {
            return SingleOccurenceAttributes.Contains(name);
        }

        private static string quoteChars = "'\"";

        private static string[] SplitOn(string input, char splitChar)
        {
            bool escape = false;
            char inString = (char) 0;
            List<string> output = new List<string>();
            int startChar = 0;
            

            for (int i = 0; i < input.Length; i++) {
                char c = input[i];
                if (c == '\\') {
                    escape = !escape;
                    continue;
                }

                if (c == splitChar) {
                    if (inString == 0) {
                        output.Add(input.Substring(startChar, i - startChar));
                        startChar = i + 1;
                    }
                }
                else if (quoteChars.IndexOf(c) > -1 && !escape) {
                    if (c == inString) inString = (char) 0;
                    else if (inString == 0) inString = c;
                }
            }

            if (inString != 0) throw new ArgumentException();
            if (startChar < input.Length) output.Add(input.Substring(startChar));

            return output.ToArray();
        }
    }
}
