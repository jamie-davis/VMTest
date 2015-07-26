using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using NUnit.Framework;
using TestConsoleLib;
using VMTest.Tests.Annotations;
using VMTest.Tests.TestingUtilities;

namespace VMTest.Tests.AcceptanceTests
{
    [TestFixture]
    [UseReporter(typeof (CustomReporter))]
    public class BaselinePropertyAndCollectionNotifications
    {
        private VMMonitor _monitor;

        #region Types for test

        class CollectionMember : AcceptanceTestVM
        {
            private string _name;
            private int _count;

            public string Name
            {
                get { return _name; }
                set
                {
                    if (value == _name) return;
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }

            public int Count
            {
                get { return _count; }
                set
                {
                    if (value == _count) return;
                    _count = value;
                    OnPropertyChanged("Count");
                }
            }
        }

        class MainVM : AcceptanceTestVM
        {
            private ObservableCollection<CollectionMember> _members;

            public ObservableCollection<CollectionMember> Members
            {
                get { return _members; }
                set
                {
                    if (Equals(value, _members)) return;
                    _members = value;
                    OnPropertyChanged("Members");
                }
            }

            public void AddMember(string name, int count)
            {
                Members.Add(new CollectionMember
                {
                    Name = name,
                    Count = count
                });
            }

            public MainVM()
            {
                Members = new ObservableCollection<CollectionMember>();
                var members = new[]
                {
                    "Alpha", "Beta", "Charlie",
                    "Delta", "Echo", "Hotel",
                    "Indigo", "Juliette", "Kilo",
                    "Lima", "Mike", "November",
                    "Oscar", "Papa", "Quebec",
                    "Romeo", "Sierra", "Tango",
                    "Uniform", "Victor", "Whiskey",
                    "X-Ray", "Zulu"
                }
                .Select(n => new CollectionMember
                    {Name = n, Count = n.Length});
                foreach (var member in members)
                {
                    Members.Add(member);
                }
            }
        }
        #endregion

        [SetUp]
        public void SetUp()
        {
            _monitor = new VMMonitor();
        }

        [Test]
        public void DefaultReportIsFormatted()
        {
            //Act
            _monitor.Monitor(new MainVM(), "main");

            //Assert
            Approvals.Verify(_monitor.Report);
        }

        [Test]
        public void CollectionMemberUpdatesAreReported()
        {
            //Arrange
            var main = new MainVM();
            _monitor.Monitor(main, "main");

            //Act
            var x = 0;
            foreach (var member in main.Members)
            {
                member.Count = x++;
            }
            _monitor.ReportState(main);

            //Assert
            Approvals.Verify(_monitor.Report);
        }

        [Test]
        public void CollectionMemberUpdatesReportCorrectIndex()
        {
            //Arrange
            _monitor.WriteLine("First this test will remove items and update the last item in the Members collection.");
            _monitor.WriteLine("Second, it will replace the removed items and again, update the last item in the Members collection.");
            _monitor.WriteLine("Each last item update will set the item's Count to the index of the last item. This should match the reported index in all cases.");

            var main = new MainVM();
            _monitor.Monitor(main, "main");

            //Act
            var removed = new List<CollectionMember>();
            for (var x = 0; x < 5; ++x)
            {
                var member = main.Members[main.Members.Count - 1];
                member.Count = main.Members.Count - 1;

                var removeThisTime = main.Members[0];
                removed.Add(removeThisTime);
                main.Members.Remove(removeThisTime);
            }

            _monitor.ReportState(main);

            foreach (var restore in removed)
            {
                main.Members.Insert(0, restore);
                var member = main.Members[main.Members.Count - 1];
                member.Count = main.Members.Count - 1;
            }

            _monitor.ReportState(main);

            //Assert
            Approvals.Verify(_monitor.Report);
        }
    }

    internal class AcceptanceTestVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
