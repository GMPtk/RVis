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

      try
      {
        await socket.ConnectAsync(_endPoint, cancellationToken).ConfigureAwait(false);
        return new NetworkStream(socket, true);
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
