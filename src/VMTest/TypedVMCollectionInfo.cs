using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using TestConsoleLib;
using TestConsoleLib.ObjectReporting;
using VMTest.Utilities;

namespace VMTest
{
    internal class TypedVMCollectionInfo<T> : VMInfo where T : class, ICollection
    {
        private object _lock = new object();

        private readonly Output _output;
        private readonly List<VMInfo> _notifyingChildren = new List<VMInfo>();
        private DataErrorInfoMonitor _errorInfoMonitor;

        public TypedVMCollectionInfo(Output output, T vm, string name, VMMonitor container, VMInfo parent) : base(container, parent)
        {
            VM = vm;
            Name = name;
            _output = output;
            _errorInfoMonitor = new DataErrorInfoMonitor(output, this, vm);

            SignUpForChildNotifications();
        }

        private void SignUpForChildNotifications()
        {
            lock (_lock)
            {
                var index = 0;
                foreach (var item in VM)
                {
                    CallAttachChild(item, index++);
                }                
            }
        }

        private void CallAttachChild(object item, int index)
        {
            var method = GetType().GetMethod("Attach", BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(method != null);
            Debug.Assert(method.IsGenericMethodDefinition);

            var typedMethod = method.MakeGenericMethod(item.GetType());
            MethodInvoker.Invoke(typedMethod, this, item, index);
        }

        private void CallAttachCollection(PropertyInfo prop)
        {
            if (!typeof (INotifyCollectionChanged).IsAssignableFrom(prop.PropertyType))
                return;

            var method = GetType().GetMethod("AttachCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(method != null);
            Debug.Assert(method.IsGenericMethodDefinition);

            var typedMethod = method.MakeGenericMethod(prop.PropertyType);
            MethodInvoker.Invoke(typedMethod, this, prop);
        }

// ReSharper disable once UnusedMember.Global
        internal void Attach<TITem>(TITem item, int index) where TITem : class
        {
            lock (_lock)
            {
                if (item != null)
                {
                    if (_notifyingChildren.Count > index)
                    {
                        var existing = _notifyingChildren[index];
                        if (existing != null)
                            existing.Detach();
                    }

                    if (item as INotifyPropertyChanged == null)
                    {
                        _notifyingChildren[index] = null;
                    }
                    else
                    {
                        var method = GetType().GetMethod("AddChild", BindingFlags.NonPublic | BindingFlags.Instance);
                        Debug.Assert(method != null);
                        var typedMethod = method.MakeGenericMethod(item.GetType());
                        MethodInvoker.Invoke(typedMethod, this, index, item);
                    }
                }
            }
        }

        internal void AddChild<TItem>(int index, TItem item) where TItem : class, INotifyPropertyChanged
        {
            while (_notifyingChildren.Count <= index)
            {
                _notifyingChildren.Add(null);
            }

            _notifyingChildren[index] = new TypedVMInfo<TItem>(_output, item, Name + "[" + index + "]", Container, Parent)
            {
                VMType = item.GetType()
            };                
        }

        public T VM { get; private set; }

        public override void ReportState(IInfoAccess infoAccess, ReportType reportType)
        {
            throw new NotImplementedException();
//                infoAccess.ReportState(this, reportType);
        }

        public override void Detach()
        {
            lock (_lock)
            {
                foreach (var child in _notifyingChildren)
                {
                    child.Detach();
                }
                _notifyingChildren.Clear();
            }
        }

        public override object GetValue()
        {
            return VM;
        }

        private string MakeName(int index)
        {
            return FullName + "[" + index + "]";
        }

        private void ReportValue(object sender, PropertyChangedEventArgs e, PropertyInfo prop)
        {
            _output.Wrap("-->{0}.{1} = ", FullName, e.PropertyName);
                
            var value = prop.GetValue(sender, null);

            if (value != null && Type.GetTypeCode(value.GetType()) == TypeCode.Object)
            {
                _output.WriteLine();
                ReportObject(value);
                return;
            }

            _output.WrapLine("{0}", value);
        }

        private void ReportObject(object value)
        {
            var reporterFn = GetType()
                .GetMethod("ReportObjectWithType", BindingFlags.NonPublic | BindingFlags.Instance);
            var typedFn = reporterFn.MakeGenericMethod(value.GetType());

            _output.WriteLine();
            typedFn.Invoke(this, new [] {value});
            _output.WriteLine();
        }

        internal void ReportObjectWithType<TValue>(TValue value)
        {
            var reporter = new ObjectReporter<TValue>();
            reporter.Report(value, _output);
        }
    }
}