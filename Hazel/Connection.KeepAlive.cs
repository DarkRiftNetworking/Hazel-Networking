using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Hazel
{
    partial class Connection
    {
        /// <summary>
        ///     The interval from data being received or transmitted to a keepalive packet being sent in milliseconds.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Keepalive packets serve to close connections when an endpoint abruptly disconnects and to ensure than any
        ///         NAT devices do not close their translation for our argument. By ensuring there is regular contact the
        ///         connection can detect and prevent these issues.
        ///     </para>
        ///     <para>
        ///         The default value is 10 seconds, set to System.Threading.Timeout.Infinite to disable keepalive packets.
        ///     </para>
        /// </remarks>
        public int KeepAliveInterval
        {
            get
            {
                return keepAliveInterval;
            }

            set
            {
                keepAliveInterval = value;

                //Update timer
                ResetKeepAliveTimer();
            }
        }
        int keepAliveInterval = 10000;

        /// <summary>
        ///     The timer creating keepalive pulses.
        /// </summary>
        Timer keepAliveTimer;

        /// <summary>
        ///     Lock for keep alive timer.
        /// </summary>
        Object keepAliveTimerLock = new Object();

        /// <summary>
        ///     Has the keep alive timer been disposed already?
        /// </summary>
        bool keepAliveTimerDisposed;

        readonly byte[] keepAlivePacket = new byte[1];

        /// <summary>
        ///     Starts the keepalive timer.
        /// </summary>
        protected void InitializeKeepAliveTimer()
        {
            lock (keepAliveTimerLock)
            {
                keepAliveTimer = new Timer(
                    (o) =>
                    {
                        Trace.WriteLine("Keepalive packet sent.");
                        SendBytes(keepAlivePacket, SendOption.KeepAlive);
                    },
                    null,
                    keepAliveInterval,
                    keepAliveInterval
                );
            }
        }

        /// <summary>
        ///     Resets the keepalive timer to zero.
        /// </summary>
        protected void ResetKeepAliveTimer()
        {
            lock (keepAliveTimerLock)
            {                
                if(keepAliveTimer != null)
                    keepAliveTimer.Change(keepAliveInterval, keepAliveInterval);
            }
        }

        /// <summary>
        ///     Disposes of the keep alive timer.
        /// </summary>
        protected void DisposeKeepAliveTimer()
        {
            lock (keepAliveTimerLock)
            {
                if (!keepAliveTimerDisposed && keepAliveTimer != null)
                    keepAliveTimer.Dispose();
                keepAliveTimerDisposed = true;
            }
        }
    }
}
