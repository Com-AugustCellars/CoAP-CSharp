/*
 * Copyright (c) 2011-2015, Longxiang He <helongxiang@smeshlink.com>,
 * SmeshLink Technology Co.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY.
 * 
 * This file is part of the CoAP.NET, a CoAP framework in C#.
 * Please see README for more information.
 */

using System;
// ReSharper disable InconsistentNaming

namespace Com.AugustCellars.CoAP
{
    /// <summary>
    /// This class describes the CoAP Code Registry as defined in 
    /// draft-ietf-core-coap-08, section 11.1
    /// </summary>
    public class Code
    {
        /// <summary>
        /// Undefined
        /// </summary>
        public const Int32 Empty = 0;

        /// <summary>
        /// Indicate client request was successfully processed.
        /// </summary>
        /// 
        public const int SuccessCode = 2;

        /// <summary>
        /// Indicate a client request had an error of some type.
        /// </summary>
        public const int ClientErrorCode = 4;

        /// <summary>
        /// Indicate a server error while processing request
        /// </summary>
        public const int ServerErrorCode = 5;

        #region Method Codes

        /// <summary>
        /// The GET method
        /// </summary>
        public const int GET = 1;

        /// <summary>
        /// The POST method
        /// </summary>
        public const int POST = 2;

        /// <summary>
        /// The PUT method
        /// </summary>
        public const int PUT = 3;

        /// <summary>
        /// The DELETE method
        /// </summary>
        public const int DELETE = 4;

        /// <summary>
        /// The FETCH method [RFC8132]
        /// </summary>
        public const int FETCH = 5;

        /// <summary>
        /// The PATCH method [RFC8132]
        /// </summary>
        public const int PATCH = 6;

        /// <summary>
        /// The iPATCH method [RFC8132]
        /// </summary>
        public const int iPATCH = 7;

        #endregion

        #region Response Codes

        /// <summary>
        /// 2.01 Created
        /// </summary>
        public const int Created = 65;

        /// <summary>
        /// 2.02 Deleted
        /// </summary>
        public const int Deleted = 66;

        /// <summary>
        /// 2.03 Valid 
        /// </summary>
        public const int Valid = 67;

        /// <summary>
        /// 2.04 Changed
        /// </summary>
        public const int Changed = 68;

        /// <summary>
        /// 2.05 Content
        /// </summary>
        public const int Content = 69;

        /// <summary>
        /// 2.31 Continue
        /// </summary>
        public const int Continue = 95;

        /// <summary>
        /// 4.00 Bad Request
        /// </summary>
        public const int BadRequest = 128;

        /// <summary>
        /// 4.01 Unauthorized
        /// </summary>
        public const int Unauthorized = 129;

        /// <summary>
        /// 4.02 Bad Option
        /// </summary>
        public const int BadOption = 130;

        /// <summary>
        /// 4.03 Forbidden
        /// </summary>
        public const int Forbidden = 131;

        /// <summary>
        /// 4.04 Not Found
        /// </summary>
        public const int NotFound = 132;

        /// <summary>
        /// 4.05 Method Not Allowed
        /// </summary>
        public const int MethodNotAllowed = 133;

        /// <summary>
        /// 4.06 Not Acceptable
        /// </summary>
        public const int NotAcceptable = 134;

        /// <summary>
        /// 4.08 Request Entity Incomplete [RFC7959]
        /// </summary>
        public const int RequestEntityIncomplete = 136;

        /// <summary>
        /// 4.09 Conflict [RFC8132]
        /// </summary>
        public const int Conflict = 137;

        /// <summary>
        /// 4.12 Precondition Failed
        /// </summary>
        public const int PreconditionFailed = 140;

        /// <summary>
        /// 4.13 Request Entity Too Large
        /// </summary>
        public const int RequestEntityTooLarge = 141;

        /// <summary>
        /// 4.15 Unsupported Media Type
        /// </summary>
        public const int UnsupportedMediaType = 143;

        /// <summary>
        /// 4.22 UnprocessableEntity
        /// </summary>
        public const int UnprocessableEntity = 150;

        /// <summary>
        /// 5.00 Internal Server Error
        /// </summary>
        public const int InternalServerError = 160;

        /// <summary>
        /// 5.01 Not Implemented
        /// </summary>
        public const int NotImplemented = 161;

        /// <summary>
        /// 5.02 Bad Gateway
        /// </summary>
        public const int BadGateway = 162;

        /// <summary>
        /// 5.03 Service Unavailable 
        /// </summary>
        public const int ServiceUnavailable = 163;

        /// <summary>
        /// 5.04 Gateway Timeout
        /// </summary>
        public const int GatewayTimeout = 164;

        /// <summary>
        /// 5.05 Proxying Not Supported
        /// </summary>
        public const int ProxyingNotSupported = 165;

        #endregion

        /// <summary>
        /// Return the class of code from the message.
        /// </summary>
        /// <param name="code">code to be checked</param>
        /// <returns>class in range of 0-7</returns>
        public static int GetResponseClass(int code)
        {
            return (code >> 5) & 0x7;
        }

        /// <summary>
        /// Checks whether a code indicates a request
        /// </summary>
        /// <param name="code">The code to be checked</param>
        /// <returns>True iff the code indicates a request</returns>
        public static Boolean IsRequest(int code)
        {
            return (code >= 1) && (code <= 31);
        }

        /// <summary>
        /// Checks whether a code indicates a response
        /// </summary>
        /// <param name="code">The code to be checked</param>
        /// <returns>True iff the code indicates a response</returns>
        public static Boolean IsResponse(int code)
        {
            return (code >= 64) && (code <= 191);
        }

        /// <summary>
        /// Checks whether a code represents a success code.
        /// </summary>
        public static Boolean IsSuccess(int code)
        {
            return code >= 64 && code < 96;
        }

        /// <summary>
        /// Checks whether a code is valid
        /// </summary>
        /// <param name="code">The code to be checked</param>
        /// <returns>True iff the code is valid</returns>
        public static Boolean IsValid(int code)
        {
            // allow unknown custom codes
            return (code >= 0) && (code <= 255);
        }

        /// <summary>
        /// Returns a string representation of the code
        /// </summary>
        /// <param name="code">The code to be described</param>
        /// <returns>A string describing the code</returns>
        public static string ToString(int code)
        {
            switch (code) {
                case Empty:
                    return "Empty Message";
                case GET:
                    return "GET";
                case POST:
                    return "POST";
                case PUT:
                    return "PUT";
                case DELETE:
                    return "DELETE";
                case FETCH:
                    return "FETCH";
                case PATCH: return "PATCH";
                case iPATCH: return "iPATCH";
                case Created:
                    return "2.01 Created";
                case Deleted:
                    return "2.02 Deleted";
                case Valid:
                    return "2.03 Valid";
                case Changed:
                    return "2.04 Changed";
                case Content:
                    return "2.05 Content";
                case Continue: return "2.31 Continue";
                case BadRequest:
                    return "4.00 Bad Request";
                case Unauthorized:
                    return "4.01 Unauthorized";
                case BadOption:
                    return "4.02 Bad Option";
                case Forbidden:
                    return "4.03 Forbidden";
                case NotFound:
                    return "4.04 Not Found";
                case MethodNotAllowed:
                    return "4.05 Method Not Allowed";
                case NotAcceptable:
                    return "4.06 Not Acceptable";
                case RequestEntityIncomplete:
                    return "4.08 Request Entity Incomplete";
                case Conflict: return "4.09 Conflict";
                case PreconditionFailed:
                    return "4.12 Precondition Failed";
                case RequestEntityTooLarge:
                    return "4.13 Request Entity Too Large";
                case UnsupportedMediaType:
                    return "4.15 Unsupported Media Type";
                case UnprocessableEntity: return "4.22 Unprocessable Entity";
                case InternalServerError:
                    return "5.00 Internal Server Error";
                case NotImplemented:
                    return "5.01 Not Implemented";
                case BadGateway:
                    return "5.02 Bad Gateway";
                case ServiceUnavailable:
                    return "5.03 Service Unavailable";
                case GatewayTimeout:
                    return "5.04 Gateway Timeout";
                case ProxyingNotSupported:
                    return "5.05 Proxying Not Supported";
                default:
                    break;
            }

            if (IsValid(code)) {
                if (IsRequest(code)) {
                    return $"Unknown Request [code {code}]";
                }
                else if (IsResponse(code)) {
                    return $"Unknown Response [code {code}]";
                }
                else {
                    return $"Reserved [code {code}]";
                }
            }
            else {
                return $"Invalid Message [code {code}]";
            }
        }
    }

    /// <summary>
    /// Methods of request
    /// </summary>
    public enum Method
    {
        /// <summary>
        /// GET method
        /// </summary>
        GET = 1,

        /// <summary>
        /// POST method
        /// </summary>
        POST = 2,

        /// <summary>
        /// PUT method
        /// </summary>
        PUT = 3,

        /// <summary>
        /// DELETE method
        /// </summary>
        DELETE = 4,

        /// <summary>
        /// FETCH method [RFC8132]
        /// </summary>
        FETCH = 5,

        /// <summary>
        /// PATCH method [RFC8132]
        /// </summary>
        PATCH = 6,

        /// <summary>
        /// iPATCH method [RFC8132]
        /// </summary>
        iPATCH = 7
    }

    /// <summary>
    /// Response status codes.
    /// </summary>
    public enum StatusCode
    {
        /// <summary>
        /// 2.01 Created
        /// </summary>
        Created = 65,

        /// <summary>
        /// 2.02 Deleted
        /// </summary>
        Deleted = 66,

        /// <summary>
        /// 2.03 Valid 
        /// </summary>
        Valid = 67,

        /// <summary>
        /// 2.04 Changed
        /// </summary>
        Changed = 68,

        /// <summary>
        /// 2.05 Content
        /// </summary>
        Content = 69,

        /// <summary>
        /// 2.31 Continue
        /// </summary>
        Continue = 95,

        /// <summary>
        /// 4.00 Bad Request
        /// </summary>
        BadRequest = 128,

        /// <summary>
        /// 4.01 Unauthorized
        /// </summary>
        Unauthorized = 129,

        /// <summary>
        /// 4.02 Bad Option
        /// </summary>
        BadOption = 130,

        /// <summary>
        /// 4.03 Forbidden
        /// </summary>
        Forbidden = 131,

        /// <summary>
        /// 4.04 Not Found
        /// </summary>
        NotFound = 132,

        /// <summary>
        /// 4.05 Method Not Allowed
        /// </summary>
        MethodNotAllowed = 133,

        /// <summary>
        /// 4.06 Not Acceptable
        /// </summary>
        NotAcceptable = 134,

        /// <summary>
        /// 4.08 Request Entity Incomplete [RFC7959]
        /// </summary>
        RequestEntityIncomplete = 136,

        /// <summary>
        /// 4.09 Conflict [RFC8132]
        /// </summary>
        Conflict = 137,

        /// <summary>
        /// 4.12 Precondition Failed
        /// </summary>
        PreconditionFailed = 140,

        /// <summary>
        /// 4.13 Request Entity Too Large
        /// </summary>
        RequestEntityTooLarge = 141,

        /// <summary>
        /// 4.15 Unsupported Media Type
        /// </summary>
        UnsupportedMediaType = 143,

        /// <summary>
        /// 4.22 Unprocessable Entity [RC8132]
        /// </summary>
        UnprocessableEntity = 150,

        /// <summary>
        /// 5.00 Internal Server Error
        /// </summary>
        InternalServerError = 160,

        /// <summary>
        /// 5.01 Not Implemented
        /// </summary>
        NotImplemented = 161,

        /// <summary>
        /// 5.02 Bad Gateway
        /// </summary>
        BadGateway = 162,

        /// <summary>
        /// 5.03 Service Unavailable 
        /// </summary>
        ServiceUnavailable = 163,

        /// <summary>
        /// 5.04 Gateway Timeout
        /// </summary>
        GatewayTimeout = 164,

        /// <summary>
        /// 5.05 Proxying Not Supported
        /// </summary>
        ProxyingNotSupported = 165
    }
}
