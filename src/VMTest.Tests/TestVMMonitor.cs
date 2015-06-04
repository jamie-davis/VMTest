using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ApprovalTests;
using ApprovalTests.Reporters;
using NUnit.Framework;
using VMTest.Tests.Annotations;
using VMTest.Tests.TestingUtilities;

namespace VMTest.Tests
{
    [TestFixture]
    [UseReporter(typeof (CustomReporter))]
    public class TestVMMonitor
    {
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
                    OnPropertyChanged();
                }
            }

            public int Number
            {
                get { return _number; }
                set
                {
                    if (value == _number) return;
                    _number = value;
                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

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

    }
}
