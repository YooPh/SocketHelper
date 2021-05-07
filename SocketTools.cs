using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketHelper
{
    public class SocketTools
    {
        public static EndPoint StrToEndPoint(string strEndPoint)
        {
            IPAddress ip = IPAddress.Parse(strEndPoint.Split(':')[0]);
            IPEndPoint endPoint = new IPEndPoint(ip, Convert.ToInt32(strEndPoint.Split(':')[1]));
            return endPoint;
        }

        public static EndPoint StrToEndPoint(string ipAdr,int port)
        {
            IPAddress ip = IPAddress.Parse(ipAdr);
            IPEndPoint endPoint = new IPEndPoint(ip, port);
            return endPoint;
        }

        #region ping ip,测试能否ping通
        /// <summary>
        /// ping ip,测试能否ping通
        /// </summary>
        /// <param name="strIP">IP地址</param>
        /// <returns></returns>
        public static bool PingIp(string strIP)
        {
            bool bRet = false;
            try
            {
                Ping pingSend = new Ping();
                PingReply reply = pingSend.Send(strIP, 1000);
                if (reply.Status == IPStatus.Success)
                    bRet = true;
            }
            catch
            {
                bRet = false;
            }
            return bRet;
        }
        #endregion

        #region 判断IP地址是否处在同一网段
        /// <summary>
        /// 判断IP地址是否处在同一网段
        /// </summary>
        /// <param name="ip1">对比的IP地址1</param>
        /// <param name="ip2">对比的IP地址2</param>
        /// <returns>返回结果bool</returns>
        public static bool CheckIPGateway(string ip1, string ip2)
        {
            string[] ip1_List = ip1.Split('.');
            string[] ip2_List = ip2.Split('.');

            for (int i = 0; i < 3; i++)
            {
                if (ip1_List[i] != ip2_List[i])
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region 检查CRC校验
        private bool CheckResponse(byte[] response, int Length)
        {
            byte[] CRC = new byte[2];
            GetCRC(response, Length, ref CRC);
            try
            {
                return CRC[0] == response[Length - 2] && CRC[1] == response[Length - 1];
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region 计算CRC
        private void GetCRC(byte[] message, int Length, ref byte[] CRC)
        {
            ushort CRCFull = 0xFFFF;
            byte CRCHigh = 0xFF, CRCLow = 0xFF;
            char CRCLSB;

            for (int i = 0; i < Length - 2; i++)
            {
                CRCFull = (ushort)(CRCFull ^ message[i]);

                for (int j = 0; j < 8; j++)
                {
                    CRCLSB = (char)(CRCFull & 0x0001);
                    CRCFull = (ushort)((CRCFull >> 1) & 0x7FFF);

                    if (CRCLSB == 1)
                        CRCFull = (ushort)(CRCFull ^ 0xA001);
                }
            }
            CRC[1] = CRCHigh = (byte)((CRCFull >> 8) & 0xFF);
            CRC[0] = CRCLow = (byte)(CRCFull & 0xFF);
        }
        #endregion

        #region 获取本地IP地址
        /// <summary>
        /// 获取本地IP地址
        /// </summary>
        /// <returns></returns>
        public static List<IPAddress> GetLocalIP()
        {
            List<IPAddress> ipList = new List<IPAddress>();
            try
            {
                IPHostEntry IpEntry = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress item in IpEntry.AddressList)
                {
                    if (item.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipList.Add(item);
                    }
                }
                return ipList;
            }
            catch
            {
                return ipList;
            }
        }
        #endregion
    }
}
