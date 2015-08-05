using System.Collections.Generic;
using System.ComponentModel;
using TestConsoleLib;

namespace VMTest
{
    internal class DataErrorInfoMonitor
    {
        private readonly Output _output;
        private readonly VMInfo _info;
        private string _errorString = string.Empty;
        private Dictionary<string, string> _existingError = new Dictionary<string, string>(); 
        public DataErrorInfoMonitor(Output output, VMInfo info, object vm)
        {
            _output = output;
            _info = info;
            if (vm is INotifyPropertyChanged && vm is IDataErrorInfo)
            {
                Notifier = vm as INotifyPropertyChanged;
                DataErrorInfo = vm as IDataErrorInfo;

                Notifier.PropertyChanged += OnPropertyChanged;
            }
        }

        public IDataErrorInfo DataErrorInfo { get; set; }

        public INotifyPropertyChanged Notifier { get; set; }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var errorString = DataErrorInfo.Error ?? string.Empty;
            if (errorString != (_errorString ?? string.Empty) || !string.IsNullOrEmpty(errorString))
            {
                _output.WrapLine("-->{0} Error = \"{1}\"", _info.Name, errorString);
                _errorString = errorString;
            }

            string fieldError;
            _existingError.TryGetValue(e.PropertyName, out fieldError);
            var currentFieldError = DataErrorInfo[e.PropertyName] ?? string.Empty;
            if (!string.IsNullOrEmpty(currentFieldError) || currentFieldError != (fieldError ?? string.Empty))
            {
                _output.WrapLine("-->{0}.{1} Data Error = \"{2}\"", _info.Name, e.PropertyName, currentFieldError);
                _existingError[e.PropertyName] = currentFieldError;
            }
        }
    }
}