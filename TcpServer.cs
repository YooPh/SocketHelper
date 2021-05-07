using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketHelper
{
    public class TcpServer
    {
        /// <summary>
        /// 创建一个和客户端通信的套接字
        /// </summary>
        private Socket socketServer = null;
        /// <summary>
        /// 接收缓存
        /// </summary>
        private byte[] receiveBuffer = new byte[1024];
        /// <summary>
        /// 缓存连接信息
        /// </summary>
        private Dictionary<EndPoint, Socket> socketsDic = new Dictionary<EndPoint, Socket>();

        private List<EndPoint> removeList = new List<EndPoint>();

        private List<EndPoint> clientConnectList = new List<EndPoint>();
        /// <summary>
        /// 客户端连接集合
        /// </summary>
        public List<EndPoint> ClientConnectList { get { return clientConnectList; } }

        /// <summary>
        /// 接收事件后广播
        /// </summary>
        public event Action<byte[], EndPoint> RecevierEvent;
        /// <summary>
        /// 客户端连接断开改变事件
        /// </summary>
        public event Action ConnectChangeEvent;
        /// <summary>
        /// 错误事件
        /// </summary>
        public event Action<string> ErrInfoEvent;

        /// <summary>
        /// TCP服务创建成功
        /// </summary>
        public bool TcpIsStart { get; set; }

        #region 开启TCP通讯服务
        /// <summary>
        /// 开启TCP通信服务
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            return Start(IPAddress.Loopback);
        }

        /// <summary>
        /// 开启TCP通信服务
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="point">端口号</param>
        /// <param name="listenCount">监听数量</param>
        /// <returns></returns>
        public bool Start(IPAddress ipAddress, int point = 9090, int listenCount = 10)
        {
            try
            {
                if (!TcpIsStart)
                {
                    //定义一个套接字用于监听客户端发来的消息，包含三个参数（IP4寻址协议，流式连接，Tcp协议）  
                    socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    //将IP地址和端口号绑定到网络节点point上  
                    IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, point);
                    //绑定监听IP
                    socketServer.Bind(iPEndPoint);
                    //套接字的监听队列长度
                    socketServer.Listen(listenCount);
                    //异步监听客户端连接
                    socketServer.BeginAccept(AcceptAsync, socketServer);
                    TcpIsStart = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrInfoEvent?.Invoke(ex.Message);
                TcpIsStart = false;
                return false;
            }
        }
        #endregion

        #region 异步监听客户端
        /// <summary>
        /// 异步监听客户端
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptAsync(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.BeginAccept(AcceptAsync, socket);
                Socket socketClient = socket.EndAccept(ar);
                socketsDic.Add(socketClient.RemoteEndPoint, socketClient);
                clientConnectList.Add(socketClient.RemoteEndPoint);
                ConnectChangeEvent?.Invoke();
                byte[] arrSendMsg = Encoding.UTF8.GetBytes("Connected Successfully!");
                Send(arrSendMsg, socketClient.RemoteEndPoint);
                socketClient.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveDataAsync, socketClient);
            }
            catch (Exception ex)
            {
                ErrInfoEvent?.Invoke(ex.Message);
            }

        }
        #endregion

        #region 异步接收客户端消息
        /// <summary>
        /// 异步接收客户端消息
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveDataAsync(IAsyncResult ar)
        {
            Socket socketClient = ar.AsyncState as Socket;
            try
            {
                int readLength = socketClient.EndReceive(ar);
                if (readLength > 0)
                {
                    RecevierEvent?.Invoke(receiveBuffer, socketClient.RemoteEndPoint);
                    Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
                    socketClient.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveDataAsync, socketClient);
                }
                else
                {
                    socketsDic.Remove(socketClient.RemoteEndPoint);
                    clientConnectList.Remove(socketClient.RemoteEndPoint);
                    ConnectChangeEvent?.Invoke();
                }
            }
            catch (Exception ex)
            {
                socketsDic.Remove(socketClient.RemoteEndPoint);
                clientConnectList.Remove(socketClient.RemoteEndPoint);
                ErrInfoEvent?.Invoke(ex.Message);
            }

        }
        #endregion

        #region  异步发送消息
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="sentBuffer">发送内容(byte[] 类型)</param>
        /// <param name="strEndPoint">已连接服务器的远端字符串类型地址</param>
        public void Send(byte[] sentBuffer, string strEndPoint)
        {
            Send(sentBuffer, SocketTools.StrToEndPoint(strEndPoint));
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="sendBuffer">发送内容(byte[] 类型)</param>
        /// <param name="endPoint">已连接服务器的远端EndPoint类型地址</param>
        public void Send(byte[] sendBuffer, EndPoint endPoint)
        {
            try
            {
                Socket socket = socketsDic[endPoint];

                if (!((socket.Poll(500, SelectMode.SelectRead) && socket.Available == 0)) || !socket.Connected)
                {
                    socket.BeginSend(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, null, null);
                }
            }
            catch (Exception ex)
            {
                ErrInfoEvent?.Invoke(ex.Message);
            }
        }
        #endregion

        #region 判断客户端是否断线
        /// <summary>
        /// 判断指定客户端是否断线
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public bool ClientIsConnected(EndPoint endPoint)
        {
            if (endPoint == null) return false;
            return !socketsDic[endPoint].Poll(500, SelectMode.SelectRead);
        }

        #endregion

        #region 关闭TCP服务
        /// <summary>
        /// 关闭TCP服务器
        /// </summary>
        public void Close()
        {
            if (socketServer == null) return;

            TcpIsStart = false;

            foreach (var socketItem in socketsDic.Values)
            {
                try
                {
                    socketItem.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }
                try
                {
                    socketItem.Close();
                }
                catch
                {

                }

            }
            try
            {
                socketServer.Shutdown(SocketShutdown.Both);

            }
            catch
            {
            }
            try
            {
                socketServer.Close();
            }
            catch
            {

            }

            socketsDic.Clear();
            clientConnectList.Clear();
            ConnectChangeEvent?.Invoke();
        }
        #endregion


    }
}
