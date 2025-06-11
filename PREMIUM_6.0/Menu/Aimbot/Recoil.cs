using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bruuuh;
using MemoryAim2;
using PREMIUM_6._0.Views;

namespace PREMIUM_6._0.Menu.Aimbot
{
    public class Recoil
    {
        private Home _mainForm;
        Bool Bool = new Bool();
        Evelyn bruuuh = new Evelyn();
        AobMem2 memoryfast = new AobMem2();
        public Recoil(Home mainForm)
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
        


        string search = "03 0A 9F ED 10 0A 01 EE 00 0A 81 EE 10 0A 10 EE 10 8C BD E8 00 00 7A 44 F0";
        string replace = "03 0A 9F ED 10 0A 01 EE 00 0A 81 EE 10 0A 10 EE 10 8C BD E8 00 00 00 00 F0";
        //string search = "";
        //string replace = "";
        bool k = false;
        public void EnableNoRecoil()
        {
            if (Bool.OthersMem == 0)
            {
                OnNoRecoil1();
            }
            else if (Bool.OthersMem > 1)
            {
                OnNoRecoil2();
            }
        }
        
        public void DisableNoRecoil()
        {
            if (Bool.OthersMem == 0)
            {
                OffNoRecoil1();
            }
            else if (Bool.OthersMem > 1)
            {
                OffNoRecoil2();
            }
        }
        private async void OnNoRecoil1()
        {
            Notify("Enabling No Recoil", "");
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
            Notify("Zero Recoil Enabled", "");
        }
        public async void OnNoRecoil2()
        {

            if (Process.GetProcessesByName("HD-Player").Length == 0)
            {
                Notify("Emulator Isnt Running", "");

            }
            else
            {
                bruuuh.OpenProcess("HD-Player");
                Notify("Enabling Zero Recoil", "");
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
                    Notify("Zero Recoil Enabled", "");

                }
                else
                {
                    Notify("Zero RecoilFailed", "");
                }
            }
        }
        
        private async void OffNoRecoil1()
        {
            Notify("Disabling No Recoil", "");
            string[] pocessname = { "HD-Player" };
            bool success = memoryfast.SetProcess(pocessname);

            if (!success)
            {
                return;
            }

            IEnumerable<long> result = await memoryfast.AoBScan(replace);

            foreach (long id in result)
            {
                memoryfast.AobReplace(id, search);
            }
            Notify("Zero Recoil Disabled", "");
        }
        private async void OffNoRecoil2()
        {

            if (Process.GetProcessesByName("HD-Player").Length == 0)
            {
                Notify("Emulator Isnt Running", "");

            }
            else
            {
                bruuuh.OpenProcess("HD-Player");
                Notify("Disabling Zero Recoil", "");
                int i2 = 22000000;
                IEnumerable<long> wl = await bruuuh.AoBScan(replace, writable: true);
                string u = "0x" + wl.FirstOrDefault().ToString("X");
                if (wl.Count() != 0)
                {
                    for (int i = 0; i < wl.Count(); i++)
                    {
                        i2++;
                        bruuuh.WriteMemory(wl.ElementAt(i).ToString("X"), "bytes", search);
                    }
                    k = true;
                }
                if (k == true)
                {
                    Notify("Zero Recoil Disabled", "");

                }
                else
                {
                    Notify("Zero Recoil Failed", "");
                }
            }
        }

    }
}
