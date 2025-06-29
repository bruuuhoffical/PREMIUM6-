using System;
using System.IO;
using System.IO.Pipes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Concurrent;
using static Bruuuh.Imps;
using System.Threading;


namespace Bruuuh 
{

    struct MemoryRegionResult
    {
        public UIntPtr CurrentBaseAddress { get; set; }
        public long RegionSize { get; set; }
        public UIntPtr RegionBase { get; set; }

    }
    public class Proc
    {
        public Process Process { get; set; }
        public IntPtr Handle { get; set; }
        public bool Is64Bit { get; set; }
        //public ConcurrentDictionary<string, IntPtr> Modules { get; set; } // Use mProc.Process.Modules instead
        public ProcessModule MainModule { get; set; }
    }
    public partial class Evelyn
    {
        ConcurrentDictionary<string, CancellationTokenSource> FreezeTokenSrcs = new ConcurrentDictionary<string, CancellationTokenSource>();


        public bool FreezeValue(string address, string type, string value, string file = "")
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            if (FreezeTokenSrcs.ContainsKey(address))
            {
                Debug.WriteLine("Changing Freezing Address " + address + " Value " + value);
                try
                {
                    FreezeTokenSrcs[address].Cancel();
                    FreezeTokenSrcs.TryRemove(address, out _);
                }
                catch
                {
                    Debug.WriteLine("ERROR: Avoided a crash. Address " + address + " was not frozen.");
                    return false;
                }
            }
            else
            {
                Debug.WriteLine("Adding Freezing Address " + address + " Value " + value);
            }

            FreezeTokenSrcs.TryAdd(address, cts);

            Task.Factory.StartNew(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    WriteMemory(address, type, value, file);
                    Thread.Sleep(25);
                }
            },
            cts.Token);

            return true;
        }


        public void UnfreezeValue(string address)
        {
            Debug.WriteLine("Un-Freezing Address " + address);
            try
            {
                lock (FreezeTokenSrcs)
                {
                    FreezeTokenSrcs[address].Cancel();
                    FreezeTokenSrcs.TryRemove(address, out _);
                }
            }
            catch
            {
                Debug.WriteLine("ERROR: Address " + address + " was not frozen.");
            }
        }


        public bool WriteMemory(string code, string type, string write, string file = "", System.Text.Encoding stringEncoding = null, bool RemoveWriteProtection = true)
        {
            byte[] memory = new byte[4];
            int size = 4;

            UIntPtr theCode;
            theCode = GetCode(code, file);

            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return false;

            if (type.ToLower() == "float")
            {
                write = Convert.ToString(float.Parse(write, CultureInfo.InvariantCulture));
                memory = BitConverter.GetBytes(Convert.ToSingle(write));
                size = 4;
            }
            else if (type.ToLower() == "int")
            {
                memory = BitConverter.GetBytes(Convert.ToInt32(write));
              //  memory = BitConverter.GetBytes(Convert.ToInt64(write)); //gsdlkgjsdgdfg
                size = 4;
            }
            else if (type.ToLower() == "byte")
            {
                memory = new byte[1];
                memory[0] = Convert.ToByte(write, 16);
                size = 1;
            }
            else if (type.ToLower() == "2bytes")
            {
                memory = new byte[2];
                memory[0] = (byte)(Convert.ToInt32(write) % 256);
                memory[1] = (byte)(Convert.ToInt32(write) / 256);
                size = 2;
            }
            else if (type.ToLower() == "bytes")
            {
                if (write.Contains(",") || write.Contains(" ")) //check if it's a proper array
                {
                    string[] stringBytes;
                    if (write.Contains(","))
                        stringBytes = write.Split(',');
                    else
                        stringBytes = write.Split(' ');
                    //Debug.WriteLine("write:" + write + " stringBytes:" + stringBytes);

                    int c = stringBytes.Count();
                    memory = new byte[c];
                    for (int i = 0; i < c; i++)
                    {
                        memory[i] = Convert.ToByte(stringBytes[i], 16);
                    }
                    size = stringBytes.Count();
                }
                else //wasnt array, only 1 byte
                {
                    memory = new byte[1];
                    memory[0] = Convert.ToByte(write, 16);
                    size = 1;
                }
            }
            else if (type.ToLower() == "double")
            {
                memory = BitConverter.GetBytes(Convert.ToDouble(write));
                size = 8;
            }
            else if (type.ToLower() == "long")
            {
                memory = BitConverter.GetBytes(Convert.ToInt64(write));
                size = 8;
            }
            else if (type.ToLower() == "string")
            {
                if (stringEncoding == null)
                    memory = System.Text.Encoding.UTF8.GetBytes(write);
                else
                    memory = stringEncoding.GetBytes(write);
                size = memory.Length;
            }

            //Debug.Write("DEBUG: Writing bytes [TYPE:" + type + " ADDR:" + theCode + "] " + String.Join(",", memory) + Environment.NewLine);
            //MemoryProtection OldMemProt = 0x00;
            bool WriteProcMem = false;
            //if (RemoveWriteProtection)
            //    ChangeProtection(code, MemoryProtection.ExecuteReadWrite, out OldMemProt, file); // change protection
            WriteProcMem = WriteProcessMemory(mProc.Handle, theCode, memory, (UIntPtr)size, IntPtr.Zero);
            //if (RemoveWriteProtection)
            //    ChangeProtection(code, OldMemProt, out _, file); // restore
            return WriteProcMem;
        }

        public bool WriteMove(string code, string type, string write, int MoveQty, string file = "", int SlowDown = 0)
        {
            byte[] memory = new byte[4];
            int size = 4;

            UIntPtr theCode;
            theCode = GetCode(code, file);

            if (type == "float")
            {
                memory = new byte[write.Length];
                memory = BitConverter.GetBytes(Convert.ToSingle(write));
                size = write.Length;
            }
            else if (type == "int")
            {
                memory = BitConverter.GetBytes(Convert.ToInt32(write));
                size = 4;
            }
            else if (type == "double")
            {
                memory = BitConverter.GetBytes(Convert.ToDouble(write));
                size = 8;
            }
            else if (type == "long")
            {
                memory = BitConverter.GetBytes(Convert.ToInt64(write));
                size = 8;
            }
            else if (type == "byte")
            {
                memory = new byte[1];
                memory[0] = Convert.ToByte(write, 16);
                size = 1;
            }
            else if (type == "string")
            {
                memory = new byte[write.Length];
                memory = System.Text.Encoding.UTF8.GetBytes(write);
                size = write.Length;
            }

            UIntPtr newCode = UIntPtr.Add(theCode, MoveQty);

            //Debug.Write("DEBUG: Writing bytes [TYPE:" + type + " ADDR:[O]" + theCode + " [N]" + newCode + " MQTY:" + MoveQty + "] " + String.Join(",", memory) + Environment.NewLine);
            Thread.Sleep(SlowDown);
            return WriteProcessMemory(mProc.Handle, newCode, memory, (UIntPtr)size, IntPtr.Zero);
        }


        public void WriteBytes(string code, byte[] write, string file = "")
        {
            UIntPtr theCode;
            theCode = GetCode(code, file);
            WriteProcessMemory(mProc.Handle, theCode, write, (UIntPtr)write.Length, IntPtr.Zero);
        }


        public void WriteBits(string code, bool[] bits, string file = "")
        {
            if (bits.Length != 8)
                throw new ArgumentException("Not enough bits for a whole byte", nameof(bits));

            byte[] buf = new byte[1];

            UIntPtr theCode = GetCode(code, file);

            for (var i = 0; i < 8; i++)
            {
                if (bits[i])
                    buf[0] |= (byte)(1 << i);
            }

            WriteProcessMemory(mProc.Handle, theCode, buf, (UIntPtr)1, IntPtr.Zero);
        }

        /// <summary>
        /// Write byte array to address
        /// </summary>
        /// <param name="address">Address to write to</param>
        /// <param name="write">Byte array to write to</param>
        public void WriteBytes(UIntPtr address, byte[] write)
        {
            WriteProcessMemory(mProc.Handle, address, write, (UIntPtr)write.Length, out IntPtr bytesRead);
        }
    }
    public partial class Evelyn
    {

        public Task<IEnumerable<long>> AoBScan(string search, bool writable = false, bool executable = true, string file = "")
        {
            return AoBScan(0, long.MaxValue, search, writable, executable, false, file);
        }


        public Task<IEnumerable<long>> AoBScan(string search, bool readable, bool writable, bool executable, string file = "")
        {
            return AoBScan(0, long.MaxValue, search, readable, writable, executable, false, file);
        }



        public Task<IEnumerable<long>> AoBScan(long start, long end, string search, bool writable = false, bool executable = true, bool mapped = false, string file = "")
        {
            // Not including read only memory was scan behavior prior.
            return AoBScan(start, end, search, false, writable, executable, mapped, file);
        }


        public Task<IEnumerable<long>> AoBScan(long start, long end, string search, bool readable, bool writable, bool executable, bool mapped, string file = "")
        {
            return Task.Run(() =>
            {
                var memRegionList = new List<MemoryRegionResult>();

                string memCode = LoadCode(search, file);

                string[] stringByteArray = memCode.Split(' ');

                byte[] aobPattern = new byte[stringByteArray.Length];
                byte[] mask = new byte[stringByteArray.Length];

                for (var i = 0; i < stringByteArray.Length; i++)
                {
                    string ba = stringByteArray[i];

                    if (ba == "??" || (ba.Length == 1 && ba == "?"))
                    {
                        mask[i] = 0x00;
                        stringByteArray[i] = "0x00";
                    }
                    else if (Char.IsLetterOrDigit(ba[0]) && ba[1] == '?')
                    {
                        mask[i] = 0xF0;
                        stringByteArray[i] = ba[0] + "0";
                    }
                    else if (Char.IsLetterOrDigit(ba[1]) && ba[0] == '?')
                    {
                        mask[i] = 0x0F;
                        stringByteArray[i] = "0" + ba[1];
                    }
                    else
                        mask[i] = 0xFF;
                }


                for (int i = 0; i < stringByteArray.Length; i++)
                    aobPattern[i] = (byte)(Convert.ToByte(stringByteArray[i], 16) & mask[i]);

                SYSTEM_INFO sys_info = new SYSTEM_INFO();
                GetSystemInfo(out sys_info);

                UIntPtr proc_min_address = sys_info.minimumApplicationAddress;
                UIntPtr proc_max_address = sys_info.maximumApplicationAddress;

                if (start < (long)proc_min_address.ToUInt64())
                    start = (long)proc_min_address.ToUInt64();

                if (end > (long)proc_max_address.ToUInt64())
                    end = (long)proc_max_address.ToUInt64();

                Debug.WriteLine("[DEBUG] memory scan starting... (start:0x" + start.ToString(MSize()) + " end:0x" + end.ToString(MSize()) + " time:" + DateTime.Now.ToString("h:mm:ss tt") + ")");
                UIntPtr currentBaseAddress = new UIntPtr((ulong)start);

                MEMORY_BASIC_INFORMATION memInfo = new MEMORY_BASIC_INFORMATION();

                //Debug.WriteLine("[DEBUG] start:0x" + start.ToString("X8") + " curBase:0x" + currentBaseAddress.ToUInt64().ToString("X8") + " end:0x" + end.ToString("X8") + " size:0x" + memInfo.RegionSize.ToString("X8") + " vAloc:" + VirtualQueryEx(mProc.Handle, currentBaseAddress, out memInfo).ToUInt64().ToString());

                while (VirtualQueryEx(mProc.Handle, currentBaseAddress, out memInfo).ToUInt64() != 0 &&
                           currentBaseAddress.ToUInt64() < (ulong)end &&
                           currentBaseAddress.ToUInt64() + (ulong)memInfo.RegionSize >
                           currentBaseAddress.ToUInt64())
                {
                    bool isValid = memInfo.State == MEM_COMMIT;
                    isValid &= memInfo.BaseAddress.ToUInt64() < (ulong)proc_max_address.ToUInt64();
                    isValid &= ((memInfo.Protect & PAGE_GUARD) == 0);
                    isValid &= ((memInfo.Protect & PAGE_NOACCESS) == 0);
                    isValid &= (memInfo.Type == MEM_PRIVATE) || (memInfo.Type == MEM_IMAGE);
                    if (mapped)
                        isValid &= (memInfo.Type == MEM_MAPPED);

                    if (isValid)
                    {
                        bool isReadable = (memInfo.Protect & PAGE_READONLY) > 0;

                        bool isWritable = ((memInfo.Protect & PAGE_READWRITE) > 0) ||
                                          ((memInfo.Protect & PAGE_WRITECOPY) > 0) ||
                                          ((memInfo.Protect & PAGE_EXECUTE_READWRITE) > 0) ||
                                          ((memInfo.Protect & PAGE_EXECUTE_WRITECOPY) > 0);

                        bool isExecutable = ((memInfo.Protect & PAGE_EXECUTE) > 0) ||
                                            ((memInfo.Protect & PAGE_EXECUTE_READ) > 0) ||
                                            ((memInfo.Protect & PAGE_EXECUTE_READWRITE) > 0) ||
                                            ((memInfo.Protect & PAGE_EXECUTE_WRITECOPY) > 0);

                        isReadable &= readable;
                        isWritable &= writable;
                        isExecutable &= executable;

                        isValid &= isReadable || isWritable || isExecutable;
                    }

                    if (!isValid)
                    {
                        currentBaseAddress = new UIntPtr(memInfo.BaseAddress.ToUInt64() + (ulong)memInfo.RegionSize);
                        continue;
                    }

                    MemoryRegionResult memRegion = new MemoryRegionResult
                    {
                        CurrentBaseAddress = currentBaseAddress,
                        RegionSize = memInfo.RegionSize,
                        RegionBase = memInfo.BaseAddress
                    };

                    currentBaseAddress = new UIntPtr(memInfo.BaseAddress.ToUInt64() + (ulong)memInfo.RegionSize);

                    //Console.WriteLine("SCAN start:" + memRegion.RegionBase.ToString() + " end:" + currentBaseAddress.ToString());

                    if (memRegionList.Count > 0)
                    {
                        var previousRegion = memRegionList[memRegionList.Count - 1];

                        if ((long)previousRegion.RegionBase + previousRegion.RegionSize == (long)memInfo.BaseAddress)
                        {
                            memRegionList[memRegionList.Count - 1] = new MemoryRegionResult
                            {
                                CurrentBaseAddress = previousRegion.CurrentBaseAddress,
                                RegionBase = previousRegion.RegionBase,
                                RegionSize = previousRegion.RegionSize + memInfo.RegionSize
                            };

                            continue;
                        }
                    }

                    memRegionList.Add(memRegion);
                }

                ConcurrentBag<long> bagResult = new ConcurrentBag<long>();

                Parallel.ForEach(memRegionList,
                                 (item, parallelLoopState, index) =>
                                 {
                                     long[] compareResults = CompareScan(item, aobPattern, mask);

                                     foreach (long result in compareResults)
                                         bagResult.Add(result);
                                 });

                Debug.WriteLine("[DEBUG] memory scan completed. (time:" + DateTime.Now.ToString("h:mm:ss tt") + ")");

                return bagResult.ToList().OrderBy(c => c).AsEnumerable();
            });
        }


        public async Task<long> AoBScan(string code, long end, string search, string file = "")
        {
            long start = (long)GetCode(code, file).ToUInt64();

            return (await AoBScan(start, end, search, true, true, true, false, file)).FirstOrDefault();
        }

        private long[] CompareScan(MemoryRegionResult item, byte[] aobPattern, byte[] mask)
        {
            if (mask.Length != aobPattern.Length)
                throw new ArgumentException($"{nameof(aobPattern)}.Length != {nameof(mask)}.Length");

            IntPtr buffer = Marshal.AllocHGlobal((int)item.RegionSize);

            ReadProcessMemory(mProc.Handle, item.CurrentBaseAddress, buffer, (UIntPtr)item.RegionSize, out ulong bytesRead);

            int result = 0 - aobPattern.Length;
            List<long> ret = new List<long>();
            unsafe
            {
                do
                {

                    result = FindPattern((byte*)buffer.ToPointer(), (int)bytesRead, aobPattern, mask, result + aobPattern.Length);

                    if (result >= 0)
                        ret.Add((long)item.CurrentBaseAddress + result);

                } while (result != -1);
            }

            Marshal.FreeHGlobal(buffer);

            return ret.ToArray();
        }

        private int FindPattern(byte[] body, byte[] pattern, byte[] masks, int start = 0)
        {
            int foundIndex = -1;

            if (body.Length <= 0 || pattern.Length <= 0 || start > body.Length - pattern.Length ||
                pattern.Length > body.Length) return foundIndex;

            for (int index = start; index <= body.Length - pattern.Length; index++)
            {
                if (((body[index] & masks[0]) == (pattern[0] & masks[0])))
                {
                    var match = true;
                    for (int index2 = 1; index2 <= pattern.Length - 1; index2++)
                    {
                        if ((body[index + index2] & masks[index2]) == (pattern[index2] & masks[index2])) continue;
                        match = false;
                        break;

                    }

                    if (!match) continue;

                    foundIndex = index;
                    break;
                }
            }

            return foundIndex;
        }

        private unsafe int FindPattern(byte* body, int bodyLength, byte[] pattern, byte[] masks, int start = 0)
        {
            int foundIndex = -1;

            if (bodyLength <= 0 || pattern.Length <= 0 || start > bodyLength - pattern.Length ||
                pattern.Length > bodyLength) return foundIndex;

            for (int index = start; index <= bodyLength - pattern.Length; index++)
            {
                if (((body[index] & masks[0]) == (pattern[0] & masks[0])))
                {
                    var match = true;
                    for (int index2 = pattern.Length - 1; index2 >= 1; index2--)
                    {
                        if ((body[index + index2] & masks[index2]) == (pattern[index2] & masks[index2])) continue;
                        match = false;
                        break;

                    }

                    if (!match) continue;

                    foundIndex = index;
                    break;
                }
            }

            return foundIndex;
        }
    }
    public partial class Evelyn
    {

        public string CutString(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if (c >= ' ' && c <= '~')
                    sb.Append(c);
                else
                    break;
            }
            return sb.ToString();
        }


        public byte[] ReadBytes(string code, long length, string file = "")
        {
            byte[] memory = new byte[length];
            UIntPtr theCode = GetCode(code, file);
            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return null;

            if (!ReadProcessMemory(mProc.Handle, theCode, memory, (UIntPtr)length, IntPtr.Zero))
                return null;

            return memory;
        }


        public float ReadFloat(string code, string file = "", bool round = true)
        {
            byte[] memory = new byte[4];

            UIntPtr theCode = GetCode(code, file);
            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            try
            {
                if (ReadProcessMemory(mProc.Handle, theCode, memory, (UIntPtr)4, IntPtr.Zero))
                {
                    float address = BitConverter.ToSingle(memory, 0);
                    float returnValue = (float)address;
                    if (round)
                        returnValue = (float)Math.Round(address, 2);
                    return returnValue;
                }
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }


        public string ReadString(string code, string file = "", int length = 32, bool zeroTerminated = true, System.Text.Encoding stringEncoding = null)
        {
            if (stringEncoding == null)
                stringEncoding = System.Text.Encoding.UTF8;

            byte[] memoryNormal = new byte[length];
            UIntPtr theCode = GetCode(code, file);
            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return "";

            if (ReadProcessMemory(mProc.Handle, theCode, memoryNormal, (UIntPtr)length, IntPtr.Zero))
                return (zeroTerminated) ? stringEncoding.GetString(memoryNormal).Split('\0')[0] : stringEncoding.GetString(memoryNormal);
            else
                return "";
        }


        public double ReadDouble(string code, string file = "", bool round = true)
        {
            byte[] memory = new byte[8];

            UIntPtr theCode = GetCode(code, file);
            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            try
            {
                if (ReadProcessMemory(mProc.Handle, theCode, memory, (UIntPtr)8, IntPtr.Zero))
                {
                    double address = BitConverter.ToDouble(memory, 0);
                    double returnValue = (double)address;
                    if (round)
                        returnValue = (double)Math.Round(address, 2);
                    return returnValue;
                }
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        public int ReadUIntPtr(UIntPtr code)
        {
            byte[] memory = new byte[4];
            if (ReadProcessMemory(mProc.Handle, code, memory, (UIntPtr)4, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            else
                return 0;
        }


        public int ReadInt(string code, string file = "")
        {
            byte[] memory = new byte[4];
            UIntPtr theCode = GetCode(code, file);
            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            if (ReadProcessMemory(mProc.Handle, theCode, memory, (UIntPtr)4, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            else
                return 0;
        }


        public long ReadLong(string code, string file = "")
        {
            byte[] memory = new byte[16];
            UIntPtr theCode = GetCode(code, file);

            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            if (ReadProcessMemory(mProc.Handle, theCode, memory, (UIntPtr)8, IntPtr.Zero))
                return BitConverter.ToInt64(memory, 0);
            else
                return 0;
        }


        public UInt32 ReadUInt(string code, string file = "")
        {
            byte[] memory = new byte[4];
            UIntPtr theCode = GetCode(code, file);
            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            if (ReadProcessMemory(mProc.Handle, theCode, memory, (UIntPtr)4, IntPtr.Zero))
                return BitConverter.ToUInt32(memory, 0);
            else
                return 0;
        }


        public int Read2ByteMove(string code, int moveQty, string file = "")
        {
            byte[] memory = new byte[4];
            UIntPtr theCode = GetCode(code, file);
            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            UIntPtr newCode = UIntPtr.Add(theCode, moveQty);

            if (ReadProcessMemory(mProc.Handle, newCode, memory, (UIntPtr)2, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            else
                return 0;
        }


        public int ReadIntMove(string code, int moveQty, string file = "")
        {
            byte[] memory = new byte[4];
            UIntPtr theCode = GetCode(code, file);
            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            UIntPtr newCode = UIntPtr.Add(theCode, moveQty);

            if (ReadProcessMemory(mProc.Handle, newCode, memory, (UIntPtr)4, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            else
                return 0;
        }


        public ulong ReadUIntMove(string code, int moveQty, string file = "")
        {
            byte[] memory = new byte[8];
            UIntPtr theCode = GetCode(code, file, 8);
            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            UIntPtr newCode = UIntPtr.Add(theCode, moveQty);

            if (ReadProcessMemory(mProc.Handle, newCode, memory, (UIntPtr)8, IntPtr.Zero))
                return BitConverter.ToUInt64(memory, 0);
            else
                return 0;
        }


        public int Read2Byte(string code, string file = "")
        {
            byte[] memoryTiny = new byte[4];

            UIntPtr theCode = GetCode(code, file);
            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            if (ReadProcessMemory(mProc.Handle, theCode, memoryTiny, (UIntPtr)2, IntPtr.Zero))
                return BitConverter.ToInt32(memoryTiny, 0);
            else
                return 0;
        }


        public int ReadByte(string code, string file = "")
        {
            byte[] memoryTiny = new byte[1];

            UIntPtr theCode = GetCode(code, file);
            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return 0;

            if (ReadProcessMemory(mProc.Handle, theCode, memoryTiny, (UIntPtr)1, IntPtr.Zero))
                return memoryTiny[0];

            return 0;
        }


        public bool[] ReadBits(string code, string file = "")
        {
            byte[] buf = new byte[1];

            UIntPtr theCode = GetCode(code, file);

            bool[] ret = new bool[8];

            if (theCode == null || theCode == UIntPtr.Zero || theCode.ToUInt64() < 0x10000)
                return ret;

            if (!ReadProcessMemory(mProc.Handle, theCode, buf, (UIntPtr)1, IntPtr.Zero))
                return ret;


            if (!BitConverter.IsLittleEndian)
                throw new Exception("Should be little endian");

            for (var i = 0; i < 8; i++)
                ret[i] = Convert.ToBoolean(buf[0] & (1 << i));

            return ret;

        }


        public T ReadMemory<T>(string address, string file = "")
        {
            object ReadOutput = null;

            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.String:
                    ReadOutput = ReadString(address, file);
                    break;
                case TypeCode.Int32:
                    ReadOutput = ReadInt(address, file);
                    break;
                case TypeCode.Int64:
                    ReadOutput = ReadLong(address, file);
                    break;
                case TypeCode.Byte:
                    ReadOutput = ReadByte(address, file);
                    break;
                case TypeCode.Double:
                    ReadOutput = ReadDouble(address, file);
                    break;
                case TypeCode.Decimal:
                    ReadOutput = ReadFloat(address, file);
                    break;
                case TypeCode.UInt32:
                    ReadOutput = ReadUInt(address, file);
                    break;
                default:
                    break;
            }

            if (ReadOutput != null)
                return (T)Convert.ChangeType(ReadOutput, typeof(T));
            else
                return default(T);
        }

        ConcurrentDictionary<string, CancellationTokenSource> ReadTokenSrcs = new ConcurrentDictionary<string, CancellationTokenSource>();

        public void BindToUI<T>(string address, Action<string> UIObject, string file = "")
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            if (ReadTokenSrcs.ContainsKey(address))
            {
                try
                {
                    ReadTokenSrcs[address].Cancel();
                    ReadTokenSrcs.TryRemove(address, out _);
                }
                catch
                {
                    Debug.WriteLine("ERROR: Avoided a crash. Address " + address + " was not bound.");
                }
            }
            else
            {
                Debug.WriteLine("Adding Bound Address " + address);
            }

            ReadTokenSrcs.TryAdd(address, cts);

            Task.Factory.StartNew(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    UIObject(ReadMemory<T>(address, file).ToString());
                    Thread.Sleep(100);
                }
            },
            cts.Token);
        }
    }
    public partial class Evelyn
    {
        public Proc mProc = new Proc();

        public UIntPtr VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer)
        {
            UIntPtr retVal;

            // TODO: Need to change this to only check once.
            if (mProc.Is64Bit || IntPtr.Size == 8)
            {
                // 64 bit
                MEMORY_BASIC_INFORMATION64 tmp64 = new MEMORY_BASIC_INFORMATION64();
                retVal = Native_VirtualQueryEx(hProcess, lpAddress, out tmp64, new UIntPtr((uint)Marshal.SizeOf(tmp64)));

                lpBuffer.BaseAddress = tmp64.BaseAddress;
                lpBuffer.AllocationBase = tmp64.AllocationBase;
                lpBuffer.AllocationProtect = tmp64.AllocationProtect;
                lpBuffer.RegionSize = (long)tmp64.RegionSize;
                lpBuffer.State = tmp64.State;
                lpBuffer.Protect = tmp64.Protect;
                lpBuffer.Type = tmp64.Type;

                return retVal;
            }
            MEMORY_BASIC_INFORMATION32 tmp32 = new MEMORY_BASIC_INFORMATION32();

            retVal = Native_VirtualQueryEx(hProcess, lpAddress, out tmp32, new UIntPtr((uint)Marshal.SizeOf(tmp32)));

            lpBuffer.BaseAddress = tmp32.BaseAddress;
            lpBuffer.AllocationBase = tmp32.AllocationBase;
            lpBuffer.AllocationProtect = tmp32.AllocationProtect;
            lpBuffer.RegionSize = tmp32.RegionSize;
            lpBuffer.State = tmp32.State;
            lpBuffer.Protect = tmp32.Protect;
            lpBuffer.Type = tmp32.Type;

            return retVal;
        }


        public bool OpenProcess(int pid, out string FailReason)
        {
            /*if (!IsAdmin())
            {
              
            }*/

            if (pid <= 0)
            {
                FailReason = "OpenProcess given proc ID 0.";
                Debug.WriteLine("ERROR: OpenProcess given proc ID 0.");
                return false;
            }


            if (mProc.Process != null && mProc.Process.Id == pid)
            {
                FailReason = "mProc.Process is null";
                return true;
            }

            try
            {
                mProc.Process = Process.GetProcessById(pid);

                if (mProc.Process != null && !mProc.Process.Responding)
                {
                    Debug.WriteLine("ERROR: OpenProcess: Process is not responding or null.");
                    FailReason = "Process is not responding or null.";
                    return false;
                }

                mProc.Handle = Imps.OpenProcess(0x1F0FFF, true, pid);

                try
                {
                    Process.EnterDebugMode();
                }
                catch (Win32Exception)
                {

                }

                if (mProc.Handle == IntPtr.Zero)
                {
                    var eCode = Marshal.GetLastWin32Error();
                    Debug.WriteLine("ERROR: OpenProcess has failed opening a handle to the target process (GetLastWin32ErrorCode: " + eCode + ")");
                    Process.LeaveDebugMode();
                    mProc = null;
                    FailReason = "failed opening a handle to the target process(GetLastWin32ErrorCode: " + eCode + ")";
                    return false;
                }

                // Lets set the process to 64bit or not here (cuts down on api calls)
                mProc.Is64Bit = Environment.Is64BitOperatingSystem && (IsWow64Process(mProc.Handle, out bool retVal) && !retVal);

                mProc.MainModule = mProc.Process.MainModule;

                //GetModules();

                Debug.WriteLine("Process #" + mProc.Process + " is now open.");
                FailReason = "";
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR: OpenProcess has crashed. " + ex);
                FailReason = "OpenProcess has crashed. " + ex;
                return false;
            }
        }



        public bool OpenProcess(string proc, out string FailReason)
        {
            return OpenProcess(GetProcIdFromName(proc), out FailReason);
        }


        public bool OpenProcess(string proc)
        {
            return OpenProcess(GetProcIdFromName(proc), out string FailReason);
        }


        public bool OpenProcess(int pid)
        {
            return OpenProcess(pid, out string FailReason);
        }

        /*public bool IsAdmin()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            } 
            catch
            {
                Debug.WriteLine("ERROR: Could not determin if program is running as admin. Is the NuGet package \"System.Security.Principal.Windows\" missing?");
                return false;
            }
        }*/

        /// <summary>
        /// Builds the process modules dictionary (names with addresses). Use mProc.Process.Modules instead.
        /// </summary>
        /*public ConcurrentDictionary<string, IntPtr> GetModules()
        {
            if (mProc.Process == null)
            {
                Debug.WriteLine("mProc.Process is null so GetModules failed.");
                return null;
            }

            if (mProc.Is64Bit && IntPtr.Size != 8)
            {
                Debug.WriteLine("WARNING: Game is x64, but your Trainer is x86! You will be missing some modules, change your Trainer's Solution Platform.");
            }
            else if (!mProc.Is64Bit && IntPtr.Size == 8)
            {
                Debug.WriteLine("WARNING: Game is x86, but your Trainer is x64! You will be missing some modules, change your Trainer's Solution Platform.");
            }

            if (mProc.Process.Modules == null)
            {
                Debug.WriteLine("mProc.Process.Modules is null so GetModules failed.");
                return null;
            }

            if (mProc.Modules != null)
                mProc.Modules.Clear();
            else
                mProc.Modules = new ConcurrentDictionary<string, IntPtr>();

            foreach (ProcessModule Module in mProc.Process.Modules)
            {
                if (Module.ModuleName == null || Module.BaseAddress == null)
                    continue;

                if (!string.IsNullOrEmpty(Module.ModuleName) && !mProc.Modules.ContainsKey(Module.ModuleName))
                    mProc.Modules.TryAdd(Module.ModuleName, Module.BaseAddress);
            }

            Debug.WriteLine("Found " + mProc.Modules.Count() + " process modules.");
            return mProc.Modules;
        }*/

        public void SetFocus()
        {
            //int style = GetWindowLong(procs.MainWindowHandle, -16);
            //if ((style & 0x20000000) == 0x20000000) //minimized
            //    SendMessage(procs.Handle, 0x0112, (IntPtr)0xF120, IntPtr.Zero);
            SetForegroundWindow(mProc.Process.MainWindowHandle);
        }

        /// <summary>
        /// Get the process ID number by process name.
        /// </summary>
        /// <param name="name">Example: "eqgame". Use task manager to find the name. Do not include .exe</param>
        /// <returns></returns>
        public int GetProcIdFromName(string name) //new 1.0.2 function
        {
            Process[] processlist = Process.GetProcesses();

            if (name.ToLower().Contains(".exe"))
                name = name.Replace(".exe", "");
            if (name.ToLower().Contains(".bin")) // test
                name = name.Replace(".bin", "");

            foreach (System.Diagnostics.Process theprocess in processlist)
            {
                if (theprocess.ProcessName.Equals(name, StringComparison.CurrentCultureIgnoreCase)) //find (name).exe in the process list (use task manager to find the name)
                    return theprocess.Id;
            }

            return 0; //if we fail to find it
        }



        /// <summary>
        /// Get code. If just the ini file name is given with no path, it will assume the file is next to the executable.
        /// </summary>
        /// <param name="name">label for address or code</param>
        /// <param name="iniFile">path and name of ini file</param>
        /// <returns></returns>
        public string LoadCode(string name, string iniFile)
        {
            StringBuilder returnCode = new StringBuilder(1024);
            uint read_ini_result;

            if (!String.IsNullOrEmpty(iniFile))
            {
                if (File.Exists(iniFile))
                {
                    read_ini_result = GetPrivateProfileString("codes", name, "", returnCode, (uint)returnCode.Capacity, iniFile);
                    //Debug.WriteLine("read_ini_result=" + read_ini_result); number of characters returned
                }
                else
                    Debug.WriteLine("ERROR: ini file \"" + iniFile + "\" not found!");
            }
            else
                returnCode.Append(name);

            return returnCode.ToString();
        }

        private int LoadIntCode(string name, string path)
        {
            try
            {
                int intValue = Convert.ToInt32(LoadCode(name, path), 16);
                if (intValue >= 0)
                    return intValue;
                else
                    return 0;
            }
            catch
            {
                Debug.WriteLine("ERROR: LoadIntCode function crashed!");
                return 0;
            }
        }

        /// <summary>
        /// Make a named pipe (if not already made) and call to a remote function.
        /// </summary>
        /// <param name="func">remote function to call</param>
        /// <param name="name">name of the thread</param>
        public void ThreadStartClient(string func, string name)
        {
            //ManualResetEvent SyncClientServer = (ManualResetEvent)obj;
            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(name))
            {
                if (!pipeStream.IsConnected)
                    pipeStream.Connect();

                //MessageBox.Show("[Client] Pipe connection established");
                using (StreamWriter sw = new StreamWriter(pipeStream))
                {
                    if (!sw.AutoFlush)
                        sw.AutoFlush = true;
                    sw.WriteLine(func);
                }
            }
        }

        // WARNING: Works, but flags as virus
        //#region protection

        //public bool ChangeProtection(string code, MemoryProtection newProtection, out MemoryProtection oldProtection, string file = "")
        //{
        // UIntPtr theCode = GetCode(code, file);
        // if (theCode == UIntPtr.Zero 
        //     || mProc.Handle == IntPtr.Zero)
        // {
        //  oldProtection = default;
        //  return false;
        // }

        // return VirtualProtectEx(mProc.Handle, theCode, (IntPtr)(mProc.Is64Bit ? 8 : 4), newProtection, out oldProtection);
        //}
        //#endregion

        /// <summary>
        /// Convert code from string to real address. If path is not blank, will pull from ini file.
        /// </summary>
        /// <param name="name">label in ini file or code</param>
        /// <param name="path">path to ini file (OPTIONAL)</param>
        /// <param name="size">size of address (default is 8)</param>
        /// <returns></returns>
        public UIntPtr GetCode(string name, string path = "", int size = 8)
        {
            string theCode = "";
            if (mProc == null)
                return UIntPtr.Zero;

            if (mProc.Is64Bit)
            {
                //Debug.WriteLine("Changing to 64bit code...");
                if (size == 8) size = 16; //change to 64bit
                return Get64BitCode(name, path, size); //jump over to 64bit code grab
            }

            if (!String.IsNullOrEmpty(path))
                theCode = LoadCode(name, path);
            else
                theCode = name;

            if (String.IsNullOrEmpty(theCode))
            {
                //Debug.WriteLine("ERROR: LoadCode returned blank. NAME:" + name + " PATH:" + path);
                return UIntPtr.Zero;
            }
            else
            {
                //Debug.WriteLine("Found code=" + theCode + " NAME:" + name + " PATH:" + path);
            }

            // remove spaces
            if (theCode.Contains(" "))
                theCode = theCode.Replace(" ", String.Empty);

            if (!theCode.Contains("+") && !theCode.Contains(","))
            {
                try
                {
                    return new UIntPtr(Convert.ToUInt32(theCode, 16));
                }
                catch
                {
                    Console.WriteLine("Error in GetCode(). Failed to read address " + theCode);
                    return UIntPtr.Zero;
                }
            }

            string newOffsets = theCode;

            if (theCode.Contains("+"))
                newOffsets = theCode.Substring(theCode.IndexOf('+') + 1);

            byte[] memoryAddress = new byte[size];

            if (newOffsets.Contains(','))
            {
                List<int> offsetsList = new List<int>();

                string[] newerOffsets = newOffsets.Split(',');
                foreach (string oldOffsets in newerOffsets)
                {
                    string test = oldOffsets;
                    if (oldOffsets.Contains("0x")) test = oldOffsets.Replace("0x", "");
                    int preParse = 0;
                    if (!oldOffsets.Contains("-"))
                        preParse = Int32.Parse(test, NumberStyles.AllowHexSpecifier);
                    else
                    {
                        test = test.Replace("-", "");
                        preParse = Int32.Parse(test, NumberStyles.AllowHexSpecifier);
                        preParse = preParse * -1;
                    }
                    offsetsList.Add(preParse);
                }
                int[] offsets = offsetsList.ToArray();

                bool mainBase = (theCode.ToLower().Contains("base") || theCode.ToLower().Contains("main")) && !theCode.ToLower().Contains(".dll") && !theCode.ToLower().Contains(".exe");

                if (mainBase)
                    ReadProcessMemory(mProc.Handle, (UIntPtr)((int)mProc.MainModule.BaseAddress + offsets[0]), memoryAddress, (UIntPtr)size, IntPtr.Zero);
                else if (!mainBase && theCode.Contains("+"))
                {
                    string[] moduleName = theCode.Split('+');
                    IntPtr altModule = IntPtr.Zero;
                    if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") && !moduleName[0].ToLower().Contains(".bin"))
                    {
                        string theAddr = moduleName[0];
                        if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                        altModule = (IntPtr)Int32.Parse(theAddr, NumberStyles.HexNumber);
                    }
                    else
                    {
                        try
                        {
                            altModule = GetModuleAddressByName(moduleName[0]);
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                        }
                    }
                    ReadProcessMemory(mProc.Handle, (UIntPtr)((int)altModule + offsets[0]), memoryAddress, (UIntPtr)size, IntPtr.Zero);
                }
                else
                    ReadProcessMemory(mProc.Handle, (UIntPtr)(offsets[0]), memoryAddress, (UIntPtr)size, IntPtr.Zero);

                uint num1 = BitConverter.ToUInt32(memoryAddress, 0); //ToUInt64 causes arithmetic overflow.

                UIntPtr base1 = (UIntPtr)0;

                for (int i = 1; i < offsets.Length; i++)
                {
                    base1 = new UIntPtr(Convert.ToUInt32(num1 + offsets[i]));
                    ReadProcessMemory(mProc.Handle, base1, memoryAddress, (UIntPtr)size, IntPtr.Zero);
                    num1 = BitConverter.ToUInt32(memoryAddress, 0); //ToUInt64 causes arithmetic overflow.
                }
                return base1;
            }
            else // no offsets
            {
                int trueCode = Convert.ToInt32(newOffsets, 16);
                IntPtr altModule = IntPtr.Zero;
                //Debug.WriteLine("newOffsets=" + newOffsets);
                bool mainBase = (theCode.ToLower().Contains("base") || theCode.ToLower().Contains("main")) && !theCode.ToLower().Contains(".dll") && !theCode.ToLower().Contains(".exe");

                if (mainBase)
                    altModule = mProc.MainModule.BaseAddress;
                else if (!mainBase && theCode.Contains("+"))
                {
                    string[] moduleName = theCode.Split('+');
                    if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") && !moduleName[0].ToLower().Contains(".bin"))
                    {
                        string theAddr = moduleName[0];
                        if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                        altModule = (IntPtr)Int32.Parse(theAddr, NumberStyles.HexNumber);
                    }
                    else
                    {
                        try
                        {
                            altModule = GetModuleAddressByName(moduleName[0]);
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                        }
                    }
                }
                else
                    altModule = GetModuleAddressByName(theCode.Split('+')[0]);
                return (UIntPtr)((int)altModule + trueCode);
            }
        }

        /// <summary>
        /// Retrieve mProc.Process module baseaddress by name
        /// </summary>
        /// <param name="name">name of module</param>
        /// <returns></returns>
        public IntPtr GetModuleAddressByName(string name)
        {
            return mProc.Process.Modules.Cast<ProcessModule>().SingleOrDefault(m => string.Equals(m.ModuleName, name, StringComparison.OrdinalIgnoreCase)).BaseAddress;
        }

        /// <summary>
        /// Convert code from string to real address. If path is not blank, will pull from ini file.
        /// </summary>
        /// <param name="name">label in ini file OR code</param>
        /// <param name="path">path to ini file (OPTIONAL)</param>
        /// <param name="size">size of address (default is 16)</param>
        /// <returns></returns>
        public UIntPtr Get64BitCode(string name, string path = "", int size = 16)
        {
            string theCode = "";
            if (!String.IsNullOrEmpty(path))
                theCode = LoadCode(name, path);
            else
                theCode = name;

            if (String.IsNullOrEmpty(theCode))
                return UIntPtr.Zero;

            // remove spaces
            if (theCode.Contains(" "))
                theCode.Replace(" ", String.Empty);

            string newOffsets = theCode;
            if (theCode.Contains("+"))
                newOffsets = theCode.Substring(theCode.IndexOf('+') + 1);

            byte[] memoryAddress = new byte[size];

            if (!theCode.Contains("+") && !theCode.Contains(","))
            {
                try
                {
                    return new UIntPtr(Convert.ToUInt64(theCode, 16));
                }
                catch
                {
                    Console.WriteLine("Error in GetCode(). Failed to read address " + theCode);
                    return UIntPtr.Zero;
                }
            }

            if (newOffsets.Contains(','))
            {
                List<Int64> offsetsList = new List<Int64>();

                string[] newerOffsets = newOffsets.Split(',');
                foreach (string oldOffsets in newerOffsets)
                {
                    string test = oldOffsets;
                    if (oldOffsets.Contains("0x")) test = oldOffsets.Replace("0x", "");
                    Int64 preParse = 0;
                    if (!oldOffsets.Contains("-"))
                        preParse = Int64.Parse(test, NumberStyles.AllowHexSpecifier);
                    else
                    {
                        test = test.Replace("-", "");
                        preParse = Int64.Parse(test, NumberStyles.AllowHexSpecifier);
                        preParse = preParse * -1;
                    }
                    offsetsList.Add(preParse);
                }
                Int64[] offsets = offsetsList.ToArray();

                bool mainBase = (theCode.ToLower().Contains("base") || theCode.ToLower().Contains("main")) && !theCode.ToLower().Contains(".dll") && !theCode.ToLower().Contains(".exe");

                if (mainBase)
                    ReadProcessMemory(mProc.Handle, (UIntPtr)((Int64)mProc.MainModule.BaseAddress + offsets[0]), memoryAddress, (UIntPtr)size, IntPtr.Zero);
                else if (!mainBase && theCode.Contains("+"))
                {
                    string[] moduleName = theCode.Split('+');
                    IntPtr altModule = IntPtr.Zero;
                    if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") && !moduleName[0].ToLower().Contains(".bin"))
                        altModule = (IntPtr)Int64.Parse(moduleName[0], System.Globalization.NumberStyles.HexNumber);
                    else
                    {
                        try
                        {
                            altModule = GetModuleAddressByName(moduleName[0]);
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                        }
                    }
                    ReadProcessMemory(mProc.Handle, (UIntPtr)((Int64)altModule + offsets[0]), memoryAddress, (UIntPtr)size, IntPtr.Zero);
                }
                else // no offsets
                    ReadProcessMemory(mProc.Handle, (UIntPtr)(offsets[0]), memoryAddress, (UIntPtr)size, IntPtr.Zero);

                Int64 num1 = BitConverter.ToInt64(memoryAddress, 0);

                UIntPtr base1 = (UIntPtr)0;

                for (int i = 1; i < offsets.Length; i++)
                {
                    base1 = new UIntPtr(Convert.ToUInt64(num1 + offsets[i]));
                    ReadProcessMemory(mProc.Handle, base1, memoryAddress, (UIntPtr)size, IntPtr.Zero);
                    num1 = BitConverter.ToInt64(memoryAddress, 0);
                }
                return base1;
            }
            else
            {
                Int64 trueCode = Convert.ToInt64(newOffsets, 16);
                IntPtr altModule = IntPtr.Zero;

                bool mainBase = (theCode.ToLower().Contains("base") || theCode.ToLower().Contains("main")) && !theCode.ToLower().Contains(".dll") && !theCode.ToLower().Contains(".exe");

                if (mainBase)
                    altModule = mProc.MainModule.BaseAddress;
                else if (!mainBase && theCode.Contains("+"))
                {
                    string[] moduleName = theCode.Split('+');
                    if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") && !moduleName[0].ToLower().Contains(".bin"))
                    {
                        string theAddr = moduleName[0];
                        if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                        altModule = (IntPtr)Int64.Parse(theAddr, NumberStyles.HexNumber);
                    }
                    else
                    {
                        try
                        {
                            altModule = GetModuleAddressByName(moduleName[0]);
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                        }
                    }
                }
                else
                    altModule = GetModuleAddressByName(theCode.Split('+')[0]);
                return (UIntPtr)((Int64)altModule + trueCode);
            }
        }

        /// <summary>
        /// Close the process when finished.
        /// </summary>
        public void CloseProcess()
        {
            if (mProc.Handle == null)
                return;

            CloseHandle(mProc.Handle);
            mProc = null;
        }

        /// <summary>
        /// Inject a DLL file.
        /// </summary>
        /// <param name="strDllName">path and name of DLL file. Ex: "C:\MyTrainer\inject.dll" or "inject.dll" if the DLL file is in the same directory as the trainer.</param>
        public bool InjectDll(String strDllName)
        {
            IntPtr bytesout;

            if (mProc.Process == null)
            { // check if process is open first
                Debug.WriteLine("Inject failed due to mProc.Process being null. Is the process not open?");
                return false;
            }

            foreach (ProcessModule pm in mProc.Process.Modules)
            {
                if (pm.ModuleName.StartsWith("inject", StringComparison.InvariantCultureIgnoreCase))
                    return false;
            }

            if (!mProc.Process.Responding)
                return false;

            int lenWrite = strDllName.Length + 1;
            UIntPtr allocMem = VirtualAllocEx(mProc.Handle, (UIntPtr)null, (uint)lenWrite, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

            WriteProcessMemory(mProc.Handle, allocMem, strDllName, (UIntPtr)lenWrite, out bytesout);
            UIntPtr GameProc = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (GameProc == null)
                return false;

            IntPtr hThread = CreateRemoteThread(mProc.Handle, (IntPtr)null, 0, GameProc, allocMem, 0, out bytesout);

            int Result = WaitForSingleObject(hThread, 10 * 1000);
            if (Result == 0x00000080L || Result == 0x00000102L)
            {
                if (hThread != null)
                    CloseHandle(hThread);
                return false;
            }
            VirtualFreeEx(mProc.Handle, allocMem, (UIntPtr)0, 0x8000);

            if (hThread != null)
                CloseHandle(hThread);

            return true;
        }

#if WINXP
#else
        /// <summary>
        /// Creates a code cave to write custom opcodes in target process
        /// </summary>
        /// <param name="code">Address to create the trampoline</param>
        /// <param name="newBytes">The opcodes to write in the code cave</param>
        /// <param name="replaceCount">The number of bytes being replaced</param>
        /// <param name="size">size of the allocated region</param>
        /// <param name="file">ini file to look in</param>
        /// <remarks>Please ensure that you use the proper replaceCount
        /// if you replace halfway in an instruction you may cause bad things</remarks>
        /// <returns>UIntPtr to created code cave for use for later deallocation</returns>
        public UIntPtr CreateCodeCave(string code, byte[] newBytes, int replaceCount, int size = 0x1000, string file = "")
        {
            if (replaceCount < 5)
                return UIntPtr.Zero; // returning UIntPtr.Zero instead of throwing an exception
                                     // to better match existing code

            UIntPtr theCode;
            theCode = GetCode(code, file);
            UIntPtr address = theCode;

            // if x64 we need to try to allocate near the address so we dont run into the +-2GB limit of the 0xE9 jmp

            UIntPtr caveAddress = UIntPtr.Zero;
            UIntPtr prefered = address;

            for (var i = 0; i < 10 && caveAddress == UIntPtr.Zero; i++)
            {
                caveAddress = VirtualAllocEx(mProc.Handle, FindFreeBlockForRegion(prefered, (uint)size), (uint)size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

                if (caveAddress == UIntPtr.Zero)
                    prefered = UIntPtr.Add(prefered, 0x10000);
            }

            // Failed to allocate memory around the address we wanted let windows handle it and hope for the best?
            if (caveAddress == UIntPtr.Zero)
                caveAddress = VirtualAllocEx(mProc.Handle, UIntPtr.Zero, (uint)size, MEM_COMMIT | MEM_RESERVE,
                                             PAGE_EXECUTE_READWRITE);

            int nopsNeeded = replaceCount > 5 ? replaceCount - 5 : 0;

            // (to - from - 5)
            int offset = (int)((long)caveAddress - (long)address - 5);

            byte[] jmpBytes = new byte[5 + nopsNeeded];
            jmpBytes[0] = 0xE9;
            BitConverter.GetBytes(offset).CopyTo(jmpBytes, 1);

            for (var i = 5; i < jmpBytes.Length; i++)
            {
                jmpBytes[i] = 0x90;
            }

            byte[] caveBytes = new byte[5 + newBytes.Length];
            offset = (int)(((long)address + jmpBytes.Length) - ((long)caveAddress + newBytes.Length) - 5);

            newBytes.CopyTo(caveBytes, 0);
            caveBytes[newBytes.Length] = 0xE9;
            BitConverter.GetBytes(offset).CopyTo(caveBytes, newBytes.Length + 1);

            WriteBytes(caveAddress, caveBytes);
            WriteBytes(address, jmpBytes);

            return caveAddress;
        }

        private UIntPtr FindFreeBlockForRegion(UIntPtr baseAddress, uint size)
        {
            UIntPtr minAddress = UIntPtr.Subtract(baseAddress, 0x70000000);
            UIntPtr maxAddress = UIntPtr.Add(baseAddress, 0x70000000);

            UIntPtr ret = UIntPtr.Zero;
            UIntPtr tmpAddress = UIntPtr.Zero;

            GetSystemInfo(out SYSTEM_INFO si);

            if (mProc.Is64Bit)
            {
                if ((long)minAddress > (long)si.maximumApplicationAddress ||
                    (long)minAddress < (long)si.minimumApplicationAddress)
                    minAddress = si.minimumApplicationAddress;

                if ((long)maxAddress < (long)si.minimumApplicationAddress ||
                    (long)maxAddress > (long)si.maximumApplicationAddress)
                    maxAddress = si.maximumApplicationAddress;
            }
            else
            {
                minAddress = si.minimumApplicationAddress;
                maxAddress = si.maximumApplicationAddress;
            }

            MEMORY_BASIC_INFORMATION mbi;

            UIntPtr current = minAddress;
            UIntPtr previous = current;

            while (VirtualQueryEx(mProc.Handle, current, out mbi).ToUInt64() != 0)
            {
                if ((long)mbi.BaseAddress > (long)maxAddress)
                    return UIntPtr.Zero;  // No memory found, let windows handle

                if (mbi.State == MEM_FREE && mbi.RegionSize > size)
                {
                    if ((long)mbi.BaseAddress % si.allocationGranularity > 0)
                    {
                        // The whole size can not be used
                        tmpAddress = mbi.BaseAddress;
                        int offset = (int)(si.allocationGranularity -
                                           ((long)tmpAddress % si.allocationGranularity));

                        // Check if there is enough left
                        if ((mbi.RegionSize - offset) >= size)
                        {
                            // yup there is enough
                            tmpAddress = UIntPtr.Add(tmpAddress, offset);

                            if ((long)tmpAddress < (long)baseAddress)
                            {
                                tmpAddress = UIntPtr.Add(tmpAddress, (int)(mbi.RegionSize - offset - size));

                                if ((long)tmpAddress > (long)baseAddress)
                                    tmpAddress = baseAddress;

                                // decrease tmpAddress until its alligned properly
                                tmpAddress = UIntPtr.Subtract(tmpAddress, (int)((long)tmpAddress % si.allocationGranularity));
                            }

                            // if the difference is closer then use that
                            if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)ret - (long)baseAddress))
                                ret = tmpAddress;
                        }
                    }
                    else
                    {
                        tmpAddress = mbi.BaseAddress;

                        if ((long)tmpAddress < (long)baseAddress) // try to get it the cloest possible 
                                                                  // (so to the end of the region - size and
                                                                  // aligned by system allocation granularity)
                        {
                            tmpAddress = UIntPtr.Add(tmpAddress, (int)(mbi.RegionSize - size));

                            if ((long)tmpAddress > (long)baseAddress)
                                tmpAddress = baseAddress;

                            // decrease until aligned properly
                            tmpAddress =
                                UIntPtr.Subtract(tmpAddress, (int)((long)tmpAddress % si.allocationGranularity));
                        }

                        if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)ret - (long)baseAddress))
                            ret = tmpAddress;
                    }
                }

                if (mbi.RegionSize % si.allocationGranularity > 0)
                    mbi.RegionSize += si.allocationGranularity - (mbi.RegionSize % si.allocationGranularity);

                previous = current;
                current = new UIntPtr(((ulong)mbi.BaseAddress) + (ulong)mbi.RegionSize);

                if ((long)current >= (long)maxAddress)
                    return ret;

                if ((long)previous >= (long)current)
                    return ret; // Overflow
            }

            return ret;
        }
#endif

        public static void SuspendProcess(int pid)
        {
            var process = System.Diagnostics.Process.GetProcessById(pid);

            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero)
                    continue;

                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }

        public static void ResumeProcess(int pid)
        {
            var process = System.Diagnostics.Process.GetProcessById(pid);
            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero)
                    continue;

                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);
                CloseHandle(pOpenThread);
            }
        }

#if WINXP
#else
        async Task PutTaskDelay(int delay)
        {
            await Task.Delay(delay);
        }
#endif

        void AppendAllBytes(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Append))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public byte[] FileToBytes(string path, bool dontDelete = false)
        {
            byte[] newArray = File.ReadAllBytes(path);
            if (!dontDelete)
                File.Delete(path);
            return newArray;
        }

        public string MSize()
        {
            if (mProc.Is64Bit)
                return ("x16");
            else
                return ("x8");
        }

        /// <summary>
        /// Convert a byte array to hex values in a string.
        /// </summary>
        /// <param name="ba">your byte array to convert</param>
        /// <returns></returns>
        public static string ByteArrayToHexString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            int i = 1;
            foreach (byte b in ba)
            {
                if (i == 16)
                {
                    hex.AppendFormat("{0:x2}{1}", b, Environment.NewLine);
                    i = 0;
                }
                else
                    hex.AppendFormat("{0:x2} ", b);
                i++;
            }
            return hex.ToString().ToUpper();
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2} ", b);
            }
            return hex.ToString();
        }

        public ulong GetMinAddress()
        {
            SYSTEM_INFO SI;
            GetSystemInfo(out SI);
            return (ulong)SI.minimumApplicationAddress;
        }

        /// <summary>
        /// Dump memory page by page to a dump.dmp file. Can be used with Cheat Engine.
        /// </summary>
        public bool DumpMemory(string file = "dump.dmp")
        {
            Debug.Write("[DEBUG] memory dump starting... (" + DateTime.Now.ToString("h:mm:ss tt") + ")" + Environment.NewLine);
            SYSTEM_INFO sys_info = new SYSTEM_INFO();
            GetSystemInfo(out sys_info);

            UIntPtr proc_min_address = sys_info.minimumApplicationAddress;
            UIntPtr proc_max_address = sys_info.maximumApplicationAddress;

            // saving the values as long ints so I won't have to do a lot of casts later
            Int64 proc_min_address_l = (Int64)proc_min_address; //(Int64)procs.MainModule.BaseAddress;
            Int64 proc_max_address_l = (Int64)mProc.Process.VirtualMemorySize64 + proc_min_address_l;

            //int arrLength = 0;
            if (File.Exists(file))
                File.Delete(file);


            MEMORY_BASIC_INFORMATION memInfo = new MEMORY_BASIC_INFORMATION();
            while (proc_min_address_l < proc_max_address_l)
            {
                VirtualQueryEx(mProc.Handle, proc_min_address, out memInfo);
                byte[] buffer = new byte[(Int64)memInfo.RegionSize];
                UIntPtr test = (UIntPtr)((Int64)memInfo.RegionSize);
                UIntPtr test2 = (UIntPtr)((Int64)memInfo.BaseAddress);

                ReadProcessMemory(mProc.Handle, test2, buffer, test, IntPtr.Zero);

                AppendAllBytes(file, buffer); //due to memory limits, we have to dump it then store it in an array.
                //arrLength += buffer.Length;

                proc_min_address_l += (Int64)memInfo.RegionSize;
                proc_min_address = new UIntPtr((ulong)proc_min_address_l);
            }


            Debug.Write("[DEBUG] memory dump completed. Saving dump file to " + file + ". (" + DateTime.Now.ToString("h:mm:ss tt") + ")" + Environment.NewLine);
            return true;
        }

        /// <summary>
        /// get a list of available threads in opened process
        /// </summary>
        public void GetThreads()
        {
            if (mProc.Process == null)
            {
                Debug.WriteLine("mProc.Process is null so GetThreads failed.");
                return;
            }

            foreach (ProcessThread thd in mProc.Process.Threads)
            {
                Debug.WriteLine("ID:" + thd.Id + " State:" + thd.ThreadState + " Address:" + thd.StartAddress + " Priority:" + thd.PriorityLevel);
            }
        }


        public static IntPtr GetThreadStartAddress(int threadId)
        {
            var hThread = OpenThread(ThreadAccess.QUERY_INFORMATION, false, (uint)threadId);
            if (hThread == IntPtr.Zero)
                throw new Win32Exception();
            var buf = Marshal.AllocHGlobal(IntPtr.Size);
            try
            {
                var result = Imps.NtQueryInformationThread(hThread,
                                 ThreadInfoClass.ThreadQuerySetWin32StartAddress,
                                 buf, IntPtr.Size, IntPtr.Zero);
                if (result != 0)
                    throw new Win32Exception(string.Format("NtQueryInformationThread failed; NTSTATUS = {0:X8}", result));
                return Marshal.ReadIntPtr(buf);
            }
            finally
            {
                CloseHandle(hThread);
                Marshal.FreeHGlobal(buf);
            }
        }

        /// <summary>
        /// suspend a thread by ID
        /// </summary>
        /// <param name="ThreadID">the thread you wish to suspend by ID</param>
        /// <returns></returns>
        public bool SuspendThreadByID(int ThreadID)
        {
            foreach (ProcessThread thd in mProc.Process.Threads)
            {
                if (thd.Id != ThreadID)
                    continue;
                else
                    Debug.WriteLine("Found thread " + ThreadID);

                IntPtr threadHandle = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)ThreadID);

                if (threadHandle == IntPtr.Zero)
                    break;

                if (SuspendThread(threadHandle) == -1)
                {
                    Debug.WriteLine("Thread failed to suspend");
                    CloseHandle(threadHandle);
                    break;
                }
                else
                {
                    Debug.WriteLine("Thread suspended!");
                    CloseHandle(threadHandle);
                    return true;
                }
            }
            return false;
        }

    }
    public class Imps
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            UInt32 dwDesiredAccess,
            bool bInheritHandle,
            Int32 dwProcessId
            );

#if WINXP
#else
        [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
        public static extern UIntPtr Native_VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION32 lpBuffer, UIntPtr dwLength);

        [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
        public static extern UIntPtr Native_VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION64 lpBuffer, UIntPtr dwLength);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();



        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
#endif

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        public static extern int ResumeThread(IntPtr hThread);

        [DllImport("dbghelp.dll")]
        static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            Int32 ProcessId,
            IntPtr hFile,
            MINIDUMP_TYPE DumpType,
            IntPtr ExceptionParam,
            IntPtr UserStreamParam,
            IntPtr CallackParam);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess,
            UIntPtr lpBaseAddress,
            string lpBuffer,
            UIntPtr nSize,
            out IntPtr lpNumberOfBytesWritten
        );

        [DllImport("kernel32.dll")]
        public static extern int GetProcessId(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern uint GetPrivateProfileString(
           string lpAppName,
           string lpKeyName,
           string lpDefault,
           StringBuilder lpReturnedString,
           uint nSize,
           string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(
            IntPtr hProcess,
            UIntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType
            );

        [DllImport("psapi.dll")]
        static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In][MarshalAs(UnmanagedType.U4)] int nSize);
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EnumProcessModules(IntPtr hProcess,
        [Out] IntPtr lphModule,
        uint cb,
        [MarshalAs(UnmanagedType.U4)] out uint lpcbNeeded);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, out ulong lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] IntPtr lpBuffer, UIntPtr nSize, out ulong lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern UIntPtr VirtualAllocEx(
            IntPtr hProcess,
            UIntPtr lpAddress,
            uint dwSize,
            uint flAllocationType,
            uint flProtect
        );

        //[DllImport("kernel32.dll")]
        //public static extern bool VirtualProtectEx(IntPtr hProcess, UIntPtr lpAddress,
        //    IntPtr dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern UIntPtr GetProcAddress(
            IntPtr hModule,
            string procName
        );

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        private static extern bool _CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(
        IntPtr hObject
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(
            string lpModuleName
        );

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        internal static extern Int32 WaitForSingleObject(
            IntPtr handle,
            Int32 milliseconds
        );

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesWritten);

        // Added to avoid casting to UIntPtr
        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32")]
        public static extern IntPtr CreateRemoteThread(
          IntPtr hProcess,
          IntPtr lpThreadAttributes,
          uint dwStackSize,
          UIntPtr lpStartAddress, // raw Pointer into remote process  
          UIntPtr lpParameter,
          uint dwCreationFlags,
          out IntPtr lpThreadId
        );

        [DllImport("kernel32")]
        public static extern bool IsWow64Process(IntPtr hProcess, out bool lpSystemInfo);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr CreateToolhelp32Snapshot([In] UInt32 dwFlags, [In] UInt32 th32ProcessID);

        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern bool Process32First([In] IntPtr hSnapshot, ref PROCESSENTRY32 lppe);
        [DllImport("kernel32.dll")]
        public static extern bool Module32First(IntPtr hSnapshot, ref MODULEENTRY32 lpme);
        [DllImport("kernel32.dll")]
        public static extern bool Module32Next(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern bool Process32Next([In] IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        /*
         typedef NTSTATUS (WINAPI *LPFUN_NtCreateThreadEx)
            (
              OUT PHANDLE hThread,
              IN ACCESS_MASK DesiredAccess,
              IN LPVOID ObjectAttributes,
              IN HANDLE ProcessHandle,
              IN LPTHREAD_START_ROUTINE lpStartAddress,
              IN LPVOID lpParameter,
              IN BOOL CreateSuspended,
              IN ULONG StackZeroBits,
              IN ULONG SizeOfStackCommit,
              IN ULONG SizeOfStackReserve,
              OUT LPVOID lpBytesBuffer
            );
         */
        [DllImport("ntdll.dll", SetLastError = true)]
        internal static extern NTSTATUS NtCreateThreadEx(out IntPtr hProcess, AccessMask desiredAccess, IntPtr objectAttributes, UIntPtr processHandle, IntPtr startAddress, IntPtr parameter, ThreadCreationFlags inCreateSuspended, Int32 stackZeroBits, Int32 sizeOfStack, Int32 maximumStackSize, IntPtr attributeList);

        // privileges
        public const int PROCESS_CREATE_THREAD = 0x0002;
        public const int PROCESS_QUERY_INFORMATION = 0x0400;
        public const int PROCESS_VM_OPERATION = 0x0008;
        public const int PROCESS_VM_WRITE = 0x0020;
        public const int PROCESS_VM_READ = 0x0010;

        // used for memory allocation
        public const uint MEM_FREE = 0x10000;
        public const uint MEM_COMMIT = 0x00001000;
        public const uint MEM_RESERVE = 0x00002000;

        public const uint PAGE_READONLY = 0x02;
        public const uint PAGE_READWRITE = 0x04;
        public const uint PAGE_WRITECOPY = 0x08;
        public const uint PAGE_EXECUTE_READWRITE = 0x40;
        public const uint PAGE_EXECUTE_WRITECOPY = 0x80;
        public const uint PAGE_EXECUTE = 0x10;
        public const uint PAGE_EXECUTE_READ = 0x20;

        public const uint PAGE_GUARD = 0x100;
        public const uint PAGE_NOACCESS = 0x01;

        public const uint MEM_PRIVATE = 0x20000;
        public const uint MEM_IMAGE = 0x1000000;
        public const uint MEM_MAPPED = 0x40000;

        internal enum NTSTATUS
        {
            Success = 0x00
        }

        internal enum AccessMask
        {
            SpecificRightsAll = 0xFFFF,
            StandardRightsAll = 0x1F0000
        }
        internal enum ThreadCreationFlags
        {
            Immediately = 0x0,
            CreateSuspended = 0x01,
            HideFromDebugger = 0x04,
            StackSizeParamIsAReservation = 0x10000
        }

        internal enum MINIDUMP_TYPE
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000
        }

        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public UIntPtr minimumApplicationAddress;
            public UIntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }

        public struct MEMORY_BASIC_INFORMATION32
        {
            public UIntPtr BaseAddress;
            public UIntPtr AllocationBase;
            public uint AllocationProtect;
            public uint RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public struct MEMORY_BASIC_INFORMATION64
        {
            public UIntPtr BaseAddress;
            public UIntPtr AllocationBase;
            public uint AllocationProtect;
            public uint __alignment1;
            public ulong RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
            public uint __alignment2;
        }

        public struct MEMORY_BASIC_INFORMATION
        {
            public UIntPtr BaseAddress;
            public UIntPtr AllocationBase;
            public uint AllocationProtect;
            public long RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [Flags]
        public enum MemoryProtection : uint
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        //inner struct used only internally
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PROCESSENTRY32
        {
            const int MAX_PATH = 260;
            internal UInt32 dwSize;
            internal UInt32 cntUsage;
            internal UInt32 th32ProcessID;
            internal IntPtr th32DefaultHeapID;
            internal UInt32 th32ModuleID;
            internal UInt32 cntThreads;
            internal UInt32 th32ParentProcessID;
            internal Int32 pcPriClassBase;
            internal UInt32 dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            internal string szExeFile;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public struct MODULEENTRY32
        {
            internal uint dwSize;
            internal uint th32ModuleID;
            internal uint th32ProcessID;
            internal uint GlblcntUsage;
            internal uint ProccntUsage;
            internal IntPtr modBaseAddr;
            internal uint modBaseSize;
            internal IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            internal string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            internal string szExePath;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        internal static extern int NtQueryInformationThread(
        IntPtr threadHandle,
        ThreadInfoClass threadInformationClass,
        IntPtr threadInformation,
        int threadInformationLength,
        IntPtr returnLengthPtr);
        public enum ThreadInfoClass : int
        {
            ThreadQuerySetWin32StartAddress = 9
        }


    }
}
