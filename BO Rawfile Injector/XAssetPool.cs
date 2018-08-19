using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Ionic.Zlib;

namespace BO_Rawfile_Injector
{
    class Mem
    {
        public static void Connect()
        {
            PS3.Connect();
            PS3.AttachProcess();
        }
    };

    public class Rawfile
    {
        //struct info.
        public uint name_ptr;
        public uint length;
        public uint buffer_ptr;

        //custom info
        public int index;
        public string name;
        public byte[] buffer;
        public bool requireOverwrite;
    }

    public class RawPool
    {
        private const uint MALLOC = 0x4cf20; //address to malloc with RPC (not working)

        /* keep in mind XAssetPool is referring to start of the Rawfile XAssetPool */
        private uint XAssetPool;// = 0x00e921f8;//0x1186DC0;
        private const int RawfileSize = 12; //original struct is 12 bytes.
        public const int PoolMax = 0x400;

        private const uint WRITE_ADDR = 0x2000000;//0x2600250; //just found a bunch of empty space to write too, malloc would be better.
        private uint WRITE_POS = 0;

        public List<int> freeIndices = new List<int>(); //all the free indices (not including freehead)
        public List<Rawfile> rawfiles = new List<Rawfile>(); //all 'used' rawfiles.
        public int maxIndex = 0; //the max index to write too (speeds up time)

        byte[] PoolBuffer; //the buffer containing the XAssetPool table

        public RawPool(uint xassetpool)
        {
            XAssetPool = xassetpool;
        }

        public void ReadPoolData()
        {
            PoolBuffer = PS3.GetMemory( XAssetPool, (PoolMax * RawfileSize) + 4 ); //get the entire table.
        }

        public void ReadFree()
        {
            freeIndices.Clear(); //remove all previously added freeIndices.

            int firstUnused = getInt(PoolBuffer, 0); //load the freeHead first.
            int addr = ( firstUnused - Convert.ToInt32(XAssetPool) );
            int unusedAddr;

            for (int i = 0; i < PoolMax; i++)
            {
                if (firstUnused == 0)
                    break;

                unusedAddr = getInt(PoolBuffer, addr);
                if (unusedAddr == 0)
                    break;

                addr = ( unusedAddr - Convert.ToInt32(XAssetPool) );
                int index = addr / RawfileSize;

                freeIndices.Add(index);
            }
        }

        public void ReadRawfiles()
        {
            Mem.Connect(); //may not need.
            rawfiles.Clear(); //clear old rawfiles.

            for (int i = 0; i < PoolMax; i++)
            {
                if (freeIndices.IndexOf(i) >= 0) //it's a free location. skip to next rawfile.
                {
                    continue;
                }

                Task task = getRawfile(i);
                maxIndex = i;
            }
        }

        private Task getRawfile(int index)
        {
            return getRawfileData(index); //return new Task(() => getRawfileData(index));
        }

        public async Task getRawfileData(int index)
        {
            Rawfile raw = new Rawfile();

            raw.index = index;
            raw.name_ptr = (uint)getInt(PoolBuffer, (index * RawfileSize) + 4);
            raw.requireOverwrite = false;
            raw.length = (uint)getInt(PoolBuffer, (index * RawfileSize) + 8);
            raw.buffer_ptr = (uint)getInt(PoolBuffer, (index * RawfileSize) + 12); //could remove this for additional speed.
            //raw.name = PS3.ReadCString(raw.name_ptr); //this is returning null for some reason...
            //Console.WriteLine("name: {0}", PS3.ReadCString(raw.name_ptr));
            //Console.WriteLine("buffer addr: 0x{0}", raw.buffer_ptr.ToString("X"));
            //Console.WriteLine("");

            rawfiles.Add(raw);

        }

        public string ReadLocalFiles(string path)
        {
            string output = "";
            foreach (string str in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            {
                string raw_name = str.Substring(path.Length + 1).Replace('\\', '/');
                uint len = (uint)new FileInfo(str).Length + 1;
                byte[] buffer = new byte[len];

                Buffer.BlockCopy(File.ReadAllBytes(str), 0, buffer, 0, (int)len - 1);

                if (raw_name.EndsWith(".gsc") || raw_name.EndsWith(".csc"))
                {
                    uint uncomp_len = len;
                    byte[] comp = ZlibStream.CompressBuffer(buffer);
                    uint comp_len = (uint)comp.Length;
                    byte[] script_header = new byte[comp_len + 8];

                    Buffer.BlockCopy(getBytes(uncomp_len), 0, script_header, 0, 4);
                    Buffer.BlockCopy(getBytes(comp_len), 0, script_header, 4, 4);
                    Buffer.BlockCopy(comp, 0, script_header, 8, (int)comp_len);

                    buffer = script_header;
                    len = comp_len + 8;
                }

                int file_index = indexOfArray(raw_name);
                if (file_index >= 0)
                {
                    rawfiles[file_index].requireOverwrite = true;
                    rawfiles[file_index].buffer = buffer;
                    rawfiles[file_index].length = len;

                    //found similar rawfiles...
                    output += "Overwrote: [ 0x" + file_index.ToString("X") + " ] " + rawfiles[file_index].name + "\n";
                }
                else
                {
                    //file not old and shouldn't be overwritten, write new...
                }

                //Console.Write("name: {0}\nindex: {1}\n", raw_name, file_index);
            }
            return output;
        }

        public void writeRawfile(Rawfile raw)
        {
            int index = raw.index;
            uint write_addr = WRITE_ADDR + WRITE_POS;

            /*
            if(raw.CustomFile)
            {
                PS3.WriteString(write_addr, raw.name);

                WRITE_POS += (uint)raw.name.Length + 1;

                raw.name_ptr = write_addr;
                PS3.WriteUInt(XAssetPool + (uint)(index * RawfileSize) + 4, raw.name_ptr); //update the name ptr
            }
             */

            PS3.SetMemory(write_addr, raw.buffer);//write in mem

            WRITE_POS += raw.length;

            raw.buffer_ptr = write_addr;
            PS3.WriteUInt(XAssetPool + (uint)(index * RawfileSize) + 12, raw.buffer_ptr); //update the table
            PS3.WriteUInt(XAssetPool + (uint)(index * RawfileSize) + 8, raw.length); //update the length.

            uint table_addr = XAssetPool + (uint)(index * RawfileSize) + 12;
            uint len_addr = XAssetPool + (uint)(index * RawfileSize) + 8;
        }

        public void updateFreeHead()
        {
            PS3.WriteUInt(XAssetPool, (uint)freeIndices[0]);
        }

        private int indexOfArray(string name) //returns the index in the array, NOT IN THE TABLE (see raw.index  for that)
        {
            for (int i = 0; i < rawfiles.Count; i++)
            {
                if (rawfiles[i].name == name)
                    return i;
            }
            return -1;
        }

        private byte[] getBytes(uint v)
        {
            byte[] a = new byte[4];
            Buffer.BlockCopy(BitConverter.GetBytes(v), 0, a, 0, 4);
            Array.Reverse(a);
            return a;

        }

        private int getInt(byte[] _buffer, int position)
        {
            byte[] a = new byte[4];
            Buffer.BlockCopy(_buffer, position, a, 0, 4);
            Array.Reverse(a);
            return BitConverter.ToInt32(a, 0);
        }
    }
}
