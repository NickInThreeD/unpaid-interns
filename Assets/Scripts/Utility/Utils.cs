using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Unity.MP_FPS
{
    /// <summary>
    /// What survived the Netcode for Entities removal.
    /// </summary>
    /// <remarks>
    /// This file used to carry the ECS math and hierarchy helpers used by the DOTS
    /// character controller and the ghost rendering path. Those went with the rest of
    /// the entities layer; these two are the only members with callers outside it.
    /// </remarks>
    public static class Utils
    {
        /// <summary>
        /// Best-effort local IPv4 address, used by the direct-connect debug path to
        /// show the host what address to hand out.
        /// </summary>
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.Equals(IPAddress.Loopback))
                {
                    return ip.ToString();
                }
            }

            return IPAddress.Any.ToString();
        }

        public static void SetCursorVisible(bool isVisible)
        {
            Cursor.visible = isVisible;
            Cursor.lockState = Cursor.visible
                ? CursorLockMode.None
                : CursorLockMode.Locked;
        }
    }
}
