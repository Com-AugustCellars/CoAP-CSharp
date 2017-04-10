# CoAP.NET - A CoAP framework in C#

[![Build Status](https://api.travis-ci.org/jimsch/CoAP-CSharp.png)](https://travis-ci.org/jimsch/CoAP-CSharp)

The Constrained Application Protocol (CoAP) (https://datatracker.ietf.org/doc/draft-ietf-core-coap/)
is a RESTful web transfer protocol for resource-constrained networks and nodes.
CoAP.NET is an implementation in C# providing CoAP-based services to .NET applications. 
Reviews and suggestions would be appreciated.

## Copyright

Copyright (c) 2011-2015, Longxiang He <longxianghe@gmail.com>,
SmeshLink Technology Co.

## Content

- [Quick Start](#quick-start)
- [Build](#build)
- [License](#license)
- [Acknowledgements](#acknowledgements)

## Quick Start

CoAP sessions are considered as request-response pair.

### CoAP Client

Access remote CoAP resources by issuing a **[Request](CoAP.NET/Request.cs)**
and receive its **[Response](CoAP.NET/Request.cs)**(s).

```csharp
  // new a GET request
  Request request = new Request(Method.GET);
  request.URI = new Uri("coap://[::1]/hello-world");
  request.Send();
  
  // wait for one response
  Response response = request.WaitForResponse();
```

There are 4 types of request: GET, POST, PUT, DELETE, defined as
<code>Method.GET</code>, <code>Method.POST</code>, <code>Method.PUT</code>,
<code>Method.DELETE</code>.

Responses can be received in two ways. By calling <code>request.WaitForResponse()</code>
a response will be received synchronously, which means it will 
block until timeout or a response is arrived. If more responses
are expected, call <code>WaitForResponse()</code> again.

To receive responses asynchronously, register a event handler to
the event <code>request.Respond</code> before executing.

> #### Parsing Link Format
> Use <code>LinkFormat.Parse(String)</code> to parse a link-format
  response. The returned enumeration of <code>WebLink</code>
  contains all resources stated in the given link-format string.
> ```csharp
  Request request = new Request(Method.GET);
  request.URI = new Uri("coap://[::1]/.well-known/core");
  request.Send();
  Response response = request.WaitForResponse();
  IEnumerable<WebLink> links = LinkFormat.Parse(response.PayloadString);
  ```

See [CoAP Example Client](CoAP.Client) for more.

### CoAP Server

A new CoAP server can be easily built with help of the class
[**CoapServer**](CoAP.NET/Server/CoapServer.cs)

```csharp
  static void Main(String[] args)
  {
    CoapServer server = new CoapServer();
    
    server.Add(new HelloWorldResource("hello"));
    
    server.Start();
    
    Console.ReadKey();
  }
```

See [CoAP Example Server](CoAP.Server) for more.

### CoAP Resource

CoAP resources are classes that can be accessed by a URI via CoAP.
In CoAP.NET, a resource is defined as a subclass of [**Resource**](CoAP.NET/Server/Resources/Resource.cs).
By overriding methods <code>DoGet</code>, <code>DoPost</code>,
<code>DoPut</code> or <code>DoDelete</code>, one resource accepts
GET, POST, PUT or DELETE requests.

The following code gives an example of HelloWorldResource, which
can be visited by sending a GET request to "/hello-world", and
respones a plain string in code "2.05 Content".

```csharp
  class HelloWorldResource : Resource
  {
      public HelloWorldResource()
          : base("hello-world")
      {
          Attributes.Title = "GET a friendly greeting!";
      }

      protected override void DoGet(CoapExchange exchange)
      {
          exchange.Respond("Hello World from CoAP.NET!");
      }
  }
  
  class Server
  {
      static void Main(String[] args)
      {
          CoapServer server = new CoapServer();
          server.Add(new HelloWorldResource());
          server.Start();
      }
  }
```

See [CoAP Example Server](CoAP.Server) for more.


## License

See [LICENSE](LICENSE) for more info.

## Acknowledgements

This is a copy o the CoAP.NET project hosted at (https://http://coap.codeplex.com/).
As this project does not seem to be maintained anymore, and I am doing active updates to it, I have made a local copy that things are going to move forward on.

Current projects are:

- OSCoAP[https://datatracker.ietf.org/doc/draft-ietf-core-object-security/] - Add an implemenation of message based security
- EDHOC[https://datatracker.ietf.org/doc/draft-selander-ace-cose-ecdhe/] - Ephemeral Diffie-Hellman over COSE - a key establishment protocol
- DTLS - Support DTLS for transport
- TLS/TCP[https://datatracker.ietf.org/doc/draft-ietf-core-coap-tcp-tls/] - Support TCP and TLS over TCP for transport
- Resource Directory[https://datatracker.ietf.org/doc/draft-ietf-core-resource-directory/] - Resource directory resources
- PubSub[https://datatracker.ietf.org/doc/draft-ietf-core-coap-pubsub/] - Publish-Subscribe Broker
- AAA[https://datatracker.ietf.org/doc/draft-ietf-ace-oauth-authz/] - Authentication and authoriztion protocol work

CoAP.NET is based on [**Californium**](https://github.com/mkovatsc/Californium),
a CoAP framework in Java by Matthias Kovatsch, Dominique Im Obersteg,
and Daniel Pauli, ETH Zurich. See <http://people.inf.ethz.ch/mkovatsc/californium.php>.
Thanks to the authors and their great job.
