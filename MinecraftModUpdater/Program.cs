﻿using System;
using System.Windows.Forms;

namespace MinecraftModUpdater
{
    public static class Program
    {

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ModUpdaterForm());
        }
    }
}