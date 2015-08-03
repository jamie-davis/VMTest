using System;
using System.ComponentModel;

namespace VMTest
{
    abstract internal class VMInfo
    {
        protected VMMonitor Container { get; set; }
        public INotifyPropertyChanged Notifications { get; set; }

        public string Name { get; set; }

        protected VMInfo Parent { get; private set; }

        public string FullName
        {
            get
            {
                if (Parent == null)
                    return Name;

                return String.Format("{0}.{1}", Parent.FullName, Name);
            }
        }

        public Type VMType { get; set; }

        public abstract void ReportState(IInfoAccess infoAccess, ReportType reportType);
        public abstract void Detach();

        public abstract object GetValue();

        protected VMInfo(VMMonitor container, VMInfo parent)
        {
            Container = container;
            Parent = parent;
        }
    }
}