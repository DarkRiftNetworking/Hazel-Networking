using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace Hazel.Tcp
{
    /// <summary>
    ///     Listens for new TCP connections and creates TCPConnections for them.
    /// </summary>
    /// <inheritdoc />
    public sealed class TcpConnectionListener : NetworkConnectionListener
    {
        /// <summary>
        ///     The socket listening for connections.
        /// </summary>
        Socket listener;

        /// <summary>
        ///     Creates a new TcpConnectionListener for the given <see cref="IPAddress"/>, port and <see cref="IPMode"/>.
        /// </summary>
        /// <param name="IPAddress">The IPAddress to listen on.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="mode">The <see cref="IPMode"/> to listen with.</param>
        [Obsolete("Temporary constructor in beta only, use NetworkEndPoint constructor instead.")]
        public TcpConnectionListener(IPAddress IPAddress, int port, IPMode mode = IPMode.IPv4)
            : this (new NetworkEndPoint(IPAddress, port, mode))
        {

        }

        /// <summary>
        ///     Creates a new TcpConnectionListener for the given <see cref="IPAddress"/>, port and <see cref="IPMode"/>.
        /// </summary>
        /// <param name="endPoint">The end point to listen on.</param>
        public TcpConnectionListener(NetworkEndPoint endPoint)
        {
            this.EndPoint = endPoint.EndPoint;
            this.IPMode = endPoint.IPMode;

            if (endPoint.IPMode == IPMode.IPv4)
                this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            else
            {
                if (!Socket.OSSupportsIPv6)
                    throw new HazelException("IPV6 not supported!");

                this.listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                this.listener.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
            }
        }

        /// <inheritdoc />
        public override void Start()
        {
            try
            {
                lock (listener)
                {
                    listener.Bind(EndPoint);
                    listener.Listen(1000);

                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.Completed += AcceptConnection;
                    listener.AcceptAsync(args);
                }
            }
            catch (SocketException e)
            {
                throw new HazelException("Could not start listening as a SocketException occured", e);
            }
        }

        /// <summary>
        ///     Called when a new connection has been accepted by the listener.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The async event args.</param>
        void AcceptConnection(object sender, SocketAsyncEventArgs args)
        {
            lock (listener)
            {
                //Sort the event out
                TcpConnection tcpConnection = new TcpConnection(args.AcceptSocket);

                //Start listening for the next connection
                listener.AcceptAsync(args);

                args.Dispose();

                //Wait for handshake
                tcpConnection.StartWaitingForHandshake(
                    delegate (byte[] bytes)
                    {
                        //Invoke
                        InvokeNewConnection(bytes, tcpConnection);

                        tcpConnection.StartReceiving();
                    }
                );
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (listener)
                    listener.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
