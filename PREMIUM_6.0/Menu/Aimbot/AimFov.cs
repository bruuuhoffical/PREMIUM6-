using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bruuuh;
using BruuuhPie;
using MemoryAim2;
using PREMIUM_6._0.Views;

namespace PREMIUM_6._0.Menu.Aimbot
{
    public class AimFov
    {
        private Home _mainForm;
        public AimFov(Home mainForm)
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
        string search = "40 40 00 00 70 42 00 00 00 00 00 00 C0";
        string replace = "FF FF";
        bool k = false;
        Bool Bool = new Bool();
        Evelyn bruuuh = new Evelyn();
        AobMem2 memoryfast = new AobMem2();
        PieMem piemem = new PieMem();

        public void EnableAimfov()
        {
            if (Bool.OthersMem == 0)
            {
                Aimfov1();
            }
            else if (Bool.OthersMem > 1)
            {
                Aimfov2();
            }
        }
        private async void Aimfov1()
        {
            Notify("Enabling Aim Fov", "");
            string[] pocessname = { "HD-Player" };
            bool success = memoryfast.SetProcess(pocessname);

            if (!success)
            {
                return;
            }

            IEnumerable<long> result = await memoryfast.AoBScan(search);

            foreach (long id in result)
            {
                memoryfast.AobReplace(id, replace);
            }
            Notify("Aim Fov Enabled", "");
        }
        public async void Aimfov2()
        {

            if (Process.GetProcessesByName("HD-Player").Length == 0)
            {
                Notify("Emulator Isnt Running", "");

            }
            else
            {
                bruuuh.OpenProcess("HD-Player");
                Notify("Enabling Aim Fov", "");
                int i2 = 22000000;
                IEnumerable<long> wl = await bruuuh.AoBScan(search, writable: true);
                string u = "0x" + wl.FirstOrDefault().ToString("X");
                if (wl.Count() != 0)
                {
                    for (int i = 0; i < wl.Count(); i++)
                    {
                        i2++;
                        bruuuh.WriteMemory(wl.ElementAt(i).ToString("X"), "bytes", replace);
                    }
                    k = true;
                }
                if (k == true)
                {
                    Notify("Enabled Aim Fov", "");

                }
                else
                {
                    Notify("Failed", "");
                }
            }
        }
    }
}
