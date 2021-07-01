using System;
using System.Net;
using System.Net.Sockets;
using Mirage.SocketLayer;
using UnityEngine;

namespace Mirage.Sockets.NanoSockets
{
    public sealed class NanoSocketsFactory : SocketFactory
    {
        [SerializeField] string address = "localhost";
        [SerializeField] int port = 7777;

        static int initCount;

        private void Awake()
        {
            if (initCount == 0)
            {
                UDP.Initialize();
            }

            initCount++;
        }

        private void OnDestroy()
        {
            initCount--;

            if (initCount == 0)
            {
                UDP.Deinitialize();
            }
        }

        public override ISocket CreateClientSocket()
        {
            ThrowIfNotSupported();

            return new NanoSocket();
        }

        public override ISocket CreateServerSocket()
        {
            ThrowIfNotSupported();

            return new NanoSocket();
        }

        public override EndPoint GetBindEndPoint()
        {
            var address = Address.CreateFromIpPort("::0", (ushort)port);
            return new NanoEndPoint(address);
        }

        public override EndPoint GetConnectEndPoint(string address = null, ushort? port = null)
        {
            string addressString = address ?? this.address;
            IPAddress ipAddress = GetAddress(addressString);

            ushort portIn = port ?? (ushort)this.port;
            var nanoAddress = Address.CreateFromIpPort(ipAddress.ToString(), portIn);

            return new NanoEndPoint(nanoAddress);
        }

        /// <summary>
        /// Gets Ip address from a hostname or ip address
        /// <para>if string is hostname will use dns to get ip address</para>
        /// </summary>
        /// <param name="addressString"></param>
        /// <returns></returns>
        private IPAddress GetAddress(string addressString)
        {
            if (IPAddress.TryParse(addressString, out IPAddress address))
                return address;

            IPAddress[] results = Dns.GetHostAddresses(addressString);
            if (results.Length == 0)
            {
                throw new SocketException((int)SocketError.HostNotFound);
            }
            else
            {
                return results[0];
            }
        }

        void ThrowIfNotSupported()
        {
            if (IsWebgl)
            {
                throw new NotSupportedException("Nano Socket can not be created in Webgl builds, Use WebSocket instead");
            }
        }

        private static bool IsWebgl => Application.platform == RuntimePlatform.WebGLPlayer;
    }

    public class NanoEndPoint : EndPoint
    {
        public Address address;

        public NanoEndPoint(Address address)
        {
            this.address = address;
        }

        public override bool Equals(object obj)
        {
            if (obj is NanoEndPoint other)
            {
                return address.Equals(other.address);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return address.GetHashCode();
        }
    }
    public class NanoSocket : ISocket
    {
        Socket socket;
        NanoEndPoint anyEndpoint;

        public void Bind(EndPoint endPoint)
        {
            anyEndpoint = endPoint as NanoEndPoint;

            socket = CreateSocket();

            UDP.Bind(socket, ref anyEndpoint.address);
        }

        Socket CreateSocket()
        {
            Socket socket = UDP.Create(256 * 1024, 256 * 1024);
            UDP.SetNonBlocking(socket);

            return socket;
        }

        public void Connect(EndPoint endPoint)
        {
            anyEndpoint = endPoint as NanoEndPoint;

            socket = CreateSocket();

            UDP.Connect(socket, ref anyEndpoint.address);
        }

        public void Close()
        {
            UDP.Destroy(ref socket);
        }

        public bool Poll()
        {
            return UDP.Poll(socket, 0) > 0;
        }

        public int Receive(byte[] buffer, out EndPoint endPoint)
        {
            // todo remove alloc
            var nanoEndPoint = new NanoEndPoint(anyEndpoint.address);
            endPoint = nanoEndPoint;
            return UDP.Receive(socket, ref nanoEndPoint.address, buffer, buffer.Length);
        }

        public void Send(EndPoint endPoint, byte[] packet, int length)
        {
            var nanoEndPoint = endPoint as NanoEndPoint;
            UDP.Send(socket, ref nanoEndPoint.address, packet, length);
        }
    }
}
