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
    public class TcpClient
    {
        /// <summary>
        /// 连接客户端的套接字
        /// </summary>
        private Socket socketClient = null;
        /// <summary>
        /// 接收缓存
        /// </summary>
        private byte[] receiveBuffer = new byte[1024];
        /// <summary>
        /// 与服务器连接状态
        /// </summary>
        public bool ConnectSucceed { get; set; }
        /// <summary>
        /// 服务器IP
        /// </summary>
        private IPEndPoint remoteIp = null;
        /// <summary>
        /// 接收事件后广播
        /// </summary>
        public event Action<byte[], EndPoint> RecevierEvent;
        /// <summary>
        /// 错误事件
        /// </summary>
        public event Action<string> ErrInfoEvent;
        /// <summary>
        /// 服务器关闭事件
        /// </summary>
        public event Action ServerShutdownEvent;

        #region 连接服务器
        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="remoteIp">服务器IP</param>
        /// <param name="remotePort">服务器端口号</param>
        /// <returns></returns>
        public bool Connect(string remoteIp="127.0.0.1", int remotePort=9090,int localPort=9091)
        {
            try
            {
                if (ConnectSucceed) return false;
                socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socketClient.Bind(new IPEndPoint(IPAddress.Any, localPort));
                this.remoteIp = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
                socketClient.Connect(this.remoteIp);
                socketClient.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveAsync, socketClient);
                RefrshServerIsConnected();
                ConnectSucceed = true;
                return true;
            }
            catch (Exception ex)
            {
                ErrInfoEvent?.Invoke(ex.Message);
                return false;
            }
        }
        #endregion

        #region 异步接收服务器发来的消息
        /// <summary>
        /// 异步接收服务器信息
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveAsync(IAsyncResult ar)
        {
            try
            {
                Socket receiveSocketAsync = ar.AsyncState as Socket;
                int readLength = receiveSocketAsync.EndReceive(ar);
                if (readLength > 0)
                {
                    RecevierEvent?.Invoke(receiveBuffer, receiveSocketAsync.RemoteEndPoint);
                    Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
                    receiveSocketAsync.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveAsync, receiveSocketAsync);
                }
                else
                {
                    DisConnect();
                    ServerShutdownEvent?.Invoke();
                }  
            }
            catch(Exception ex)
            {
                ErrInfoEvent?.Invoke(ex.Message);
            }
        }
        #endregion

        #region 发消息至服务器
        /// <summary>
        /// 发送消息到服务器
        /// </summary>
        /// <param name="sendBuffer"></param>
        public void Send(byte[] sendBuffer)
        {
            try
            {
                if (!ConnectSucceed) return;
                socketClient.BeginSend(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, null, null);

            }
            catch(Exception ex)
            {
                ErrInfoEvent?.Invoke(ex.Message);
            }
        }
        #endregion

        #region 判断与服务器是否断线
        /// <summary>
        /// 判断服务器是否断线
        /// </summary>
        /// <returns></returns>
        public bool ServerIsConnected()
        {
            if (!ConnectSucceed) return false;
            return !socketClient.Poll(500, SelectMode.SelectRead);
        }
        #endregion

        #region 实时刷新与服务器连接状态
        
        /// <summary>
        /// 实时刷新与服务器连接状态
        /// </summary>
        private void RefrshServerIsConnected()
        {
            Task.Run(() =>
            {
                while (ConnectSucceed)
                {
                    if (!socketClient.Poll(500, SelectMode.SelectRead))
                    {
                        ConnectSucceed = true;
                    }
                    else
                    {
                        ConnectSucceed = false;
                        ServerShutdownEvent?.Invoke();
                        DisConnect();
                    }
                    Thread.Sleep(1);
                }
                
            });
        }
        #endregion

        #region 断开与服务器连接
        /// <summary>
        /// 断开服务器连接
        /// </summary>
        public void DisConnect()
        {
            if (socketClient == null) return;

            try
            {
                socketClient.Shutdown(SocketShutdown.Both);
            }
            catch
            {

            }
            try
            {
                socketClient.Close();
            }
            catch
            {

            }
            ConnectSucceed = false;
            ServerShutdownEvent?.Invoke();
        }
        #endregion
    }
}
