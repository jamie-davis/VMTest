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

            public abstract object GetValue();
        }

        internal class TypedVMInfo<T> : VMInfo where T : class, INotifyPropertyChanged
        {
            private object _lock = new object();

            private readonly Output _output;
            private readonly Dictionary<string, VMInfo> _notifyingChildren = new Dictionary<string, VMInfo>();
            private readonly Dictionary<string, VMInfo> _notifyingCollections = new Dictionary<string, VMInfo>(); 
            private readonly Dictionary<string, VMInfo> _simpleCollections = new Dictionary<string, VMInfo>(); 
            
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

                        _notifyingChildren[prop.Name] = new TypedVMInfo<TProp>(_output, item, Name + "." + prop.Name)
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

                        _notifyingCollections[prop.Name] = new TypedVMNotifyingCollectionInfo<TProp>(_output, item, Name + "." + prop.Name)
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

                        _simpleCollections[prop.Name] = new TypedVMCollectionInfo<TProp>(_output, item, Name + "." + prop.Name)
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
                    VM.PropertyChanged -= OnPropertyChanged;
                }
            }

            public override object GetValue()
            {
                return VM;
            }

            private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (!ReferenceEquals(sender, VM))
                    return;

                var prop = FindPropertyOnVMType(e.PropertyName);
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
                }
                ReportValue(sender, e, prop);
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
                    _output.WrapLine("-->{0}.{1} changed", Name, e.PropertyName);
                    return;
                }

                _output.Wrap("-->{0}.{1} = ", Name, e.PropertyName);

                
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

        internal class TypedVMNotifyingCollectionInfo<T> : VMInfo where T : class, INotifyCollectionChanged, ICollection
        {
            private object _lock = new object();

            private readonly Output _output;
            private readonly List<VMInfo> _notifyingChildren = new List<VMInfo>();
            
            public TypedVMNotifyingCollectionInfo(Output output, T vm, string name)
            {
                VM = vm;
                Name = name;
                _output = output;

                vm.CollectionChanged += OnCollectionChanged;

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

                _notifyingChildren[index] = new TypedVMInfo<TItem>(_output, item, Name + "[" + index + "]")
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
                    VM.CollectionChanged -= OnCollectionChanged;
                }
            }

            public override object GetValue()
            {
                return VM;
            }

            private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (!ReferenceEquals(sender, VM))
                    return;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        HandleAddToCollection(e);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        HandleRemoveFromCollection(e);
                        break;
                    
                    case NotifyCollectionChangedAction.Replace:
                        HandleReplaceInCollection(e);
                        break;

                    case NotifyCollectionChangedAction.Move:
                        HandleMoveInCollection(e);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        HandleClearCollection(e);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private void HandleClearCollection(NotifyCollectionChangedEventArgs e)
            {
                _output.WrapLine("-->{0} Cleared.");
                while (_notifyingChildren.Any())
                {
                    var item = _notifyingChildren[0];
                    item.Detach();
                    _notifyingChildren.RemoveAt(0);
                }
            }

            private void HandleMoveInCollection(NotifyCollectionChangedEventArgs e)
            {
                var item = _notifyingChildren[e.OldStartingIndex];
                _notifyingChildren.RemoveAt(e.OldStartingIndex);
                _notifyingChildren.Insert(e.NewStartingIndex, item);
                ReportMovedItem(item, e.OldStartingIndex, e.NewStartingIndex);
                RenumberNotifyingChildren();
            }

            private void RenumberNotifyingChildren()
            {
                var index = 0;
                foreach (var notifyingChild in _notifyingChildren)
                {
                    if (notifyingChild != null)
                        notifyingChild.Name = string.Format("{0}[{1}]", Name, index);
                    ++index;
                }
            }

            private void HandleReplaceInCollection(NotifyCollectionChangedEventArgs e)
            {
                var index = e.NewStartingIndex;
                foreach (var insertedItem in e.NewItems)
                {
                    if (_notifyingChildren.Count > index)
                    {
                        var item = _notifyingChildren[index];
                        if (item != null)
                            item.Detach();
                        CallAttachChild(insertedItem, index);
                        ReportReplacedItem(item, insertedItem, index);
                    }
                    ++index;
                }
            }

            private void HandleRemoveFromCollection(NotifyCollectionChangedEventArgs e)
            {
                var index = e.OldStartingIndex;
                foreach (var removedItem in e.OldItems)
                {
                    if (_notifyingChildren.Count > index)
                    {
                        var item = _notifyingChildren[index];
                        if (item != null)
                           item.Detach();
                        ReportRemovedItem(item, index);
                        _notifyingChildren.RemoveAt(index);
                    }
                    ++index;
                }
                RenumberNotifyingChildren();
            }

            private void HandleAddToCollection(NotifyCollectionChangedEventArgs e)
            {
                var index = e.NewStartingIndex;
                foreach (var item in e.NewItems)
                {
                    InsertBlankChild(item, index);
                    CallAttachChild(item, index);
                    ReportNewItem(item, index);
                    ++index;
                }
                RenumberNotifyingChildren();
            }

            private void ReportNewItem(object item, int index)
            {
                _output.WrapLine("-->{0} Item inserted at [{1}] = ", Name, index);
                if (Type.GetTypeCode(item.GetType()) == TypeCode.Object)
                {
                    ReportObject(item);
                    return;
                }

                _output.WrapLine("{0}", item);
            }

            private void ReportRemovedItem(VMInfo item, int index)
            {
                var value = item == null ? null : item.GetValue();
                _output.WrapLine("-->{0} Item at [{1}] removed = ", Name, index);
                if (value == null)
                {
                    _output.WriteLine();
                }
                else if (Type.GetTypeCode(value.GetType()) == TypeCode.Object)
                {
                    ReportObject(value);
                    return;
                }

                _output.WrapLine("{0}", value);
            }

            private void ReportReplacedItem(VMInfo item, object insertedItem, int index)
            {
                var value = item == null ? null : item.GetValue();
                _output.Wrap("-->{0} Item at [{1}] ", Name, index);
                if (value == null)
                {
                    _output.Wrap("Replaced By : ");
                }
                else if (Type.GetTypeCode(value.GetType()) == TypeCode.Object)
                {
                    _output.Wrap("= ");
                    ReportObject(value);
                    _output.Wrap("Replaced By : ");
                }
                else
                {
                    _output.WrapLine("= {0} Replaced By :", value);
                }

                if (insertedItem == null)
                {
                    _output.WriteLine();
                }
                else if (Type.GetTypeCode(insertedItem.GetType()) == TypeCode.Object)
                {
                    ReportObject(insertedItem);
                }
                else
                {
                    _output.WrapLine("{0}", insertedItem);
                }
            }

            private void ReportMovedItem(VMInfo item, int oldIndex, int newIndex)
            {
                _output.WrapLine("-->{0} Item at [{1}] Moved to [{2}]= ", Name, oldIndex, newIndex);
            }

            private void InsertBlankChild(object item, int index)
            {
                while (_notifyingChildren.Count < index - 1)
                    _notifyingChildren.Add(null);

                _notifyingChildren.Insert(index, null);
            }

            private void ReportValue(object sender, PropertyChangedEventArgs e, PropertyInfo prop)
            {
                _output.Wrap("-->{0}.{1} = ", Name, e.PropertyName);
                
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

        internal class TypedVMCollectionInfo<T> : VMInfo where T : class, ICollection
        {
            private object _lock = new object();

            private readonly Output _output;
            private readonly List<VMInfo> _notifyingChildren = new List<VMInfo>();
            
            public TypedVMCollectionInfo(Output output, T vm, string name)
            {
                VM = vm;
                Name = name;
                _output = output;

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

                _notifyingChildren[index] = new TypedVMInfo<TItem>(_output, item, Name + "[" + index + "]")
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

            private void ReportValue(object sender, PropertyChangedEventArgs e, PropertyInfo prop)
            {
                _output.Wrap("-->{0}.{1} = ", Name, e.PropertyName);
                
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
