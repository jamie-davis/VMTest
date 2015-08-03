using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VMTest.Tests;

namespace ErrorTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static TestVMMonitor.VMDataErrorInfo VM = new TestVMMonitor.VMDataErrorInfo();
    }
}
