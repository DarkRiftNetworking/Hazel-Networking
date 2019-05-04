using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Threading;

using Hazel.Tcp;
using System.Linq;

namespace Hazel.UnitTests
{
    [TestClass]
    public class TcpConnectionTests
    {
        /// <summary>
        ///     Tests the fields on TcpConnection.
        /// </summary>
        [TestMethod]
        public void TcpFieldTest()
        {
            NetworkEndPoint ep = new NetworkEndPoint(IPAddress.Loopback, 4296);

            using (TcpConnectionListener listener = new TcpConnectionListener(new NetworkEndPoint(IPAddress.Any, 4296)))
            using (TcpConnection connection = new TcpConnection(ep))
            {
                listener.Start();

                connection.Connect();

                //Connection fields
                Assert.AreEqual(ep, connection.EndPoint);

                //TcpConnection fields
                Assert.AreEqual(new IPEndPoint(IPAddress.Loopback, 4296), connection.RemoteEndPoint);
                Assert.AreEqual(1, connection.Statistics.DataBytesSent);
                Assert.AreEqual(0, connection.Statistics.DataBytesReceived);
            }
        }

        [TestMethod]
        public void TcpHandshakeTest()
        {
            using (TcpConnectionListener listener = new TcpConnectionListener(new NetworkEndPoint(IPAddress.Any, 4296, IPMode.IPv4)))
            using (TcpConnection connection = new TcpConnection(new NetworkEndPoint(IPAddress.Loopback, 4296, IPMode.IPv4)))
            {
                listener.Start();

                listener.NewConnection += delegate (object sender, NewConnectionEventArgs e)
                {
                    Assert.IsTrue(Enumerable.SequenceEqual(e.HandshakeData, new byte[] { 1, 2, 3, 4, 5, 6 }));
                };

                connection.Connect(new byte[] { 1, 2, 3, 4, 5, 6 });
            }
        }

        /// <summary>
        ///     Tests IPv4 connectivity.
        /// </summary>
        [TestMethod]
        public void TcpIPv4ConnectionTest()
        {
            using (TcpConnectionListener listener = new TcpConnectionListener(new NetworkEndPoint(IPAddress.Any, 4296, IPMode.IPv4)))
            using (TcpConnection connection = new TcpConnection(new NetworkEndPoint(IPAddress.Loopback, 4296, IPMode.IPv4)))
            {
                listener.Start();

                connection.Connect();
            }
        }

        /// <summary>
        ///     Tests dual mode connectivity.
        /// </summary>
        [TestMethod]
        public void TcpIPv6ConnectionTest()
        {
            using (TcpConnectionListener listener = new TcpConnectionListener(new NetworkEndPoint(IPAddress.IPv6Any, 4296, IPMode.IPv6)))
            {
                listener.Start();

                using (TcpConnection connection = new TcpConnection(new NetworkEndPoint(IPAddress.IPv6Loopback, 4296, IPMode.IPv6)))
                {
                    connection.Connect();
                }
            }
        }

        /// <summary>
        ///     Tests sending and receiving on the TcpConnection.
        /// </summary>
        [TestMethod]
        public void TcpServerToClientTest()
        {
            using (TcpConnectionListener listener = new TcpConnectionListener(new NetworkEndPoint(IPAddress.Any, 4296)))
            using (TcpConnection connection = new TcpConnection(new NetworkEndPoint(IPAddress.Loopback, 4296)))
            {
                TestHelper.RunServerToClientTest(listener, connection, 10, SendOption.FragmentedReliable);
            }
        }

        /// <summary>
        ///     Tests sending and receiving on the TcpConnection.
        /// </summary>
        [TestMethod]
        public void TcpClientToServerTest()
        {
            using (TcpConnectionListener listener = new TcpConnectionListener(new NetworkEndPoint(IPAddress.Any, 4296)))
            using (TcpConnection connection = new TcpConnection(new NetworkEndPoint(IPAddress.Loopback, 4296)))
            {
                TestHelper.RunClientToServerTest(listener, connection, 10, SendOption.FragmentedReliable);
            }
        }

        /// <summary>
        ///     Tests disconnection from the client.
        /// </summary>
        [TestMethod]
        public void ClientDisconnectTest()
        {
            using (TcpConnectionListener listener = new TcpConnectionListener(new NetworkEndPoint(IPAddress.Any, 4296)))
            using (TcpConnection connection = new TcpConnection(new NetworkEndPoint(IPAddress.Loopback, 4296)))
            {
                TestHelper.RunClientDisconnectTest(listener, connection);
            }
        }

        /// <summary>
        ///     Tests disconnection from the server.
        /// </summary>
        [TestMethod]
        public void ServerDisconnectTest()
        {
            using (TcpConnectionListener listener = new TcpConnectionListener(new NetworkEndPoint(IPAddress.Any, 4296)))
            using (TcpConnection connection = new TcpConnection(new NetworkEndPoint(IPAddress.Loopback, 4296)))
            {
                TestHelper.RunServerDisconnectTest(listener, connection);
            }
        }

        /// <summary>
        ///     Tests the keepalive functionality from the client,
        /// </summary>
        [TestMethod]
        public void KeepAliveClientTest()
        {
            using (TcpConnectionListener listener = new TcpConnectionListener(new NetworkEndPoint(IPAddress.Any, 4296)))
            using (TcpConnection connection = new TcpConnection(new NetworkEndPoint(IPAddress.Loopback, 4296)))
            {
                listener.Start();

                connection.Connect();
                connection.KeepAliveInterval = 100;

                System.Threading.Thread.Sleep(1050);    //Enough time for ~10 keep alive packets

                Assert.IsTrue(
                    connection.Statistics.TotalBytesSent >= 30 &&
                    connection.Statistics.TotalBytesSent <= 50,
                    "Sent: " + connection.Statistics.TotalBytesSent
                );
            }
        }

        /// <summary>
        ///     Tests the keepalive functionality from the client,
        /// </summary>
        [TestMethod]
        public void KeepAliveServerTest()
        {
            ManualResetEvent mutex = new ManualResetEvent(false);
            TcpConnection listenerConnectionToClient = null;

            using (TcpConnectionListener listener = new TcpConnectionListener(new NetworkEndPoint(IPAddress.Any, 4296)))
            using (TcpConnection connection = new TcpConnection(new NetworkEndPoint(IPAddress.Loopback, 4296)))
            {                
                listener.NewConnection += delegate (object sender, NewConnectionEventArgs args)
                {
                    listenerConnectionToClient = (TcpConnection)args.Connection;
                    listenerConnectionToClient.KeepAliveInterval = 100;

                    Thread.Sleep(1050);    //Enough time for ~10 keep alive packets

                    mutex.Set();
                };

                listener.Start();

                connection.Connect();

                mutex.WaitOne();

                Assert.IsNotNull(listenerConnectionToClient);
                Assert.IsTrue(
                    listenerConnectionToClient.Statistics.TotalBytesSent >= 30 &&
                    listenerConnectionToClient.Statistics.TotalBytesSent <= 50,
                    "Sent: " + listenerConnectionToClient.Statistics.TotalBytesSent
                );
            }
        }
    }
}
