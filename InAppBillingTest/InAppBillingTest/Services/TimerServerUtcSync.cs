using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Xamarin.Essentials;

namespace InAppBillingTest.Services
{
    internal class TimerServerUtcSync
    {
        public static DateTime GetNetworkTime()
        {
            DateTime dateTime = new DateTime(1900, 1, 1);
            //1. time.google.com, 속도:63ms
            //2. time.apple.com, 속도:120ms
            //3. time.windows.com
            //4. time.nist.gov

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                try
                {
                    dateTime = GetNetworkTime("time.google.com"); //ip 8개
                }
                catch (Exception ex)
                {
                    //dateTime = DateTime.UtcNow;
                    Console.WriteLine(ex.Message);
                }

                if (dateTime > new DateTime(1970, 1, 1))
                {
                    return dateTime;
                }
            }
            else
            {
                try
                {
                    dateTime = GetNetworkTime("time.apple.com"); //ip 5개
                }
                catch (Exception ex)
                {
                    //dateTime = DateTime.UtcNow;
                    Console.WriteLine(ex.Message);
                }

                if (dateTime > new DateTime(1970, 1, 1))
                {
                    return dateTime;
                }
            }


            try
            {
                dateTime = GetNetworkTime("time.windows.com"); //ip 1개
            }
            catch (Exception ex)
            {
                //dateTime = DateTime.UtcNow;
                Console.WriteLine(ex.Message);
            }

            if (dateTime > new DateTime(1970, 1, 1))
            {
                return dateTime;
            }



            try
            {
                dateTime = GetNetworkTime("time.nist.gov"); //ip 2개
            }
            catch (Exception ex)
            {
                dateTime = DateTime.UtcNow; //Time서버 3군데 실패 하면 스마트폰 시간 사용
                Console.WriteLine(ex.Message);
            }

            if (dateTime > new DateTime(1970, 1, 1))
            {
                return dateTime;
            }

            return dateTime; // time.nist.gov, time.google.com, time.windows.com
        }

        public static DateTime GetNetworkTime(string ntpServer)
        {
            IPAddress[] address = Dns.GetHostEntry(ntpServer).AddressList; //인터넷이 안되면 여기서 Exception에러 발생.

            if (address == null || address.Length == 0)
            {
                throw new ArgumentException("Could not resolve ip address from '" + ntpServer + "'.", "ntpServer");
            }

            DateTime dateTime = new DateTime(1900, 1, 1);

            foreach (var item in address)
            {
                IPEndPoint ep = new IPEndPoint(item, 123);
                dateTime = GetNetworkTime(ep);
                if (dateTime > new DateTime(1970, 1, 1))
                {
                    break;
                }
            }
            //IPEndPoint ep = new IPEndPoint(address[0], 123);
            //return GetNetworkTime(ep);
            return dateTime;
        }

        public static DateTime GetNetworkTime(IPEndPoint ep)
        {
            DateTime dateTime = new DateTime(1900, 1, 1);

            try
            {
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                Console.WriteLine(s.SendTimeout);
                Console.WriteLine(s.ReceiveTimeout);
                s.SendTimeout = 2000;
                s.ReceiveTimeout = 2000;
                Console.WriteLine(s.SendTimeout);
                Console.WriteLine(s.ReceiveTimeout);

                s.Connect(ep);

                byte[] ntpData = new byte[48]; // RFC 2030 
                ntpData[0] = 0x1B;
                for (int i = 1; i < 48; i++)
                    ntpData[i] = 0;

                s.Send(ntpData);
                s.Receive(ntpData);

                byte offsetTransmitTime = 40;
                ulong intpart = 0;
                ulong fractpart = 0;

                for (int i = 0; i <= 3; i++)
                    intpart = 256 * intpart + ntpData[offsetTransmitTime + i];

                for (int i = 4; i <= 7; i++)
                    fractpart = 256 * fractpart + ntpData[offsetTransmitTime + i];

                ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);
                s.Close();

                TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);

                //UTC 시간 계산하기
                //utc 시간은 1970년 1월 1일 부터 오늘까지 시간을 더해서 표시
                //DateTime dateTime = new DateTime(1970, 1, 1, 23, 59, 59);
                //DateTime dateTime = new DateTime(1900, 1, 1);
                dateTime += timeSpan;
                Console.WriteLine(dateTime); //utc time

                Console.WriteLine(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            //TimeSpan offsetAmount = TimeZone.CurrentTimeZone.GetUtcOffset(dateTime);
            //DateTime networkDateTime = (dateTime + offsetAmount);  //현지 시간

            return dateTime;
        }
    }
}
