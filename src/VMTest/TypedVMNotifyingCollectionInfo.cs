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
    internal class TypedVMNotifyingCollectionInfo<T> : VMInfo where T : class, INotifyCollectionChanged, ICollection
    {
        private object _lock = new object();

        private readonly Output _output;
        private readonly List<VMInfo> _notifyingChildren = new List<VMInfo>();
        private DataErrorInfoMonitor _errorInfoMonitor;

        public TypedVMNotifyingCollectionInfo(Output output, T vm, string name, VMMonitor container, VMInfo parent) : base(container, parent)
        {
            VM = vm;
            Name = name;
            _output = output;
            _errorInfoMonitor = new DataErrorInfoMonitor(output, vm);

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

            _notifyingChildren[index] = new TypedVMInfo<TItem>(_output, item, MakeName(index), Container, Parent)
            {
                VMType = item.GetType()
            };                
        }

        private string MakeName(int index)
        {
            return Name + "[" + index + "]";
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
                    notifyingChild.Name = MakeName(index);
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
            _output.WrapLine("-->{0} Item inserted at [{1}] = ", FullName, index);
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
            _output.WrapLine("-->{0} Item at [{1}] removed = ", FullName, index);
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
            _output.Wrap("-->{0} Item at [{1}] ", FullName, index);
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
            _output.WrapLine("-->{0} Item at [{1}] Moved to [{2}]= ", FullName, oldIndex, newIndex);
        }

        private void InsertBlankChild(object item, int index)
        {
            while (_notifyingChildren.Count < index - 1)
                _notifyingChildren.Add(null);

            _notifyingChildren.Insert(index, null);
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