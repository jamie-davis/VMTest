using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TestConsole.OutputFormatting;
using TestConsoleLib;

namespace VMTest
{
    internal interface IInfoAccess
    {
        void ReportState<T>(VMMonitor.TypedVMInfo<T> vm, ReportType reportType) where T : INotifyPropertyChanged;
    }

    public class VMMonitor : IInfoAccess
    {

        abstract internal class VMInfo
        {
            public INotifyPropertyChanged Notifications { get; set; }
            public string Name { get; set; }
            public Type VMType { get; set; }

            public abstract void ReportState(IInfoAccess infoAccess, ReportType reportType);
        }

        internal class TypedVMInfo<T> : VMInfo where T : INotifyPropertyChanged
        {
            public T VM { get; set; }
            public override void ReportState(IInfoAccess infoAccess, ReportType reportType)
            {
                infoAccess.ReportState(this, reportType);
            }
        }

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

        public void Monitor<T>(T vm, string name, ReportType initialReportType = ReportType.Default) where T: INotifyPropertyChanged
        {
            var info = TrackVM(vm, name);
            DisplayVM(info, initialReportType, ReportType.Table, "Accepted view model \"{0}\":");
        }

        private TypedVMInfo<T> TrackVM<T>(T vm, string name) where T : INotifyPropertyChanged
        {
            var vmInfo = new TypedVMInfo<T>
            {
                VM = vm,
                Notifications = vm, 
                Name = name,
                VMType = vm.GetType()
            };
            _vms.Add(vm, vmInfo);
            vm.PropertyChanged += OnPropertyChanged;

            return vmInfo;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            VMInfo vmInfo;
            if (!_vms.TryGetValue(sender, out vmInfo))
                return;

            var prop = vmInfo.VMType.GetProperty(e.PropertyName);
            if (prop == null)
            {
                _output.WrapLine("-->{0}.{1} Change event for unknown property.", vmInfo.Name, e.PropertyName);
                return;
            }

            _output.WrapLine("-->{0}.{1} = {2}", vmInfo.Name, e.PropertyName, prop.GetValue(sender));
        }

        private void DisplayVM<T>(TypedVMInfo<T> vmInfo, ReportType reportType, ReportType defaultReportType, string heading = null) where T : INotifyPropertyChanged
        {
            if (reportType != ReportType.NoReport)
            {
                if (heading != null)
                {
                    _output.WrapLine(heading, vmInfo.Name);
                    _output.WriteLine();
                }

                switch (reportType == ReportType.Default ? defaultReportType : reportType)
                {
                    case ReportType.PropertyList:
                        ListProperties(vmInfo.VM);
                        break;

                    case ReportType.Table:
                        _output.FormatTable(new[] { vmInfo.VM });
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("reportType");
                }

                _output.WriteLine();
            }
        }

        private void ListProperties(INotifyPropertyChanged vm)
        {
            var vmType = vm.GetType();
            var props = vmType.GetProperties().Select(p => new {Property = p.Name, Value = p.GetValue(vm)});
            _output.FormatTable(props, ReportFormattingOptions.OmitHeadings);
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
