using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using MemoryAim2;
using PREMIUM_6._0.Views;
using Red;
using System.Windows.Forms;


namespace PREMIUM_6._0.Menu.Aimbot
{
    public class AimbotHead
    {
        Bool Bool = new Bool();
        private Home _mainForm;
        public AimbotHead(Home mainForm)
        {
            _mainForm = mainForm;

        }
        private void Notify(string message, string message1)
        {
            _mainForm.Invoke(new Action(() =>
            {
                _mainForm.Notify(message, message1);
            }));
        }
        private string RED;
        public static MemRed RedLib = new MemRed();
        private static BRUUUHAIMBOTMEM bruuuhaimmem = new BRUUUHAIMBOTMEM();
        public static String PID;
        private int _processId;

        #region AimbotHead
        //string aimbotaob = "00 00 A5 43 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 BF ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 80 BF";
        string aimbotaob = "FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 A5 43";
        string aimchest = "0xAA";
        string aimchest1 = "AA";
        string aimhead = "0xA6";
        string aimhead1 = "A6";
        long startRange = 0x0000000000010000;
        long stopRange = 0x00007ffffffeffff;
        bool writable = true;
        bool executable = true;
        private List<long> Type1Address = new List<long>();
        private List<long> Type2Address = new List<long>();
        private Dictionary<long, long> HeadValuesV11 = new Dictionary<long, long>();
        private Dictionary<long, long> HeadValuesV12 = new Dictionary<long, long>();
        private Dictionary<long, long> HeadValuesV13 = new Dictionary<long, long>();
        private Dictionary<long, long> HeadValuesV14 = new Dictionary<long, long>();
        

        public void EnableAimbotHead()
        {
            if (Bool.AimbotMem == 0)
            {
                var processes = Process.GetProcessesByName("HD-Player");
                if (processes.Length == 0)
                {
                    Notify("Emulator Isnt Running", "");
                }
                else
                {
                    EnableAimbot();
                }
            }
            else if (Bool.AimbotMem == 1)
            {
                var processes = Process.GetProcessesByName("HD-Player");
                if (processes.Length == 0)
                {
                    Notify("Emulator Isnt Running", "");
                }
                else
                {
                    EnableAimbot2();
                }
            }
            else if (Bool.AimbotMem == 2)
            {
                var processes = Process.GetProcessesByName("HD-Player");
                if (processes.Length == 0)
                {
                    Notify("Emulator Isnt Running", "");
                }
                else
                {
                }
            }
        }
        public void EnableAimbot()
        {
            var processes = Process.GetProcessesByName("HD-Player");
            if (processes.Length == 0)
            {
                Notify("Emulator Isnt Running", "");
            }
            _processId = processes[0].Id;
            RedLib.OpenProcess(_processId);
            Notify("Enabling Aimbot Head [0]", "");
            var task = RedLib.AoBScan(
                startRange,
                stopRange,
                aimbotaob,
                writable,
                executable
            );

            task.Wait();
            var result = task.Result;

            if (result == null || !result.Any())
            {
                Notify("Aimbot Head Error [0]", "");
                Bool.AimbotHead = false;
                return;
            }

            Type1Address = result.ToList();
            Notify("Load Complete [0]...", "");
            Notify("Applying Offsets [0]...", "");
            ApplyOffsets1();
            Bool.AimbotHead = true;
        }

        public void ApplyOffsets1()
        {
            int delay = Bool.AimbotDelay * 1000;

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = delay;
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                timer.Dispose();
                foreach (long address in Type1Address)
                {
                    byte[] numArray = RedLib.ReadMemory((address + 170L).ToString("X"), 4);
                    RedLib.WriteMemory((address + 166L).ToString("X"), "int", BitConverter.ToInt32(numArray, 0).ToString());
                }
                Notify("Aimbot Head Enabled [0]", "400");
            };
            timer.Start();
        }


        public async void EnableAimbot2()
        {
            Notify("Enabling Aimbot [1]", "");
            long readOffset = Convert.ToInt64(aimchest1, 16);
            long writeOffset = Convert.ToInt64(aimhead1, 16);
            int delay = Bool.AimbotDelay * 1000;

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

            int proc = Process.GetProcessesByName("HD-Player")[0].Id;
            bruuuhaimmem.OpenProcess(proc);

            var result = await bruuuhaimmem.AoBScan2(aimbotaob, true, true);

            if (result.Count() != 0)
            {
                foreach (var CurrentAddress in result)
                {
                    Notify("Almost Done [1]","");
                    timer.Interval = delay;
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        timer.Dispose();
                        long addressToSave = CurrentAddress + writeOffset;
                        long addressToSave9 = CurrentAddress + readOffset;

                        var currentBytes = bruuuhaimmem.readMemory(addressToSave.ToString("X"), sizeof(long));
                        HeadValuesV11[addressToSave] = BitConverter.ToInt64(currentBytes, 0);

                        var currentBytes9 = bruuuhaimmem.readMemory(addressToSave9.ToString("X"), sizeof(long));
                        HeadValuesV11[addressToSave9] = BitConverter.ToInt64(currentBytes9, 0);

                        bruuuhaimmem.WriteMemory(addressToSave9.ToString("X"), "long", HeadValuesV11[addressToSave].ToString());
                        bruuuhaimmem.WriteMemory(addressToSave.ToString("X"), "long", HeadValuesV11[addressToSave9].ToString());

                        var currentBytes19 = bruuuhaimmem.readMemory(addressToSave9.ToString("X"), sizeof(long));
                        HeadValuesV12[addressToSave9] = BitConverter.ToInt64(currentBytes19, 0);
                    };
                    timer.Start();
                }

                Notify("Aimbot Enabled [1]", "400");
            }
            else
            {
                Notify("Aimbot Failed [1]", "800");
            }
        }

        private void DelayTest()
        {
            int delay = Bool.AimbotDelay * 1000;

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = delay;
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                timer.Dispose();
                Console.Beep();
            };

            timer.Start();
        }
        #endregion
    }
}
