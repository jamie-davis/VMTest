using System.ComponentModel;
using VMTest.Tests.Annotations;

namespace VMTest.Tests.AcceptanceTests
{
    public class AcceptanceTestVM : INotifyPropertyChanged
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