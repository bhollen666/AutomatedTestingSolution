using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace AutomatedGatewayTesting
{
    class Program
    {
        static void Main(string[] args)
        {
                                                    // COMMAND LINE ARGUMENTS
            String cmdFileName = null;              // man:[0] Command input file name
            String hostIP = null;                   // man:[1] Host IP address (dotted quad currently only supported)
            int hostPort = 0;                       // man:[2] Host port number
            String logFileName = null;              // opt:[3] Name of optional log file
                                                    //
            string[] GatewayCommands;               // List of commands to exchange with the Gateway
            bool loggingEnabled = false;            // Boolean for logging commands
            System.IO.StreamWriter swLog = null;    // Output log file option

            if (args.Length < 3)
            {
                Console.WriteLine("Error: Missing command input arguments!");
                Console.WriteLine("AutomatedGatewayTesting[.exe] CommandsFile.config hostIP hostPort [LogFileName]");
                Console.WriteLine("AutomatedGatewayTesting.exe FTP75Cmds.config 192.168.1.12 7798 20140325.Log");
                return;
            }
            else
            {
                cmdFileName = args[0].ToString();
                hostIP = args[1].ToString();
                hostPort = Convert.ToInt32(args[2].ToString());

                if (args.Length > 3)
                {
                    logFileName = args[3].ToString();
                    swLog = new System.IO.StreamWriter(logFileName);
                    swLog.AutoFlush = true;
                    loggingEnabled = true;
                }
            }

            try
            {
                GatewayCommands = File.ReadAllLines(cmdFileName);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: Unable to open or read the input file of commands!");
                return;
            }

            TcpClient tcpC = null; 
            try
            {
                tcpC = new TcpClient(hostIP, hostPort);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: Unable to open the TCP connection to the Gateway!");
                Console.WriteLine("Message=" + e.Message.ToString());
                return;
            }

            NetworkStream ns = null;
            try
            {
                ns = tcpC.GetStream();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: Unable to get stream from TCP client!");
                return;
            }

            Byte[] reqData = null;
            Byte[] reqLen = null;
            Byte[] reqDataPacket = null;
            Byte[] rspData = null;
            Byte[] rspLen = new Byte[4];
            short rspLength = 0;
            Byte[] rspDataPacket = new Byte[16384];
            String rspStr = null;
            DateTime dt;

            foreach (String s in GatewayCommands)
            {
                switch (s[0])
                {
                    case 'P':
                        {
                            String slpTime = s.Substring(2, s.Length - 2);
                            int sleepTime = Convert.ToInt32(slpTime) * 1000;
                            Console.WriteLine(DateTime.Now.ToString() + "=" + "PAUSING: " + sleepTime.ToString());
                            if (loggingEnabled)
                                swLog.WriteLine(DateTime.Now.ToString() + "=" + "PAUSING: " + sleepTime.ToString());
                            System.Threading.Thread.Sleep(sleepTime);
                            break;
                        }
                    case 'S':
                        {
                            reqData = System.Text.Encoding.ASCII.GetBytes(s.ToCharArray(2, s.Length - 2), 0, s.Length - 2);
                            reqLen = BitConverter.GetBytes(s.Length - 2);
                            reqDataPacket = CreateGatewayDataPacket(reqLen, reqData);
                            ns.Write(reqDataPacket, 0, reqDataPacket.Length);
                            Console.WriteLine(DateTime.Now.ToString() + "=" + System.Text.Encoding.ASCII.GetString(reqData));
                            if (loggingEnabled)
                                swLog.WriteLine(DateTime.Now.ToString() + "=" + System.Text.Encoding.ASCII.GetString(reqData));
                            ns.Read(rspDataPacket, 0, rspDataPacket.Length);
                            for (int i = 0; i < 4; i++)
                                rspLen[i] = rspDataPacket[i];
                            Array.Reverse(rspLen);
                            rspLength = BitConverter.ToInt16(rspLen, 0);
                            rspStr = System.Text.Encoding.ASCII.GetString(rspDataPacket, 4, rspLength);
                            Console.WriteLine(DateTime.Now.ToString() + "=" + rspStr);
                            if (loggingEnabled)
                                swLog.WriteLine(DateTime.Now.ToString() + "=" + rspStr);
                            break;
                        }
                    case 'R':
                        {
                            String ss = s.Substring(2, s.Length - 2);
                            if (String.Compare(ss, rspStr) != -1)
                            {
                                Console.WriteLine(DateTime.Now.ToString() + "=" + "MISMATCH!");
                                if (loggingEnabled)
                                    swLog.WriteLine(DateTime.Now.ToString() + "=" + "MISMATCH!");
                            }
                            break;
                        }
                    case 'W':
                        {
                            bool matched = false;
                            do
                            {
                                String reqStatus = "<request:GetGatewayStatus/>";
                                reqData = System.Text.Encoding.ASCII.GetBytes(reqStatus.ToCharArray(0, reqStatus.Length));
                                reqLen = BitConverter.GetBytes(reqStatus.Length);
                                reqDataPacket = CreateGatewayDataPacket(reqLen, reqData);
                                System.Threading.Thread.Sleep(5000);
                                ns.Write(reqDataPacket, 0, reqDataPacket.Length);
                                Console.Write(DateTime.Now.ToString() + "=" + "WAITING FOR:");
                                Console.WriteLine(s);
                                if (loggingEnabled)
                                {
                                    swLog.Write(DateTime.Now.ToString() + "=" + "WAITING FOR:");
                                    swLog.WriteLine(s);
                                }
                                ns.Read(rspDataPacket, 0, rspDataPacket.Length);
                                for (int i = 0; i < 4; i++)
                                    rspLen[i] = rspDataPacket[i];
                                Array.Reverse(rspLen);
                                rspLength = BitConverter.ToInt16(rspLen, 0);
                                rspStr = System.Text.Encoding.ASCII.GetString(rspDataPacket, 4, rspLength);
                                String ss = s.Substring(2, s.Length - 2);
                                matched = (rspStr.Contains(ss));
                            }
                            while (matched == false);
                            break;
                        }
                    default:
                        break;
                }

            }
            
            tcpC.Close();
            swLog.Close();
   
        }

        static Byte[] CreateGatewayDataPacket(Byte[] PacketLen, Byte[] PacketData)
        {
            Array.Reverse(PacketLen);
            List<byte> list1 = new List<byte>(PacketLen);
            List<byte> list2 = new List<byte>(PacketData);
            list1.AddRange(list2);
            return (list1.ToArray());
        }
    }
}
