﻿using Bruuuh;
using MemoryAim2;
using PREMIUM;
using PREMIUM_6._0.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PREMIUM_6._0.Menu.Sniper
{
    public class NewSniperScope
    {
        private static SNIPERSCOPEMEM SNP = new SNIPERSCOPEMEM();
        string SNIPERAOB = "CC 3D 06 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 33 33 13 40 00 00 B0 3F 00 00 80 3F";
        string READ = "0x16";
        string write = "0x06";
        private Dictionary<long, int> originalvalues1 = new Dictionary<long, int>();
        private Dictionary<long, int> originalvalues2 = new Dictionary<long, int>();
        private Dictionary<long, int> originalvalues3 = new Dictionary<long, int>();
        private Dictionary<long, int> originalvalues4 = new Dictionary<long, int>();

        public void SNIPER_ON()
        {
            foreach (var entry in originalvalues1)
            {
                SNP.WriteMemory(entry.Key.ToString("X"), "int", entry.Value.ToString());
            }
            foreach (var entry in originalvalues2)
            {
                SNP.WriteMemory(entry.Key.ToString("X"), "int", entry.Value.ToString());
            }
            Notify("Sniper Scope Enabled", "");
        }

        public void SNIPER_OFF()
        {
            foreach (var entry in originalvalues3)
            {
                SNP.WriteMemory(entry.Key.ToString("X"), "int", entry.Value.ToString());
            }
            foreach (var entry in originalvalues4)
            {
                SNP.WriteMemory(entry.Key.ToString("X"), "int", entry.Value.ToString());
            }
        }
        public async void LoadSniperScope()
        {
            originalvalues1.Clear();
            originalvalues2.Clear();
            originalvalues3.Clear();
            originalvalues4.Clear();
            Notify("Enabling Sniper Scope", "");
            Int64 readOffset = Convert.ToInt64(READ, 16);
            Int64 writeOffset = Convert.ToInt64(write, 16);
            Int32 proc = Process.GetProcessesByName("HD-Player")[0].Id;
            SNP.OpenProcess(proc);
            var result = await SNP.AOB(SNIPERAOB, true, true);
            if (result.Count() != 0)
            {
                foreach (var CurrentAddress in result)
                {
                    Int64 AddressToSave = CurrentAddress + writeOffset;
                    var currentBytes = SNP.readMemory(AddressToSave.ToString("X"), sizeof(int));
                    int currentValue = BitConverter.ToInt32(currentBytes, 0); originalvalues2[AddressToSave] = currentValue;
                    Int64 addressToSave9 = CurrentAddress + readOffset;
                    var currentBytes9 = SNP.readMemory(addressToSave9.ToString("X"), sizeof(int));
                    var currentValue9 = BitConverter.ToInt32(currentBytes9, 0); originalvalues2[addressToSave9] = currentValue9;
                    Int64 headbytes = CurrentAddress + readOffset;
                    Int64 chestbytes = CurrentAddress + writeOffset;
                    var bytes = SNP.readMemory(headbytes.ToString("X"), sizeof(int));
                    int read = BitConverter.ToInt32(bytes, 0);

                    var bytes2 = SNP.readMemory(chestbytes.ToString("X"), sizeof(int));
                    int read2 = BitConverter.ToInt32(bytes2, 0);

                    SNP.WriteMemory(chestbytes.ToString("X"), "int", read.ToString());
                    SNP.WriteMemory(headbytes.ToString("X"), "int", read2.ToString());

                    Int64 addressToSave1 = CurrentAddress + writeOffset;
                    var currentBytes1 = SNP.readMemory(addressToSave1.ToString("X"), sizeof(int));
                    int currentValue1 = BitConverter.ToInt32(currentBytes1, 0); originalvalues4[addressToSave1] = currentValue1;

                    Int64 addressToSave19 = CurrentAddress + readOffset;
                    var currentBytes19 = SNP.readMemory(addressToSave19.ToString("X"), sizeof(int));
                    int currentValue19 = BitConverter.ToInt32(currentBytes19, 0); originalvalues4[addressToSave19] = currentValue19;

                }

                Notify("Sniper Scope Loaded", "");
                SNIPER_ON();
            }
            else
            {
                Notify("Sniper Scope Error","");
            }
        }
        private Home _mainForm;
        Bool Bool = new Bool();
        Evelyn bruuuh = new Evelyn();
        AobMem2 memoryfast = new AobMem2();
        public NewSniperScope(Home mainForm)
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
    }
}
