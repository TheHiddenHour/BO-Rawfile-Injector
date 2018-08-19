namespace BO_Rawfile_Injector
{
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using PS3Lib;

    public static class PS3
    {
        private static PS3API DEX = new PS3API();
        private static uint ProcessID;
        private static uint[] ProcessIDs;

        public static void AttachProcess()
        {
            PS3TMAPI.GetProcessList(0, out ProcessIDs);
            ulong num = ProcessIDs[0];
            ProcessID = Convert.ToUInt32(num);
            PS3TMAPI.ProcessAttach(0, PS3TMAPI.UnitType.PPU, ProcessID);
            PS3TMAPI.ProcessContinue(0, ProcessID);
            DEX.AttachProcess();
            DEX.AttachProcess();
        }

        public static void Connect()
        {
            PS3TMAPI.InitTargetComms();
            PS3TMAPI.Connect(0, null);
            DEX.ConnectTarget();
        }

        public static void Continue()
        {
            PS3TMAPI.GetProcessList(0, out ProcessIDs);
            ulong num = ProcessIDs[0];
            ProcessID = Convert.ToUInt32(num);
            PS3TMAPI.ProcessAttach(0, PS3TMAPI.UnitType.PPU, ProcessID);
            PS3TMAPI.ProcessContinue(0, ProcessID);
        }

        public static string GetGame()
        {
            PS3TMAPI.ProcessInfo processInfo = new PS3TMAPI.ProcessInfo();
            PS3TMAPI.GetProcessInfo(0, ProcessID, out processInfo);
            string str = processInfo.Hdr.ELFPath.Split(new char[] { '/' })[3];
            try
            {
                WebClient client = new WebClient();
                ServicePointManager.ServerCertificateValidationCallback = (param0, param1, param2, param3) => true;
                return client.DownloadString("https://a0.ww.np.dl.playstation.net/tpl/np/" + str + "/" + str + "-ver.xml").Replace("<TITLE>", ";").Split(new char[] { ';' })[1].Replace("</TITLE>", ";").Split(new char[] { ';' })[0];
            }
            catch
            {
                return str;
            }
        }

        public static string GetIP()
        {
            PS3TMAPI.TCPIPConnectProperties connectProperties = new PS3TMAPI.TCPIPConnectProperties();
            PS3TMAPI.GetConnectionInfo(0, out connectProperties);
            return connectProperties.IPAddress.ToString();
        }

        public static byte[] GetMemory(uint Address, int length, uint thread = 0)
        {
            Random random = new Random();
            ulong threadID = (ulong) random.Next(0, 4);
            byte[] buffer = new byte[length];
            PS3TMAPI.ProcessGetMemory(0, PS3TMAPI.UnitType.PPU, ProcessID, threadID, (ulong) Address, ref buffer);
            return buffer;
        }

        public static string GetStatus()
        {
            PS3TMAPI.ConnectStatus connected = PS3TMAPI.ConnectStatus.Connected;
            string usage = "";
            PS3TMAPI.GetConnectStatus(0, out connected, out usage);
            return connected.ToString();
        }

        public static void Pause()
        {
            PS3TMAPI.GetProcessList(0, out ProcessIDs);
            ulong num = ProcessIDs[0];
            ProcessID = Convert.ToUInt32(num);
            PS3TMAPI.ProcessAttach(0, PS3TMAPI.UnitType.PPU, ProcessID);
        }

        public static byte ReadByte(uint address)
        {
            return GetMemory(address, 1, 0)[0];
        }

        public static string ReadCString(uint addr)
        {
            byte num;
            uint num2 = 0;
            byte[] buffer = GetMemory(addr, 500); //instead of calling get mem every time it's not...
            StringBuilder builder = new StringBuilder();
            while ((num = buffer[num2++]) != 0)
            {
                builder.Append(Convert.ToChar(num));
            }
            return builder.ToString();
        }

        public static float ReadFloat(uint address)
        {
            byte[] array = GetMemory(address, 4, 0);
            Array.Reverse(array);
            return BitConverter.ToSingle(array, 0);
        }

        public static int ReadInt(uint address)
        {
            byte[] array = GetMemory(address, 4, 0);
            Array.Reverse(array);
            return BitConverter.ToInt32(array, 0);
        }

        public static short ReadShort(uint address, bool dvar = false)
        {
            byte[] array = GetMemory(address, 2, 0);
            if (!dvar)
            {
                Array.Reverse(array);
            }
            return BitConverter.ToInt16(array, 0);
        }

        public static uint ReadUInt(uint address)
        {
            byte[] array = GetMemory(address, 4, 0);
            Array.Reverse(array);
            return BitConverter.ToUInt32(array, 0);
        }

        public static byte[] Reverse(byte[] buff)
        {
            Array.Reverse(buff);
            return buff;
        }

        public static void SetMemory(uint Address, byte[] Bytes, uint thread = 0)
        {
            PS3TMAPI.ProcessSetMemory(0, PS3TMAPI.UnitType.PPU, ProcessID, (ulong) thread, (ulong) Address, Bytes);
        }

        public static void TurnOff()
        {
            PS3TMAPI.PowerOff(0, true);
        }

        public static void WriteByte(uint address, byte val)
        {
            SetMemory(address, new byte[] { val }, 0);
        }

        public static void WriteFloat(uint address, float val)
        {
            SetMemory(address, Reverse(BitConverter.GetBytes(val)), 0);
        }

        public static void WriteInt(uint address, int val)
        {
            SetMemory(address, Reverse(BitConverter.GetBytes(val)), 0);
        }

        public static void WriteShort(uint address, int val, bool dvar = false)
        {
            byte[] bytes = BitConverter.GetBytes(val);
            if (!dvar)
            {
                SetMemory(address, new byte[] { bytes[0], bytes[1] }, 0);
            }
            else
            {
                SetMemory(address, new byte[] { bytes[1], bytes[0] }, 0);
            }
        }

        public static void WriteString(uint address, string txt)
        {
            SetMemory(address, Encoding.ASCII.GetBytes(txt + "\0"), 0);
        }

        public static void WriteUInt(uint address, uint val)
        {
            SetMemory(address, Reverse(BitConverter.GetBytes(val)), 0);
        }
    }
}

