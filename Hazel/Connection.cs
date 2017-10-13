﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Hazel
{
    /// <summary>
    ///     Base class for all connections.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Connection is the base class for all connections that Hazel can make. It provides common functionality and a 
    ///         standard interface to allow connections to be swapped easily.
    ///     </para>
    ///     <para>
    ///         Any class inheriting from Connection should provide the 3 standard guarantees that Hazel provides:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Thread Safe</description>
    ///             </item>
    ///             <item>
    ///                 <description>Connection Orientated</description>
    ///             </item>
    ///             <item>
    ///                 <description>Packet/Message Based</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <threadsafety static="true" instance="true"/>
    public abstract partial class Connection : IDisposable
    {
        /// <summary>
        ///     Called when a message has been received.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         DataReceived is invoked everytime a message is received from the end point of this connection, the message 
        ///         that was received can be found in the <see cref="DataReceivedEventArgs"/> alongside other information from the 
        ///         event.
        ///     </para>
        ///     <include file="DocInclude/common.xml" path="docs/item[@name='Event_Thread_Safety_Warning']/*" />
        /// </remarks>
        /// <example>
        ///     <code language="C#" source="DocInclude/TcpClientExample.cs"/>
        /// </example>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <summary>
        ///     Called when the end point disconnects or an error occurs.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Disconnected is invoked when the connection is closed due to an exception occuring or because the remote 
        ///         end point disconnected. If it was invoked due to an exception occuring then the exception is available 
        ///         in the <see cref="DisconnectedEventArgs"/> passed with the event.
        ///     </para>
        ///     <include file="DocInclude/common.xml" path="docs/item[@name='Event_Thread_Safety_Warning']/*" />
        /// </remarks>
        /// <example>
        ///     <code language="C#" source="DocInclude/TcpClientExample.cs"/>
        /// </example>
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        /// <summary>
        ///     The remote end point of this Connection.
        /// </summary>
        /// <remarks>
        ///     This is the end point that this connection is connected to (i.e. the other device). This returns an abstract 
        ///     <see cref="ConnectionEndPoint"/> which can then be cast to an appropriate end point depending on the 
        ///     connection type.
        /// </remarks>
        public ConnectionEndPoint EndPoint { get; protected set; }

        /// <summary>
        ///     The traffic statistics about this Connection.
        /// </summary>
        /// <remarks>
        ///     Contains statistics about the number of messages and bytes sent and received by this connection.
        /// </remarks>
        public ConnectionStatistics Statistics { get; protected set; }

        /// <summary>
        ///     The state of this connection.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Connections go round 4 states in their lifetime: they start as <see cref="ConnectionState.NotConnected"/> to 
        ///         indicate they have no endpoint, calling <see cref="Connect"/> takes them into 
        ///         <see cref="ConnectionState.Connecting"/>, once they have received confirmation they are connected they enter
        ///         <see cref="ConnectionState.Connected"/> and finally calling <see cref="Close"/> sets them to 
        ///         <see cref="ConnectionState.Disconnecting"/> and then the sequence repeats back to
        ///         <see cref="ConnectionState.NotConnected"/> once disconnection is complete.
        ///     </para>
        ///     <para>
        ///         Data can only be sent while in <see cref="ConnectionState.Connected"/> and all attempts to send data when
        ///         in any other state will throw an InvalidOperationException.
        ///     </para>
        ///     <para>
        ///         All implementers should be aware that when this is set to <see cref="ConnectionState.Connected"/> it will
        ///         release all threads that are blocked on <see cref="WaitOnConnect"/>.
        ///     </para>
        /// </remarks>
        public ConnectionState State
        {
            get
            {
                return state;
            }
            
            protected set
            {
                state = value;

                if (state == ConnectionState.Connected)
                    connectWaitLock.Set();
                else
                    connectWaitLock.Reset();
            }
        }
        volatile ConnectionState state;

        /// <summary>
        ///     Reset event that is triggered when the connection is marked Connected.
        /// </summary>
        ManualResetEvent connectWaitLock = new ManualResetEvent(false);

        /// <summary>
        ///     Constructor that initializes the ConnecitonStatistics object.
        /// </summary>
        /// <remarks>
        ///     This constructor initialises <see cref="Statistics"/> with empty statistics and sets <see cref="State"/> to 
        ///     <see cref="ConnectionState.NotConnected"/>.
        /// </remarks>
        protected Connection()
        {
            Statistics = new ConnectionStatistics();

            State = ConnectionState.NotConnected;
        }

        /// <summary>
        ///     Sends a number of bytes to the end point of the connection using the specified <see cref="SendOption"/>.
        /// </summary>
        /// <param name="bytes">The bytes of the message to send.</param>
        /// <param name="sendOption">The option specifying how the message should be sent.</param>
        /// <remarks>
        ///     <include file="DocInclude/common.xml" path="docs/item[@name='Connection_SendBytes_General']/*" />
        ///     <para>
        ///         The sendOptions parameter is only a request to use those options and the actual method used to send the
        ///         data is up to the implementation. There are circumstances where this parameter may be ignored but in 
        ///         general any implementer should aim to always follow the user's request.
        ///     </para>
        /// </remarks>
        public abstract void SendBytes(byte[] bytes, SendOption sendOption = SendOption.None);

        /// <summary>
        ///     Connects the connection to a server and begins listening.
        /// </summary>
        /// <param name="bytes">The bytes of data to send in the handshake.</param>
        /// <param name="timeout">The number of milliseconds to wait before giving up on the connect attempt.</param>
        /// <remarks>
        ///     Calling Connect makes the connection attempt to connect to the end point that's specified in the 
        ///     constructor. This method will block until the connection attempt completes and will throw a 
        ///     <see cref="HazelException"/> if there is a problem connecting.
        /// </remarks>
        public abstract void Connect(byte[] bytes = null, int timeout = 5000);

        /// <summary>
        ///     Invokes the DataReceived event.
        /// </summary>
        /// <param name="bytes">The bytes received.</param>
        /// <param name="sendOption">The <see cref="SendOption"/> the message was received with.</param>
        /// <remarks>
        ///     Invokes the <see cref="DataReceived"/> event on this connection to alert subscribers a new message has been
        ///     received. The bytes and the send option that the message was sent with should be passed in to give to the
        ///     subscribers.
        /// </remarks>
        protected void InvokeDataReceived(byte[] bytes, SendOption sendOption)
        {
            DataReceivedEventArgs args = DataReceivedEventArgs.GetObject();
            args.Set(bytes, sendOption);

            //Make a copy to avoid race condition between null check and invocation
            EventHandler<DataReceivedEventArgs> handler = DataReceived;
            if (handler != null)
                handler(this, args);
        }

        /// <summary>
        ///     Invokes the Disconnected event.
        /// </summary>
        /// <param name="e">The exception, if any, that occured to cause this.</param>
        /// <remarks>
        ///     Invokes the <see cref="Disconnected"/> event to alert subscribres this connection has been disconnected either 
        ///     by the end point or because an error occured. If an error occured the error should be passed in in order to 
        ///     pass to the subscribers, otherwise null can be passed in.
        /// </remarks>
        protected void InvokeDisconnected(Exception e = null)
        {
            DisconnectedEventArgs args = DisconnectedEventArgs.GetObject();
            args.Set(e);

            //Make a copy to avoid race condition between null check and invocation
            EventHandler<DisconnectedEventArgs> handler = Disconnected;
            if (handler != null)
                handler(this, args);
        }

        /// <summary>
        ///     Blocks until the Connection is connected.
        /// </summary>
        /// <param name="timeout">The number of milliseconds to wait before timing out.</param>
        /// <remarks>
        ///     This is a helper method for waiting until the connection is connected. It will block until the 
        ///     <see cref="State"/> property is set to <see cref="ConnectionState.Connected"/> allowing the main thread to 
        ///     wait until specific data is received etc. before returning to the user's code.
        /// </remarks>
        protected bool WaitOnConnect(int timeout)
        {
            return connectWaitLock.WaitOne(timeout);
        }

        /// <summary>
        ///     Closes this connection safely.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Informs the end point of the connection that we are disconnecting from them and disposes of this 
        ///         connection.
        ///     </para>
        ///     <para>
        ///         This calls <see cref="Dispose()"/> and therefore sets <see cref="State"/> straight to 
        ///         <see cref="ConnectionState.NotConnected"/>. Once you call Close you will not be able to send any more
        ///         data using this connection and no more data will be received.
        ///     </para> 
        /// </remarks>
        public virtual void Close()
        {
            Dispose();
        }

        /// <summary>
        ///     Disposes of this NetworkConnection.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Disposes of this NetworkConnection.
        /// </summary>
        /// <param name="disposing">Are we currently disposing?</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeKeepAliveTimer();
            }
        }
    }
}
