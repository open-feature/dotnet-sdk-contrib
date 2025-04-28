using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpenFeature.Contrib.Providers.Flagd;

/// <inheritdoc/>
public class UnixDomainSocketConnectionFactory
{
    private readonly EndPoint _endPoint;

    /// <summary>
    ///     Constructor of the connection factory
    ///     <param name="endpoint">The path to the unix socket</param>
    /// </summary>
    public UnixDomainSocketConnectionFactory(EndPoint endpoint)
    {
        _endPoint = endpoint;
    }

#if NET5_0_OR_GREATER
    /// <summary>
    ///     ConnectAsync is a custom implementation of the ConnectAsync method used by the grpc client
    /// </summary>
    /// <param name="_">unused - SocketsHttpConnectionContext</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A ValueTask object representing the given</returns>
    public async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext _, CancellationToken cancellationToken = default)
    {
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

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
#endif
}
