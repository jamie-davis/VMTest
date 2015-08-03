using System.ComponentModel;

namespace VMTest
{
    internal interface IInfoAccess
    {
        void ReportState<T>(TypedVMInfo<T> vm, ReportType reportType) where T : class, INotifyPropertyChanged;
    }
}