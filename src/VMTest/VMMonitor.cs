using System.Collections.Generic;
using System.ComponentModel;
using TestConsoleLib;
using TestConsoleLib.ObjectReporting;
using VMTest.Utilities;

namespace VMTest
{
    public class VMMonitor : IInfoAccess
    {
        private readonly Output _output;
        private readonly Dictionary<object, VMInfo> _vms = new Dictionary<object, VMInfo>(); 
        
        public string Report
        {
            get { return _output.Report; }
        }

        public VMMonitor()
        {
            _output = new Output();
        }

        public void Monitor<T>(T vm, string name, ReportType initialReportType = ReportType.Default) where T: class, INotifyPropertyChanged
        {
            var info = TrackVM(vm, name);
            DisplayVM(info, initialReportType, ReportType.Table, "Accepted view model \"{0}\":");
        }

        private TypedVMInfo<T> TrackVM<T>(T vm, string name) where T : class, INotifyPropertyChanged
        {
            var vmInfo = new TypedVMInfo<T>(_output, vm, name, this, null)
            {
                Notifications = vm, 
                VMType = vm.GetType()
            };
            _vms.Add(vm, vmInfo);

            return vmInfo;
        }

        private void DisplayVM<T>(TypedVMInfo<T> vmInfo, ReportType reportType, ReportType defaultReportType, string heading = null) where T : class, INotifyPropertyChanged
        {
            if (reportType == ReportType.Default)
                reportType = defaultReportType;

            if (reportType != ReportType.NoReport)
            {
                if (heading != null)
                {
                    _output.WrapLine(heading, vmInfo.FullName);
                    _output.WriteLine();
                }

                var reporter = new ObjectReporter<T>(reportType);
                reporter.Report(vmInfo.VM, _output);
                _output.WriteLine();
            }
        }

        public void ReportState<T>(T vm, ReportType reportType = ReportType.Default) where T : INotifyPropertyChanged
        {
            VMInfo info;
            if (!_vms.TryGetValue(vm, out info))
            {
                throw new VMNotTrackedException();
            }
            info.ReportState(this, reportType);
        }

        void IInfoAccess.ReportState<T>(TypedVMInfo<T> vm, ReportType reportType)
        {
            _output.WriteLine();
            DisplayVM(vm, reportType, ReportType.Table, "VM \"{0}\" state:");
        }

        public void WriteLine(string formatString, params object[] args)
        {
            _output.WrapLine(formatString, args);
        }

        public void WriteLine(string message)
        {
            _output.WrapLine(message);
        }
    }
}
