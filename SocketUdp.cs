using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketHelper
{
    /// <summary>
    /// UDP通讯类
    /// </summary>
    public class SocketUdp
    {
        /// <summary>
        /// 接收数据
        /// </summary>
        private byte[] receiveBuffer = new byte[1024];
        /// <summary>
        /// 接收远程IP
        /// </summary>
        private EndPoint receveIP;
        /// <summary>
        /// socket实例
        /// </summary>
        public Socket socket;
        /// <summary>
        /// 接收事件后广播
        /// </summary>
        public event Action<byte[], EndPoint> Recevier;
        /// <summary>
        /// socket使用的IP
        /// </summary>
        private IPEndPoint localIP;
        /// <summary>
        /// 错误事件
        /// </summary>
        public event Action<string> ErrInfoEvent;

        private static uint IOC_IN = 0x80000000;
        private static uint IOC_VENDOR = 0x18000000;
        private static uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="point">本机端口号</param>
        public SocketUdp(int point)
        {
            try
            {
                receveIP = new IPEndPoint(IPAddress.Any, point);
                localIP = new IPEndPoint(SocketTools.GetLocalIP()[0], point);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);  //处理发送消息至找不到的IP后出现异常
                socket.Bind(localIP);
                socket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref receveIP, ReceiveDataAsync, null);
                //socket.BeginReceive(receiveBuffer, 0,receiveBuffer.Length, SocketFlags.None, ReceiveDataAsync, null);


            }
            catch (Exception ex)
            {
                ErrInfoEvent?.Invoke(ex.Message);
            }

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="address">本机IP</param>
        /// <param name="point">端口号</param>
        public SocketUdp(IPAddress address, int point)
        {
            try
            {
                receveIP = new IPEndPoint(IPAddress.Any, point);
                localIP = new IPEndPoint(address, point);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);  //处理发送消息至找不到的IP后出现异常
                socket.Bind(localIP);
                socket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref receveIP, ReceiveDataAsync, null);
                //socket.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveDataAsync, null);

            }
            catch (Exception ex)
            {
                ErrInfoEvent?.Invoke(ex.Message);
            }

        }

        #region socket使用UDP协议发送消息
        /// <summary>
        /// socket使用UDP协议异步发送消息
        /// </summary>
        /// <param name="data">数据byte[]类型</param>
        /// <param name="address">远端IP地址</param>
        public void SendMessage(byte[] data, IPEndPoint address)
        {
            string localIPStr = localIP.Address.ToString();  //socket绑定的IP
            string connectIPStr = address.Address.ToString();

            if (SocketTools.CheckIPGateway(localIPStr, connectIPStr) && connectIPStr != "127.0.0.1" && localIPStr != "127.0.0.1")  //同一网段则发送消息
            {
                socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, address, null, null);
            }
        }

        /// <summary>
        /// socket使用UDP协议异步发送消息
        /// </summary>
        /// <param name="data">数据byte[]类型</param>
        /// <param name="address">远端IP地址</param>
        public void SendMessage(byte[] data, string address)
        {
            IPEndPoint ip = SocketTools.StrToEndPoint(address) as IPEndPoint;
            string localIPStr = localIP.Address.ToString();  //socket绑定的IP
            string connectIPStr = ip.Address.ToString();

            if (SocketTools.CheckIPGateway(localIPStr, connectIPStr) && connectIPStr != "127.0.0.1" && localIPStr != "127.0.0.1")  //同一网段则发送消息
            {
                socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, ip, null, null);
            }
        }

        /// <summary>
        /// 以广播形式异步发送消息
        /// </summary>
        /// <param name="data">数据byte[]类型</param>
        /// <param name="port">端口</param>
        public void SendMessageBroadcast(byte[] data, int port)
        {
            string localIPStr = localIP.Address.ToString();  //socket绑定的IP
            IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, port);

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, ip, null, null);

        }
        #endregion

        #region socket UDP协议异步接收消息触发事件

        /// <summary>
        /// socket UDP协议异步接收消息触发事件
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveDataAsync(IAsyncResult ar)
        {
            int readLength = socket.EndReceiveFrom(ar, ref receveIP);
            Recevier?.Invoke(receiveBuffer, receveIP);
            Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
            socket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref receveIP, ReceiveDataAsync, null);  //接收完消息后再次进行接收
        }
        #endregion



    }
}
