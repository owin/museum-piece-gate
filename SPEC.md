<html>
<head>
	<meta http-equiv="Content-Type" content="text/html;charset=utf-8" />
</head>
<body>

# OWIN — Open Web Interface for .NET, v1.0 Draft 

## Overview

This document defines a standard interface between .NET web servers and web applications. The goal of the OWIN interface is to decouple server and application, encourage the development of simple modules for .NET web development, and, by being an open standard, stimulate the open source ecosystem of .NET web development tools.

## Definition

OWIN is neither framework nor server. It is an interface through which which servers and frameworks or applications can communicate with each other. OWIN is defined by three core interfaces: `IApplication`, `IRequest`, and `IResponse`. Broadly speaking, hosts provide application objects with request objects, and application objects provide response objects back to the server. In this document, an OWIN-compatible web server is referred to as a “host”, and an object implementing `IApplication` is referred to as an “application”. How an application is provided to a host is outside the scope of this specification.

### IApplication

    public interface IApplication
    {
        IAsyncResult BeginInvoke(IRequest request, AsyncCallback callback, object state);
        IResponse EndInvoke(IAsyncResult result);
    }

Applications generate responses to requests received by a host by implementing the `IApplication` interface, which defines single asynchronous operation returning IResponse. The asynchronous operation uses the IAsyncResult pattern (see: [MSDN Asynchronous Programming Overview](http://msdn.microsoft.com/en-us/library/ms228963.aspx)). Applications should always generate a response. 

### IRequest

    public interface IRequest
    {
        string Method { get; }
        string Uri { get; }
        IDictionary<string, IEnumerable<string>> Headers { get; }

        IAsyncResult BeginReadBody(byte[] buffer, int offset, int count, AsyncCallback callback, object state);
        int EndReadBody(IAsyncResult result);

        IDictionary<string, object> Items { get; }
    }

The `Method` property is the HTTP request method string of the request (e.g., `"GET"`, `"POST"`).

The `Uri` property is the HTTP request URI string of the request, relative to the application object. See [Paths](#Paths). The value of the `Uri` property includes the query string of the request URI (e.g., “/path/and?query=string”).  

The `Headers` property is a dictionary whose items correspond to HTTP headers in the request. Keys are lower-cased header names without `':'` or whitespace. Values are `IEnumerable<string>` sequences containing the corresponding header value strings, without newlines. If a header appears in a request multiple times, the sequence value for that key will contain a number of elements corresponding to the number of times the header appears in the request, with each element being a value of a single header.

The methods `BeginReadBody` and `EndReadBody` provide an asynchronous operation which reads body data of the request into a destination buffer. The `EndReadBody` method returns the number of bytes read. Hosts must signal the end of the request body by returning 0 from `EndReadBody`.

The `Items` property is a bag in which the host, application, or user can store arbitrary data associated with the request. Hosts should provide the following keys in `Items`:

- `owin.BasePath` – The portion of the request URI’s path corresponding to the “root” of the application object. See [Paths](#Paths).
- `owin.ServerName`, `owin.ServerPort` – Hosts should provide values can be used to reconstruct the full URL of the request in absence of the HTTP `Host` header of the request.
- `owin.UrlScheme` – `"http"` or `"https"`
- `owin.RemoteEndPoint` — A `System.Net.IPEndPoint` representing the connected client.

### IResponse

    public interface IResponse
    {
        string Status { get; }
        IDictionary<string, IEnumerable<string>> Headers { get; }
        IEnumerable<object> GetBody();
    }

The `Status` property is a string containing the integer status of the response followed by a space and a reason phrase without a newline (e.g., `"200 OK"`). All characters in the status string provided by an application should be within the ASCII codepage.

The `Headers` property is a dictionary representing the headers to be sent with the request. Keys must be header names without `':'` or whitespace. Values must be `IEnumerable<string>` sequences containing the corresponding header value strings, without newlines. If the sequence value for a header name contains multiple elements, the host should write a header line with that name once for each value in the sequence. All characters in header name and value strings should be within the ASCII codepage.

The `GetBody` method returns an enumerable which represents the body data. Each element in the enumerable must be of one of the following types:

- `string`
- `byte[]` 
- `ArraySegment<byte>`
- `FileInfo`

[TODO] Async primitives?

Hosts must write `string` objects to the underlying transport as UTF-8 data, both `byte[]` and `ArraySegment<byte>` as raw data. `FileInfo` must cause the host to write the named file to the underlying transport. After all of the items have been enumerated or if an error occurs during enumeration, the host must call `Dispose` on the enumerator.

<a name="Paths"></a>
### Paths

Some hosts may have the ability to map application objects to some base path. For example, a host may have an application object configured to respond to requests beginning with `"/my-app"`, in which case it must set the value of `"owin.BasePath"` in `IRequest.Items` to `"/my-app"`. If this host received a request for `"/my-app/foo"`, the `Uri` property of the `IRequest` object provided to the application must be `"/foo"`. The value of `"owin.BasePath"` may be an empty string and must not end with a trailing slash; the value of the `URI` property must start with a slash.

### Error Handling

Hosts may throw exceptions in the following places:

- The `IRequest.BeginReadBody` method
- The `IRequest.EndReadBody` method

Hosts may throw exceptions from either of these methods to indicate that the client has closed or dropped the connection. [TODO: Maybe these should never throw exceptions–the host should shield applications from that and instead just provide zero bytes?]

Applications may throw exceptions in the following places:

- The `IApplication.BeginInvoke` method
- The `IApplication.EndInvoke` method
- The `IResponse.GetBody` method
- The `GetEnumerator` method of the `IEnumerable<object>` returned by `IResponse.GetBody`
- The `MoveNext` method of the enumerator returned by the `GetEnumerator` method of the `IEnumerable<object>` returned by `IResponse.GetBody`
- The `Current` property of the enumerator returned by the `GetEnumerator` method of the `IEnumerable<object>` returned by `IResponse.GetBody`

Host implementations should strive to write response data to the network as “late” as possible, so as to be able to handle as many errors from the application as possible and cleanly send the client a 500-level response. Generally, this means invoking the application and enumerating the first object from its response body, if any, before writing any header or body data to the network. If an error occurs before data is written to the network, the server should provide a 500-level response. If an error occurs enumerating subsequent items from the response body enumerable, the host may append a textual description of the error to the response data which it has already sent and close the connection.

If an uncaught error occurs during the `Invoke` operation of an application, the application must invoke the `AsyncCallback` provided by the host and throw an exception from `EndInvoke`, effectively propagating the exception back to the host.



</body>
</html>