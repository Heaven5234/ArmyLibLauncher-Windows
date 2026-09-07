using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ArmyLib.Source
{
    public class Host
    {
        private static async Task<int> GetAvailablePortAsync(int startRange, int endRange)
        {
            try
            {
                IPEndPoint[] endPoints;
                List<int> portArray = new List<int>();

                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();

                TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
                portArray.AddRange(from tcpc in connections where tcpc.LocalEndPoint.Port >= startRange select tcpc.LocalEndPoint.Port);

                endPoints = properties.GetActiveTcpListeners();
                portArray.AddRange(from tcpl in endPoints where tcpl.Port >= startRange select tcpl.Port);

                endPoints = properties.GetActiveUdpListeners();
                portArray.AddRange(from udp in endPoints where udp.Port >= startRange select udp.Port);

                for (int i = startRange; i <= endRange; i++)
                {
                    if (!portArray.Contains(i))
                    {
                        return i;
                    }
                }
            }

            catch(Exception ex)
            {
                Debug.WriteLine($"GetAvailbalePort : error {ex.Message}");
            }

            return -1;
        }

        public static async Task<int> GetAvailablePort(int startRange, int endRange)
        {
            return await GetAvailablePortAsync(startRange, endRange);
        }
    }
}
