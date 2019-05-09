using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hazel.Tcp
{
    public static class TcpOptions
    {
        /// <summary>
        /// Force data received even to fire before reading the next message. 
        /// Setting to false may offer a slight performance advantage at the risk of compromising message ordering. 
        /// </summary>
        public static bool ForceReliableOrdering { get; set; } = false;
    }
}
