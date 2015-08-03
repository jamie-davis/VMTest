using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TestConsoleLib;
using VMTest.ObjectReporting;
using VMTest.Utilities;

namespace VMTest
{
    internal class TypedVMInfo<T> : VMInfo where T : class, INotifyPropertyChanged
    {
        private object _lock = new object();

        private readonly Output _output;
        private readonly Dictionary<string, VMInfo> _notifyingChildren = new Dictionary<string, VMInfo>();
        private readonly Dictionary<string, VMInfo> _notifyingCollections = new Dictionary<string, VMInfo>(); 
        private readonly Dictionary<string, VMInfo> _simpleCollections = new Dictionary<string, VMInfo>();
        private DataErrorInfoMonitor _errorInfoMonitor;

        public TypedVMInfo(Output output, T vm, string name, VMMonitor container, VMInfo parent) : base(container, parent)
        {
            VM = vm;
            Name = name;
            _output = output;
            _errorInfoMonitor = new DataErrorInfoMonitor(output, vm);
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
                    CallAttachCollection(prop);
                }
                    
            }
        }

        private void CallAttachChild(PropertyInfo prop)
        {
            if (!typeof (INotifyPropertyChanged).IsAssignableFrom(prop.PropertyType)
                || prop.GetIndexParameters().Any())
                return;

            var method = GetType().GetMethod("Attach", BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(method != null);
            Debug.Assert(method.IsGenericMethodDefinition);

            var typedMethod = method.MakeGenericMethod(prop.PropertyType);
            MethodInvoker.Invoke(typedMethod, this, prop);
        }

        private void CallAttachCollection(PropertyInfo prop)
        {
            MethodInfo method = null;

            var isCollection = typeof(ICollection).IsAssignableFrom(prop.PropertyType);
            var isNotifyingCollection = typeof (INotifyCollectionChanged).IsAssignableFrom(prop.PropertyType);
            if (isNotifyingCollection && isCollection)
            {
                method = GetType().GetMethod("AttachNotifyingCollection", BindingFlags.NonPublic | BindingFlags.Instance);
                Debug.Assert(method != null);
                Debug.Assert(method.IsGenericMethodDefinition);
            }
            else if (isCollection)
            {
                method = GetType().GetMethod("AttachSimpleCollection", BindingFlags.NonPublic | BindingFlags.Instance);
                Debug.Assert(method != null);
                Debug.Assert(method.IsGenericMethodDefinition);
            }

            if (method != null)
            {
                var typedMethod = method.MakeGenericMethod(prop.PropertyType);
                MethodInvoker.Invoke(typedMethod, this, prop);
            }
        }

// ReSharper disable once UnusedMember.Global
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

                    _notifyingChildren[prop.Name] = new TypedVMInfo<TProp>(_output, item, prop.Name, Container, this)
                    {
                        VMType = item.GetType()
                    };
                }
            }
        }

// ReSharper disable once UnusedMember.Global
        internal void AttachNotifyingCollection<TProp>(PropertyInfo prop) where TProp : class, INotifyCollectionChanged, ICollection
        {
            lock (_lock)
            {
                var item = prop.GetValue(VM, null) as TProp;
                if (item != null)
                {
                    VMInfo existing;
                    if (_notifyingCollections.TryGetValue(prop.Name, out existing))
                    {
                        existing.Detach();
                    }

                    _notifyingCollections[prop.Name] = new TypedVMNotifyingCollectionInfo<TProp>(_output, item, prop.Name, Container, this)
                    {
                        VMType = item.GetType()
                    };
                }
            }
        }

// ReSharper disable once UnusedMember.Global
        internal void AttachSimpleCollection<TProp>(PropertyInfo prop) where TProp : class, ICollection
        {
            lock (_lock)
            {
                var item = prop.GetValue(VM, null) as TProp;
                if (item != null)
                {
                    VMInfo existing;
                    if (_simpleCollections.TryGetValue(prop.Name, out existing))
                    {
                        existing.Detach();
                    }

                    _simpleCollections[prop.Name] = new TypedVMCollectionInfo<TProp>(_output, item, prop.Name, Container, this)
                    {
                        VMType = item.GetType()
                    };
                }
            }
        }

        public T VM { get; private set; }

        public override void ReportState(IInfoAccess infoAccess, ReportType reportType)
        {
            infoAccess.ReportState<T>(this, reportType);
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
                VM.PropertyChanged -= OnPropertyChanged;
            }
        }

        public override object GetValue()
        {
            return VM;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            lock (Container)
            {
                if (!ReferenceEquals(sender, VM))
                    return;

                var prop = FindPropertyOnVMType(e.PropertyName);
                if (prop == null)
                {
                    _output.WrapLine("-->{0}.{1} Change event for unknown property.", FullName, e.PropertyName);
                    return;
                }
                    
                VMInfo child;
                if (_notifyingChildren.TryGetValue(e.PropertyName, out child))
                {

                    child.Detach();
                    _notifyingChildren.Remove(e.PropertyName);
                }

                if (_simpleCollections.TryGetValue(e.PropertyName, out child))
                {
                    child.Detach();
                    _simpleCollections.Remove(e.PropertyName);
                }

                if (_notifyingCollections.TryGetValue(e.PropertyName, out child))
                {
                    child.Detach();
                    _notifyingCollections.Remove(e.PropertyName);
                }

                CallAttachChild(prop);
                ReportValue(sender, e, prop);
            }
        }

        private PropertyInfo FindPropertyOnVMType(string propertyName)
        {
            var prop = VMType.GetProperty(propertyName);
            if (prop == null && propertyName.EndsWith("[]"))
                return FindPropertyOnVMType(propertyName.Substring(0, propertyName.Length - 2));
            return prop;
        }

        private void ReportValue(object sender, PropertyChangedEventArgs e, PropertyInfo prop)
        {
            if (prop.GetIndexParameters().Any())
            {
                _output.WrapLine("-->{0}.{1} changed", FullName, e.PropertyName);
                return;
            }

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

    internal class DataErrorInfoMonitor
    {
        public DataErrorInfoMonitor(Output output, object vm)
        {
        }
    }
}