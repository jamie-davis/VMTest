using System.Collections.ObjectModel;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using NUnit.Framework;
using TestConsoleLib.ObjectReporting;
using VMTest.Tests.TestingUtilities;

namespace VMTest.Tests.AcceptanceTests
{
    [TestFixture]
    [UseReporter(typeof (CustomReporter))]
    public class NotificationsFromAggregateObject
    {
        private VMMonitor _monitor;

        #region Types for test

        class AggregateVM : AcceptanceTestVM
        {
            private ComponentVM _component1;
            private ComponentVM _component2;
            private ObservableCollection<ComponentVM> _components;

            public ComponentVM Component1
            {
                get { return _component1; }
                set
                {
                    if (Equals(value, _component1)) return;
                    _component1 = value;
                    OnPropertyChanged("Component1");
                }
            }

            public ComponentVM Component2
            {
                get { return _component2; }
                set
                {
                    if (Equals(value, _component2)) return;
                    _component2 = value;
                    OnPropertyChanged("Component2");
                }
            }

            public ObservableCollection<ComponentVM> Components
            {
                get { return _components; }
                set
                {
                    if (Equals(value, _components)) return;
                    _components = value;
                    OnPropertyChanged("Components");
                }
            }

            public AggregateVM()
            {
                Component1 = new ComponentVM
                {
                    Name = "Component1",
                    Index = -1
                };

                Component2 = new ComponentVM
                {
                    Name = "Component2",
                    Index = -1
                };

                Components = new ObservableCollection<ComponentVM>();

                var components = new[]
                {
                    "Alpha", "Beta", "Charlie"
                }
                    .Select((a, index) => new ComponentVM
                    {
                        Name = a,
                        Index = index
                    });

                foreach (var component in components)
                {
                    Components.Add(component);
                }
            }
        }

        private class ComponentVM : AcceptanceTestVM
        {
            private string _name;
            private int _index;
            private ObservableCollection<CollectionMember> _members;

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

            public int Index
            {
                get { return _index; }
                set
                {
                    if (value == _index) return;
                    _index = value;
                    OnPropertyChanged("Index");
                }
            }

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

            public ComponentVM()
            {
                Members = new ObservableCollection<CollectionMember>();
                CollectionMember.InitialiseMembersCollection(Members);
            }
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _monitor = new VMMonitor();
        }

        [Test]
        public void NotificationsFromAggregateComponentsHaveCorrectName()
        {
            //Arrange
            var main = new AggregateVM();
            _monitor.Monitor(main, "main", ReportType.NoReport);

            //Act
            main.Component1.Index = 1000;
            main.Component2.Index = 2000;
            main.Components[2].Index = 500;
            main.Components[2].Members[1].Count = 50;

            //Assert
            Approvals.Verify(_monitor.Report);
        }

        [Test]
        public void 
            AggregateComponentNamesChangeCorrectly()
        {
            //Arrange
            var main = new AggregateVM();
            _monitor.Monitor(main, "main", ReportType.NoReport);

            //Act
            main.Components[2].Members[1].Count = 50;
            main.Components.RemoveAt(1);
            main.Components[1].Members[1].Count = 60;

            //Assert
            Approvals.Verify(_monitor.Report);
        }
    }
}