using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using NUnit.Framework;
using TestConsoleLib.ObjectReporting;
using VMTest.Tests.Annotations;
using VMTest.Tests.TestingUtilities;

namespace VMTest.Tests
{
    [TestFixture]
    [UseReporter(typeof (CustomReporter))]
    public class TestVMMonitor
    {
        #region Types for test

        class VM : INotifyPropertyChanged
        {
            private string _text;
            private int _number;

            public string Text
            {
                get { return _text; }
                set
                {
                    if (value == _text) return;
                    _text = value;
                    OnPropertyChanged("Text");
                }
            }

            public int Number
            {
                get { return _number; }
                set
                {
                    if (value == _number) return;
                    _number = value;
                    OnPropertyChanged("Number");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged(string propertyName)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        class VM2 : INotifyPropertyChanged
        {
            private string _text;
            private int _number;
            private VM _vm;

            public string Text
            {
                get { return _text; }
                set
                {
                    if (value == _text) return;
                    _text = value;
                    OnPropertyChanged("Text");
                }
            }

            public int Number
            {
                get { return _number; }
                set
                {
                    if (value == _number) return;
                    _number = value;
                    OnPropertyChanged("Number");
                }
            }

            public VM VM
            {
                get { return _vm; }
                set
                {
                    if (Equals(value, _vm)) return;
                    _vm = value;
                    OnPropertyChanged("VM");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged(string propertyName)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        class VM3 : INotifyPropertyChanged
        {
            private ObservableCollection<VM> _vmCollection;
            private List<VM> _vmList;

            public ObservableCollection<VM> VMCollection
            {
                get { return _vmCollection; }
                set
                {
                    if (Equals(value, _vmCollection)) return;
                    _vmCollection = value;
                    OnPropertyChanged("VMCollection");
                }
            }

            public List<VM> VMList
            {
                get { return _vmList; }
                set
                {
                    if (Equals(value, _vmList)) return;
                    _vmList = value;
                    OnPropertyChanged("VMList");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged(string propertyName)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        class VMIndexPassDown : INotifyPropertyChanged
        {
            private ObservableCollection<IndexedItem> _items;

            public ObservableCollection<IndexedItem> Items
            {
                get { return _items; }
                set
                {
                    if (Equals(value, _items)) return;
                    _items = value;
                    OnPropertyChanged("Items");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged(string propertyName)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }

            public VMIndexPassDown()
            {
                Items = new ObservableCollection<IndexedItem>();
                var itemsToAdd = Enumerable.Range(0, 3)
                    .Select(n => new IndexedItem());
                foreach (var indexedItem in itemsToAdd)
                {
                    Items.Add(indexedItem);
                }
            }
        }

        class IndexedItem : INotifyPropertyChanged
        {
            private VM2 _vm2;
            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged(string propertyName)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }

            public VM2 VM2
            {
                get { return _vm2; }
                set
                {
                    if (Equals(value, _vm2)) return;
                    _vm2 = value;
                    OnPropertyChanged("VM2");
                }
            }

            public IndexedItem()
            {
                VM2 = new VM2
                {
                    Number = 1,
                    Text = "IndexedItem VM2",
                    VM = new VM()
                    {
                        Number = 10,
                        Text = "VM2 Text"
                    }
                };
            }

        }

        public class VMDataErrorInfo : INotifyPropertyChanged, IDataErrorInfo
        {
            private int _a;
            private int _b;
            private int _c;
            private int _d;
            private Dictionary<string, string> _errors = new Dictionary<string, string>();
            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged(string propertyName)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }

            public string this[string columnName]
            {
                get
                {
                    string value = null;
                    _errors.TryGetValue(columnName, out value);
                    return value;
                }
            }

            public string Error { get; private set; }

            public int A
            {
                get { return _a; }
                set
                {
                    if (value == _a) return;
                    _a = value;
                    OnPropertyChanged("A");
                }
            }

            public int B
            {
                get { return _b; }
                set
                {
                    if (value == _b) return;
                    _b = value;
                    OnPropertyChanged("B");
                }
            }

            public int C
            {
                get { return _c; }
                set
                {
                    if (value == _c) return;
                    _c = value;
                    OnPropertyChanged("C");
                }
            }

            public int D
            {
                get { return _d; }
                set
                {
                    if (value == _d) return;
                    _d = value;
                    OnPropertyChanged("D");
                }
            }

            public void SetError(string field, string message)
            {
                _errors[field] = message;
            }

            public void ClassError(string errorForClass)
            {
                Error = errorForClass;
            }
        }

        #endregion

        [Test]
        public void InitialStateIsDisplayed()
        {
            //Arrange
            var vm = new VM
            {
                Text = "Initial text",
                Number = 809
            };

            var vmt = new VMMonitor();

            //Act
            vmt.Monitor(vm, "vm");

            //Assert
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void InitialStateCanBeTable()
        {
            //Arrange
            var vm = new VM
            {
                Text = "Initial text",
                Number = 809
            };

            var vmt = new VMMonitor();

            //Act
            vmt.Monitor(vm, "vm", ReportType.Table);

            //Assert
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void InitialStateCanBePropertyList()
        {
            //Arrange
            var vm = new VM
            {
                Text = "Initial text",
                Number = 809
            };

            var vmt = new VMMonitor();

            //Act
            vmt.Monitor(vm, "vm", ReportType.PropertyList);

            //Assert
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void InitialStateCanBeSuppressed()
        {
            //Arrange
            var vm = new VM
            {
                Text = "Initial text",
                Number = 809
            };

            var vmt = new VMMonitor();

            //Act
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Assert
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void PropertyChangeEventsAreTracked()
        {
            //Arrange
            var vm = new VM
            {
                Text = "Initial text",
                Number = 809
            };

            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm");

            //Act
            vm.Text = "Changed";
            vm.Number = 815;

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void VMStateIsReportedOnRequest()
        {
            //Arrange
            var vm = new VM
            {
                Text = "Initial text",
                Number = 809
            };

            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);
            vm.Text = "Changed";
            vm.Number = 815;

            //Act
            vmt.ReportState(vm);

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test, ExpectedException(typeof(VMNotTrackedException))]
        public void RequestToShowUntrackedVMStateThrows()
        {
            //Arrange
            var vm = new VM
            {
                Text = "Initial text",
                Number = 809
            };

            var vmt = new VMMonitor();

            //Act
            vmt.ReportState(vm);
        }

        [Test]
        public void TextMessagesCanBeAddedToReport()
        {
            //Arrange
            var vm = new VM
            {
                Text = "Initial text",
                Number = 809
            };

            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);
            vm.Text = "Changed";

            //Act
            vmt.WriteLine("About to change number to {0}", 815);
            vm.Number = 815;
            vmt.WriteLine("Done");

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void ChangeNotificationsFromChildObjectsAreReported()
        {
            //Arrange
            var vm = new VM2
            {
                Text = "Initial text",
                Number = 809,
                VM = new VM
                {
                    Text = "Child Object Text",
                    Number = 655
                }
            };

            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);
            vm.Text = "Changed";

            //Act
            vmt.WriteLine("About to change child number to {0}", 815);
            vm.VM.Number = 815;
            vmt.WriteLine("Done");
            vmt.ReportState(vm);

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void MonitorSignsUpForNewObjectWhenChildIsChanged()
        {
            //Arrange
            var vm = new VM2
            {
                Text = "Initial text",
                Number = 809,
                VM = new VM
                {
                    Text = "Child Object Text",
                    Number = 655
                }
            };
            var vm1 = vm.VM;
            var vm2 = new VM
            {
                Text = "Replacement object",
                Number = 10
            };

            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vmt.WriteLine("About to change VM");
            vm.VM = vm2;
            vmt.WriteLine("Done");
            vmt.WriteLine("About to set new VM number to 100");
            vm2.Number = 100;
            vmt.WriteLine("Done");
            vmt.WriteLine("About to change old VM number to 200 - should not show up");
            vm1.Number = 200;
            vmt.WriteLine("Done");

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void MonitorDropsExistingObjectWhenChildIsNulled()
        {
            //Arrange
            var vm = new VM2
            {
                Text = "Initial text",
                Number = 809,
                VM = new VM
                {
                    Text = "Child Object Text",
                    Number = 655
                }
            };
            var vm1 = vm.VM;

            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vmt.WriteLine("About to change VM");
            vm.VM = null;
            vmt.WriteLine("Done");
            vmt.WriteLine("About to change old VM number to 200 - should not show up");
            vm1.Number = 200;
            vmt.WriteLine("Done");

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void MonitorSignsUpForNewObjectWhenNullChildIsSet()
        {
            //Arrange
            var vm = new VM2
            {
                Text = "Initial text",
                Number = 809,
            };
            var vm1 = new VM
            {
                Text = "Child Object Text",
                Number = 655
            };

            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vmt.WriteLine("About to set VM");
            vm.VM = vm1;
            vmt.WriteLine("Done");
            vmt.WriteLine("About to change VM number to 200");
            vm1.Number = 200;
            vmt.WriteLine("Done");

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void MonitorSignsUpForIEnumerableMembers()
        {
            //Arrange
            var item = new VM
            {
                Text = "Child Object Text",
                Number = 655
            };

            var vm = new VM3
            {
                VMList = new List<VM> { item }
            };
 
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm");

            //Act
            vmt.WriteLine("About to change VM list item");
            item.Text = "list item changed";
            vmt.WriteLine("Done");

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void MonitorDropsIEnumerableMembersWhenCollectionReplaced()
        {
            //Arrange
            var item = new VM
            {
                Text = "Child Object Text",
                Number = 655
            };

            var vm = new VM3
            {
                VMList = new List<VM> { item },
            };
 
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vm.VMList = new List<VM>(); //old collection should be detached
            vmt.WriteLine("About to change old VM list item");
            item.Text = "list item changed";
            vmt.WriteLine("Done");

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void MonitorDropsINotifyCollectionChangedMembersWhenCollectionReplaced()
        {
            //Arrange
            var item = new VM
            {
                Text = "Child Object Text",
                Number = 655
            };

            var vm = new VM3
            {
                VMCollection = new ObservableCollection<VM> { item },
            };
 
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vm.VMCollection = new ObservableCollection<VM>(); //old collection should be detached
            vmt.WriteLine("About to change old VM list item");
            item.Text = "list item changed";
            vmt.WriteLine("Done");

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void MonitorSignsUpForItemsAddedToNotifyingCollection()
        {
            //Arrange
            var item = new VM
            {
                Text = "Child Object Text",
                Number = 655
            };

            var vm = new VM3
            {
                VMCollection = new ObservableCollection<VM>(),
            };
 
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vmt.WriteLine("About to add new collection item");
            vm.VMCollection.Add(item);
            vmt.WriteLine("About to update new collection item");
            item.Text = "list item changed";
            vmt.WriteLine("Done");

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void MonitorDropsItemsRemovedFromNotifyingCollection()
        {
            //Arrange
            var item = new VM
            {
                Text = "Child Object Text",
                Number = 655
            };

            var vm = new VM3
            {
                VMCollection = new ObservableCollection<VM>
                {
                    item
                },
            };
 
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vmt.WriteLine("About to remove collection item");
            vm.VMCollection.Remove(item);
            vmt.WriteLine("About to update removed collection item");
            item.Text = "list item changed";
            vmt.WriteLine("Done");

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void MonitorDropsItemsReplacedInNotifyingCollection()
        {
            //Arrange
            var item = new VM
            {
                Text = "Original",
                Number = 1
            };
            var item2 = new VM
            {
                Text = "Replacement",
                Number = 2
            };

            var vm = new VM3
            {
                VMCollection = new ObservableCollection<VM>
                {
                    item
                },
            };
 
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vmt.WriteLine("About to replace collection item");
            vm.VMCollection[0] = item2;
            vmt.WriteLine("About to update replaced collection item");
            item.Text = "original item changed";
            vmt.WriteLine("Done");
            vmt.WriteLine("About to update new collection item");
            item2.Text = "new item changed";
            vmt.WriteLine("Done");

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void MonitorTracksItemsMovedInCollection()
        {
            //Arrange
            var item0 = new VM
            {
                Text = "Item 0",
                Number = 0
            };
            var item1 = new VM
            {
                Text = "Item 1",
                Number = 1
            };
            var item2 = new VM
            {
                Text = "Item 2",
                Number = 2
            };
            var item3 = new VM
            {
                Text = "Item 3",
                Number = 3
            };

            var vm = new VM3
            {
                VMCollection = new ObservableCollection<VM>
                {
                    item0,
                    item1,
                    item2,
                    item3
                },
            };
 
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vmt.WriteLine("About to move item 1 to next position");
            vm.VMCollection.Move(1, 2);
            vmt.WriteLine("Done");
            vmt.WriteLine("About to update item in position 1");
            vm.VMCollection[1].Text = "This is now item 1";
            vmt.WriteLine("Done");
            vmt.WriteLine("About to update item in position 2");
            vm.VMCollection[2].Text = "This is now item 2";
            vmt.WriteLine("Done");
            vmt.WriteLine("New VM state:");
            vmt.ReportState(vm);

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void MonitorUpdatesItemIndexWhenItemsAreAddedToCollection()
        {
            //Arrange
            var alpha = new VM
            {
                Text = "Alpha",
                Number = 0
            };
            var beta = new VM
            {
                Text = "Beta",
                Number = 0
            };
            var charlie = new VM
            {
                Text = "Charlie",
                Number = 0
            };
            var delta = new VM
            {
                Text = "Delta",
                Number = 0
            };

            var vm = new VM3
            {
                VMCollection = new ObservableCollection<VM>
                {
                    alpha,
                    charlie,
                    delta,                },
            };
 
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vmt.WriteLine("About to edit item charlie at position 1");
            charlie.Number++;
            vmt.WriteLine("Done");
            vmt.WriteLine("About to insert item Beta at position 1");
            vm.VMCollection.Insert(1, beta);
            vmt.WriteLine("Done");
            vmt.WriteLine("About to edit item charlie at position 2");
            charlie.Number++;
            vmt.WriteLine("Done");
            vmt.WriteLine("New VM state:");
            vmt.ReportState(vm);

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void MonitorUpdatesItemIndexWhenItemsAreRemovedFromCollection()
        {
            //Arrange
            var alpha = new VM
            {
                Text = "Alpha",
                Number = 0
            };
            var beta = new VM
            {
                Text = "Beta",
                Number = 0
            };
            var charlie = new VM
            {
                Text = "Charlie",
                Number = 0
            };
            var delta = new VM
            {
                Text = "Delta",
                Number = 0
            };

            var vm = new VM3
            {
                VMCollection = new ObservableCollection<VM>
                {
                    alpha,
                    beta,
                    charlie,
                    delta,                },
            };
 
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vmt.WriteLine("About to edit item charlie at position 2");
            charlie.Number++;
            vmt.WriteLine("Done");
            vmt.WriteLine("About to remove item Beta at position 1");
            vm.VMCollection.Remove(beta);
            vmt.WriteLine("Done");
            vmt.WriteLine("About to edit item charlie at position 1");
            charlie.Number++;
            vmt.WriteLine("Done");
            vmt.WriteLine("New VM state:");
            vmt.ReportState(vm);

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void MonitorDropsItemsRemovedFromCollectionByClear()
        {
            //Arrange
            var item0 = new VM
            {
                Text = "Item 0",
                Number = 0
            };
            var item1 = new VM
            {
                Text = "Item 1",
                Number = 1
            };
            var item2 = new VM
            {
                Text = "Item 2",
                Number = 2
            };
            var item3 = new VM
            {
                Text = "Item 3",
                Number = 3
            };

            var vm = new VM3
            {
                VMCollection = new ObservableCollection<VM>
                {
                    item0,
                    item1,
                    item2,
                    item3
                },
            };
 
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vmt.WriteLine("About to clear the collection");
            vm.VMCollection.Clear();
            vmt.WriteLine("Done");
            vmt.WriteLine("About to update each removed item in turn");
            item0.Text = "Updated Item 0";
            item1.Text = "Updated Item 1";
            item2.Text = "Updated Item 2";
            item3.Text = "Updated Item 3";
            vmt.WriteLine("Done");
            vmt.WriteLine("New VM state:");
            vmt.ReportState(vm);

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void IndexChangesArePassedDown()
        {
            //Arrange
            var vm = new VMIndexPassDown();
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vmt.WriteLine("About to modify item 1:");
            vm.Items[1].VM2.VM.Text = "Item 1";
            vmt.WriteLine("Removing item 0:");
            vm.Items.RemoveAt(0);
            vmt.WriteLine("About to modify item 0:");
            vm.Items[0].VM2.VM.Text = "Item 0";

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void DataErrorsAreMonitored()
        {
            //Arrange
            var vm = new VMDataErrorInfo();
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vm.SetError("A", "BAD");
            vm.A = 1;

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void DataErrorsAreAlwaysReported()
        {
            //Arrange
            var vm = new VMDataErrorInfo();
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vm.SetError("A", "BAD");
            vm.A = 1;
            vm.A = 2;

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void DataErrorsAreReportedBlankOnceWhenCleared()
        {
            //Arrange
            var vm = new VMDataErrorInfo();
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vm.SetError("A", "BAD");
            vm.A = 1;
            vm.SetError("A", null);
            vm.A = 2;
            vm.A = 3;

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void FieldDataErrorsAreTrackedIndividually()
        {
            //Arrange
            var vm = new VMDataErrorInfo();
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vm.SetError("A", "BAD A");
            vm.SetError("B", "BAD B");
            vm.A = 1;
            vm.B = 1;
            vm.SetError("A", null);
            vm.A = 2;
            vm.B = 2;
            vm.A = 3;
            vm.B = 3;
            vm.SetError("B", string.Empty);
            vm.A = 4;
            vm.B = 4;
            vm.A = 5;
            vm.B = 5;

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void ErrorMessageIsTracked()
        {
            //Arrange
            var vm = new VMDataErrorInfo();
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vm.ClassError("Error for class");
            vm.A = 1;

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void ErrorMessageIsReportedWhileSet()
        {
            //Arrange
            var vm = new VMDataErrorInfo();
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vm.ClassError("Error for class");
            vm.A = 1;
            vm.B = 1;
            vm.C = 1;

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }

        [Test]
        public void ErrorMessageIsReportedOnceWhenCleared()
        {
            //Arrange
            var vm = new VMDataErrorInfo();
            var vmt = new VMMonitor();
            vmt.Monitor(vm, "vm", ReportType.NoReport);

            //Act
            vm.ClassError("Error for class");
            vm.A = 1;
            vm.B = 1;
            vm.C = 1;
            vm.ClassError(null);
            vm.A = 2;
            vm.B = 2;
            vm.C = 2;

            //Assert
            Console.WriteLine(vmt.Report);
            Approvals.Verify(vmt.Report);
        }
    }
}
