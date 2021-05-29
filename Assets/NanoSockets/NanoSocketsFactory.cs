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

        private void Awake()
        {
            UDP.Initialize();     
        }

        private void OnDestroy()
        {
            UDP.Deinitialize();    
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
            return new IPEndPoint(IPAddress.IPv6Any, port);
        }

        public override EndPoint GetConnectEndPoint(string address = null, ushort? port = null)
        {
            string addressString = address ?? this.address;
            IPAddress ipAddress = GetAddress(addressString);

            ushort portIn = port ?? (ushort)this.port;

            return new IPEndPoint(ipAddress, portIn);
        }

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

    public class NanoSocket : ISocket
    {
        Socket socket;
        IPEndPoint anyEndpoint;
        Address address;

        public void Bind(EndPoint endPoint)
        {
            anyEndpoint = endPoint as IPEndPoint;
            address = Address.CreateFromIpPort(anyEndpoint.Address.ToString(), (ushort)anyEndpoint.Port);

            socket = CreateSocket();
            UDP.Bind(socket, ref address);
        }

        Socket CreateSocket()
        {
            var socket = UDP.Create(256 * 1024, 256 * 1024);
            UDP.SetNonBlocking(socket);

            return socket;
        }

        public void Connect(EndPoint endPoint)
        {
            anyEndpoint = endPoint as IPEndPoint;
            address = Address.CreateFromIpPort(anyEndpoint.Address.ToString(), (ushort)anyEndpoint.Port);

            socket = CreateSocket();

            UDP.Connect(socket, ref address);
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
            endPoint = anyEndpoint;
            return UDP.Receive(socket, ref address, buffer, buffer.Length);
        }

        public void Send(EndPoint endPoint, byte[] packet, int length)
        {
            UDP.Send(socket, ref address, packet, length);
        }
    }
}