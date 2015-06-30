using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using TestConsoleLib;
using VMTest.ObjectReporting;
using VMTest.Utilities;

namespace VMTest
{
    internal interface IInfoAccess
    {
        void ReportState<T>(VMMonitor.TypedVMInfo<T> vm, ReportType reportType) where T : class, INotifyPropertyChanged;
    }

    public class VMMonitor : IInfoAccess
    {
        abstract internal class VMInfo
        {
            public INotifyPropertyChanged Notifications { get; set; }
            public string Name { get; set; }
            public Type VMType { get; set; }

            public abstract void ReportState(IInfoAccess infoAccess, ReportType reportType);
            public abstract void Detach();
        }

        internal class TypedVMInfo<T> : VMInfo where T : class, INotifyPropertyChanged
        {
            private object _lock = new object();

            private readonly Output _output;
            private readonly Dictionary<string, VMInfo> _notifyingChildren = new Dictionary<string, VMInfo>(); 
            
            public TypedVMInfo(Output output, T vm, string name)
            {
                VM = vm;
                Name = name;
                _output = output;

                vm.PropertyChanged += OnPropertyChanged;

                SignUpForChildNotifications();
            }

            private void SignUpForChildNotifications()
            {
                lock (_lock)
                {
                    foreach (var prop in typeof (T).GetProperties())
                    {
                        CallAttachChild(prop);
                    }
                    
                }
            }

            private void CallAttachChild(PropertyInfo prop)
            {
                if (!typeof (INotifyPropertyChanged).IsAssignableFrom(prop.PropertyType))
                    return;

               var method = GetType().GetMethod("Attach", BindingFlags.NonPublic | BindingFlags.Instance);
                Debug.Assert(method != null);
                Debug.Assert(method.IsGenericMethodDefinition);

                var typedMethod = method.MakeGenericMethod(prop.PropertyType);
                MethodInvoker.Invoke(typedMethod, this, prop);
            }

            internal void Attach<TProp>(PropertyInfo prop) where TProp : class, INotifyPropertyChanged
            {
                lock (_lock)
                {
                    var item = prop.GetValue(VM, null) as TProp;
                    if (item != null)
                    {
                        VMInfo existing;
                        if (_notifyingChildren.TryGetValue(prop.Name, out existing))
                        {
                            existing.Detach();
                        }

                        _notifyingChildren[prop.Name] = new TypedVMInfo<TProp>(_output, item, Name + "." + prop.Name)
                        {
                            VMType = item.GetType()
                        };
                    }
                }
            }

            public T VM { get; private set; }
            public override void ReportState(IInfoAccess infoAccess, ReportType reportType)
            {
                infoAccess.ReportState(this, reportType);
            }

            public override void Detach()
            {
                lock (_lock)
                {
                    foreach (var child in _notifyingChildren)
                    {
                        child.Value.Detach();
                    }
                    _notifyingChildren.Clear();
                }
            }

            private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (!ReferenceEquals(sender, VM))
                    return;

                var prop = VMType.GetProperty(e.PropertyName);
                if (prop == null)
                {
                    _output.WrapLine("-->{0}.{1} Change event for unknown property.", Name, e.PropertyName);
                    return;
                }

                lock (_lock)
                {
                    VMInfo child;
                    if (_notifyingChildren.TryGetValue(e.PropertyName, out child))
                    {
                        child.Detach();
                        _notifyingChildren.Remove(e.PropertyName);
                    }

                    CallAttachChild(prop);
               }
                _output.WrapLine("-->{0}.{1} = {2}", Name, e.PropertyName, prop.GetValue(sender, null));
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

        public void Monitor<T>(T vm, string name, ReportType initialReportType = ReportType.Default) where T: class, INotifyPropertyChanged
        {
            var info = TrackVM(vm, name);
            DisplayVM(info, initialReportType, ReportType.Table, "Accepted view model \"{0}\":");
        }

        private TypedVMInfo<T> TrackVM<T>(T vm, string name) where T : class, INotifyPropertyChanged
        {
            var vmInfo = new TypedVMInfo<T>(_output, vm, name)
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
                    _output.WrapLine(heading, vmInfo.Name);
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
