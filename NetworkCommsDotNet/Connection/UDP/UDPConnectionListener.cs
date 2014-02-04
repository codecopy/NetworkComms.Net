﻿//  Copyright 2009-2014 Marc Fletcher, Matthew Dean
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
//  A commercial license of this software can also be purchased. 
//  Please see <http://www.networkcomms.net/licensing/> for details.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using NetworkCommsDotNet.DPSBase;
using NetworkCommsDotNet.Tools;

#if NETFX_CORE
using NetworkCommsDotNet.Tools.XPlatformHelper;
#else
using System.Net.Sockets;
#endif

namespace NetworkCommsDotNet.Connections.UDP
{
    /// <summary>
    /// A UDP connection listener
    /// </summary>
    public class UDPConnectionListener : ConnectionListenerBase
    {
        /// <summary>
        /// The UDPOptions to be used for this listener
        /// </summary>
        public UDPOptions UDPOptions { get; private set; }

        /// <summary>
        /// The UDP listener is a UDP connection
        /// </summary>
        internal UDPConnection UDPConnection { get; set; }

        /// <summary>
        /// Create a new instance of a UDP listener
        /// </summary>
        /// <param name="sendReceiveOptions">The SendReceiveOptions to use with incoming data on this listener</param>
        /// <param name="applicationLayerProtocol">If enabled NetworkComms.Net uses a custom 
        /// application layer protocol to provide useful features such as inline serialisation, 
        /// transparent packet transmission, remote peer handshake and information etc. We strongly 
        /// recommend you enable the NetworkComms.Net application layer protocol.</param>
        /// <param name="udpOptions">The UDPOptions to use with this listener</param>
        /// <param name="allowDiscoverable">Determines if the newly created <see cref="ConnectionListenerBase"/> should be discoverable via <see cref="Tools.PeerDiscovery"/></param>
        public UDPConnectionListener(SendReceiveOptions sendReceiveOptions,
            ApplicationLayerProtocolStatus applicationLayerProtocol, 
            UDPOptions udpOptions, bool allowDiscoverable = false)
            :base(ConnectionType.UDP, sendReceiveOptions, applicationLayerProtocol, allowDiscoverable)
        {
            if (applicationLayerProtocol == ApplicationLayerProtocolStatus.Disabled && udpOptions != UDPOptions.None)
                throw new ArgumentException("If the application layer protocol has been disabled the provided UDPOptions can only be UDPOptions.None.");

            UDPOptions = udpOptions;
        }

        /// <inheritdoc />
        internal override void StartListening(EndPoint desiredLocalListenEndPoint, bool useRandomPortFailOver)
        {
            if (desiredLocalListenEndPoint.GetType() != typeof(IPEndPoint)) throw new ArgumentException("Invalid desiredLocalListenEndPoint type provided.", "desiredLocalListenEndPoint");
            if (IsListening) throw new InvalidOperationException("Attempted to call StartListening when already listening.");

            IPEndPoint desiredLocalListenIPEndPoint = (IPEndPoint)desiredLocalListenEndPoint;

            try
            {
                UDPConnection = new UDPConnection(new ConnectionInfo(ConnectionType.UDP, new IPEndPoint(IPAddress.Any, 0), desiredLocalListenIPEndPoint, ApplicationLayerProtocol, this), ListenerDefaultSendReceiveOptions, UDPOptions, true);
            }
            catch (SocketException)
            {
                if (useRandomPortFailOver)
                {
                    try
                    {
                        UDPConnection = new UDPConnection(new ConnectionInfo(ConnectionType.UDP, new IPEndPoint(IPAddress.Any, 0), new IPEndPoint(desiredLocalListenIPEndPoint.Address, 0), ApplicationLayerProtocol, this), ListenerDefaultSendReceiveOptions, UDPOptions, true);
                    }
                    catch (SocketException)
                    {
                        //If we get another socket exception this appears to be a bad IP. We will just ignore this IP
                        if (NetworkComms.LoggingEnabled) NetworkComms.Logger.Error("It was not possible to open a random port on " + desiredLocalListenIPEndPoint.Address + ". This endPoint may not support listening or possibly try again using a different port.");
                        throw new CommsSetupShutdownException("It was not possible to open a random port on " + desiredLocalListenIPEndPoint.Address + ". This endPoint may not support listening or possibly try again using a different port.");
                    }
                }
                else
                {
                    if (NetworkComms.LoggingEnabled) NetworkComms.Logger.Error("It was not possible to open port #" + desiredLocalListenIPEndPoint.Port.ToString() + " on " + desiredLocalListenIPEndPoint.Address + ". This endPoint may not support listening or possibly try again using a different port.");
                    throw new CommsSetupShutdownException("It was not possible to open port #" + desiredLocalListenIPEndPoint.Port.ToString() + " on " + desiredLocalListenIPEndPoint.Address + ". This endPoint may not support listening or possibly try again using a different port.");
                }
            }

#if WINDOWS_PHONE || NETFX_CORE
            this.LocalListenEndPoint = new IPEndPoint(IPAddress.Parse(UDPConnection.socket.Information.LocalAddress.DisplayName.ToString()), int.Parse(UDPConnection.socket.Information.LocalPort)); 
#else
            this.LocalListenEndPoint = (IPEndPoint)UDPConnection.udpClient.LocalIPEndPoint;
#endif
            this.IsListening = true;
        }

        /// <inheritdoc />
        internal override void StopListening()
        {
            this.IsListening = false;
            UDPConnection.CloseConnection(false, -16);
        }
    }
}
