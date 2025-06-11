using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bruuuh;
using BruuuhPie;
using MemoryAim2;
using PREMIUM_6._0.Views;

namespace PREMIUM_6._0.Menu.Aimbot
{
    public class HeadTracking
    {
        private Home _mainForm;

        public HeadTracking(Home mainForm)
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

        private readonly Bool Bool = new Bool();
        private readonly Evelyn bruuuh = new Evelyn();
        private readonly AobMem2 memoryfast = new AobMem2();

        private List<KeyValuePair<string, string>> aobList = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("3F 00 00 00 00 00 00 C8 42 00 00 A0 40 33 33 13 40 00 00 08 40 00 00 80 3F", "3F 00 00 00 00 00 00 C8 42 00 00 A0 40 33 33 13 40 00 00 08 40 00 00 80 5C"),
            new KeyValuePair<string, string>("3F 00 00 00 00 00 00 A0 42 00 00 C0 3F 33 33 13 40 00 00 F0 3F 00 00 80 3F", "3F 00 00 00 00 00 00 A0 42 00 00 C0 3F 33 33 13 40 00 00 F0 3F 00 00 80 5C"),
            new KeyValuePair<string, string>("3F 00 00 F0 41 00 00 48 42 00 00 00 3F 33 33 13 40 00 00 D0 3F 00 00 80 3F", "3F 00 00 F0 41 00 00 48 42 00 00 00 3F 33 33 13 40 00 00 D0 3F 00 00 80 5C\r\n")
        };

        public void EnableHeadTracking()
        {
            if (Bool.OthersMem == 0)
            {
                OnHeadTracking1();
            }
            else if (Bool.OthersMem > 1)
            {
                OnHeadTracking2();
            }
        }

        private async void OnHeadTracking1()
        {
            Notify("Enabling Head Tracking [0]", "");

            if (!memoryfast.SetProcess(new[] { "HD-Player" }))
            {
                Notify("Head Tracking Failed", "600");
                return;
            }

            bool anySuccess = false;
            int total = aobList.Count;
            int current = 1;

            foreach (var pair in aobList)
            {
                Notify($"Enabling ({current}/{total})", "");

                IEnumerable<long> results = await memoryfast.AoBScan(pair.Key);
                if (results.Any())
                {
                    foreach (long address in results)
                    {
                        memoryfast.AobReplace(address, pair.Value);
                    }
                    anySuccess = true;
                }

                current++;
            }

            if (anySuccess)
                Notify("Head Tracking Enabled [0]", "400");
            else
                Notify("Head Tracking Failed", "600");
        }
        private async void OnHeadTracking2()
        {
            if (Process.GetProcessesByName("HD-Player").Length == 0)
            {
                Notify("Emulator isn't running", "");
                return;
            }

            Notify("Enabling Head Tracking [1]", "");
            bruuuh.OpenProcess("HD-Player");
            bool anySuccess = false;
            int total = aobList.Count;
            int current = 1;

            foreach (var pair in aobList)
            {
                Notify($"Enabling ({current}/{total})", "");

                IEnumerable<long> results = await bruuuh.AoBScan(pair.Key, writable: true);
                if (results.Any())
                {
                    foreach (long address in results)
                    {
                        bruuuh.WriteMemory(address.ToString("X"), "bytes", pair.Value);
                    }
                    anySuccess = true;
                }

                current++;
            }

            if (anySuccess)
                Notify("Head Tracking Enabled [1]", "400");
            else
                Notify("Head Tracking Failed", "600");
        }
    }
}
