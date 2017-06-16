using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace Hazel.Udp
{
    /// <summary>
    ///     Listens for new UDP connections and creates UdpConnections for them.
    /// </summary>
    /// <inheritdoc />
    public class UdpConnectionListener : NetworkConnectionListener
    {
        /// <summary>
        ///     The socket listening for connections.
        /// </summary>
        Socket listener;

        /// <summary>
        ///     Buffer to store incoming data in.
        /// </summary>
        byte[] dataBuffer = new byte[ushort.MaxValue];

        /// <summary>
        ///     The connections we currently hold
        /// </summary>
        Dictionary<EndPoint, UdpServerConnection> connections = new Dictionary<EndPoint, UdpServerConnection>();

        /// <summary>
        ///     Creates a new UdpConnectionListener for the given <see cref="IPAddress"/>, port and <see cref="IPMode"/>.
        /// </summary>
        /// <param name="IPAddress">The IPAddress to listen on.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="mode">The <see cref="IPMode"/> to listen with.</param>
        [Obsolete("Temporary constructor in beta only, use NetworkEndPoint constructor instead.")]
        public UdpConnectionListener(IPAddress IPAddress, int port, IPMode mode = IPMode.IPv4)
            : this (new NetworkEndPoint(IPAddress, port, mode))
        {

        }

        /// <summary>
        ///     Creates a new UdpConnectionListener for the given <see cref="IPAddress"/>, port and <see cref="IPMode"/>.
        /// </summary>
        /// <param name="endPoint">The endpoint to listen on.</param>
        public UdpConnectionListener(NetworkEndPoint endPoint)
        {
            this.EndPoint = endPoint.EndPoint;
            this.IPMode = endPoint.IPMode;

            if (endPoint.IPMode == IPMode.IPv4)
                this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            else
            {
                if (!Socket.OSSupportsIPv6)
                    throw new HazelException("IPV6 not supported!");

                this.listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                this.listener.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
            }
        }

        /// <inheritdoc />
        public override void Start()
        {
            try
            {
                lock (listener)
                    listener.Bind(EndPoint);
            }
            catch (SocketException e)
            {
                throw new HazelException("Could not start listening as a SocketException occured", e);
            }

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.SetBuffer(dataBuffer, 0, dataBuffer.Length);
            args.Completed += ReadCallback;

            StartListeningForData(args);
        }

        /// <summary>
        ///     Instructs the listener to begin listening.
        /// </summary>
        void StartListeningForData(SocketAsyncEventArgs args)
        {
            args.RemoteEndPoint = EndPoint;

            try
            {
                lock (listener)
                    listener.ReceiveFromAsync(args);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException)
            {
                //Client no longer reachable, pretend it didn't happen
                //TODO possibly able to disconnect client
                
                //However, this thread suggests the IP is not passed out from WinSoc so maybe not possible
                //http://stackoverflow.com/questions/2576926/python-socket-error-on-udp-data-receive-10054
                
                //Nevertheless this has now all changed from BeginReveiveFrom to ReceiveFromAsync so maybe?
                StartListeningForData(args);
                return;
            }
        }

        /// <summary>
        ///     Called when data has been received by the listener.
        /// </summary>
        void ReadCallback(object sender, SocketAsyncEventArgs args)
        {
            //Exit if no bytes read, we've closed.
            if (args.BytesTransferred == 0 || args.SocketError != SocketError.Success)
                return;

            EndPoint remoteEndPoint = args.RemoteEndPoint;

            //Copy to new buffer
            byte[] buffer = new byte[args.BytesTransferred];
            Buffer.BlockCopy(args.Buffer, 0, buffer, 0, args.BytesTransferred);

            //Begin receiving again
            StartListeningForData(args);

            bool aware;
            UdpServerConnection connection;
            lock (connections)
            {
                aware = connections.ContainsKey(remoteEndPoint);

                //If we're aware of this connection use the one already
                if (aware)
                    connection = connections[remoteEndPoint];
                
                //If this is a new client then connect with them!
                else
                {
                    //Check for malformed connection attempts
                    if (buffer[0] != (byte)UdpSendOption.Hello)
                        return;

                    connection = new UdpServerConnection(this, remoteEndPoint, IPMode);
                    connections.Add(remoteEndPoint, connection);
                }
            }

            //Inform the connection of the buffer (new connections need to send an ack back to client)
            connection.HandleReceive(buffer);
            
            //If it's a new connection invoke the NewConnection event.
            if (!aware)
            {
                byte[] dataBuffer = new byte[buffer.Length - 1];
                Buffer.BlockCopy(buffer, 1, dataBuffer, 0, buffer.Length - 1);
                InvokeNewConnection(dataBuffer, connection);
            }
        }

        /// <summary>
        ///     Sends data from the listener socket.
        /// </summary>
        /// <param name="bytes">The bytes to send.</param>
        /// <param name="endPoint">The endpoint to send to.</param>
        internal void SendData(byte[] bytes, EndPoint endPoint)
        {
            try
            {
                lock (listener)
                {
                    listener.SendTo(
                        bytes,
                        endPoint
                    );
                }
            }
            catch (SocketException e)
            {
                throw new HazelException("Could not send data as a SocketException occured.", e);
            }
            catch (ObjectDisposedException)
            {
                //Keep alive timer probably ran, ignore
                return;
            }
        }

        /// <summary>
        ///     Removes a virtual connection from the list.
        /// </summary>
        /// <param name="endPoint">The endpoint of the virtual connection.</param>
        internal void RemoveConnectionTo(EndPoint endPoint)
        {
            lock (connections)
                connections.Remove(endPoint);
        }

        /// <inheritdoc />
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
