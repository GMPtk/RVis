using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RVis.Client
{
  public sealed class UnixDomainSocketConnectionFactory
  {
    public UnixDomainSocketConnectionFactory(EndPoint endPoint) =>
      _endPoint = endPoint;

    public async ValueTask<Stream> ConnectAsync(
      SocketsHttpConnectionContext _,
      CancellationToken cancellationToken = default
      )
    {
      var socket = new Socket(
        AddressFamily.Unix,
        SocketType.Stream,
        ProtocolType.Unspecified
        );

      async ValueTask<Stream> DoConnectAsync(Socket s)
      {
        await s.ConnectAsync(_endPoint, cancellationToken).ConfigureAwait(false);
        return new NetworkStream(s, true);
      }

      try
      {
        return await DoConnectAsync(socket).ConfigureAwait(false);
      }
      catch (Exception) { }

      try
      {
        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        return await DoConnectAsync(socket).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        socket.Dispose();
        throw new HttpRequestException($"Error connecting to '{_endPoint}'.", ex);
      }
    }

    private readonly EndPoint _endPoint;
  }
}
