using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemoryAim2;
using PREMIUM_6._0.Views;
using Red;

namespace PREMIUM_6._0.Menu.Aimbot
{
    public class AimbotDrag
    {
        private Home _mainForm;
        public AimbotDrag(Home mainForm)
        {
            _mainForm = mainForm;

        }
        private void Notify(string message , string message1)
        {
            _mainForm.Invoke(new Action(() =>
            {
                _mainForm.Notify(message, message1);
            }));
        }
        Bool Bool = new Bool();

        private string RED;
        public static MemRed RedLib = new MemRed();
        private static BRUUUHAIMBOTMEM bruuuhaimmem = new BRUUUHAIMBOTMEM();
        public static String PID;
        private int _processId;

        #region AimbotDrag
        string aimbotaob = "A5 43 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 ?? ?? ?? ?? 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 80 BF 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ??";
        string aimchest = "0x5E";
        string aimchest1 = "5E";
        string aimDrag = "0x5A";
        string aimDrag1 = "5A";
        long startRange = 0x0000000000010000;
        long stopRange = 0x00007ffffffeffff;
        bool writable = true;
        bool executable = true;
        private List<long> Type1Address = new List<long>();
        private List<long> Type2Address = new List<long>();
        private Dictionary<long, long> DragValuesV11 = new Dictionary<long, long>();
        private Dictionary<long, long> DragValuesV12 = new Dictionary<long, long>();
        private Dictionary<long, long> DragValuesV13 = new Dictionary<long, long>();
        private Dictionary<long, long> DragValuesV14 = new Dictionary<long, long>();

        public void EnableAimbotDrag()
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
            else if (Bool.AimbotMem > 0)
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
            Notify("Enabling Aimbot Drag [0]", "");
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
                Notify("Aimbot Drag Error [0]", "");
                Bool.AimbotDrag = false;
                return;
            }

            Type1Address = result.ToList();
            Notify("Load Complete [0]...", "");
            Notify("Applying Offsets [0]...", "");
            ApplyOffsets1();
            Bool.AimbotDrag = true;
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
                    byte[] numArray = RedLib.ReadMemory((address + 94L).ToString("X"), 4);
                    RedLib.WriteMemory((address + 90L).ToString("X"), "int", BitConverter.ToInt32(numArray, 0).ToString());
                }
                Notify("Aimbot Drag Enabled", "");
                Bool.AimbotDrag = true;
            };
            timer.Start();
        }


        public async void EnableAimbot2()
        {
            Notify("Enabling Aimbot Drag [1]", "");
            long readOffset = Convert.ToInt64(aimchest1, 16);
            long writeOffset = Convert.ToInt64(aimDrag1, 16);
            int delay = Bool.AimbotDelay * 1000;

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

            int proc = Process.GetProcessesByName("HD-Player")[0].Id;
            bruuuhaimmem.OpenProcess(proc);

            var result = await bruuuhaimmem.AoBScan2(aimbotaob, true, true);

            if (result.Count() != 0)
            {
                foreach (var CurrentAddress in result)
                {
                    Notify("Almost Done [1]", "");
                    timer.Interval = delay;
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        timer.Dispose();
                        long addressToSave = CurrentAddress + writeOffset;
                        long addressToSave9 = CurrentAddress + readOffset;

                        var currentBytes = bruuuhaimmem.readMemory(addressToSave.ToString("X"), sizeof(long));
                        DragValuesV11[addressToSave] = BitConverter.ToInt64(currentBytes, 0);

                        var currentBytes9 = bruuuhaimmem.readMemory(addressToSave9.ToString("X"), sizeof(long));
                        DragValuesV11[addressToSave9] = BitConverter.ToInt64(currentBytes9, 0);

                        bruuuhaimmem.WriteMemory(addressToSave9.ToString("X"), "long", DragValuesV11[addressToSave].ToString());
                        bruuuhaimmem.WriteMemory(addressToSave.ToString("X"), "long", DragValuesV11[addressToSave9].ToString());

                        var currentBytes19 = bruuuhaimmem.readMemory(addressToSave9.ToString("X"), sizeof(long));
                        DragValuesV12[addressToSave9] = BitConverter.ToInt64(currentBytes19, 0);
                    };
                    timer.Start();
                }
                Bool.AimbotDrag = true;
                Notify("Aimbot Drag Enabled [1]", "400");
            }
            else
            {
                Notify("Aimbot Drag Failed [1]", "800");
                Bool.AimbotDrag = false;
            }
        }
        #endregion
    }
}
