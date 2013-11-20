using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TimeTrack
{
    class ToggleRecordingCommand: ICommand
    {
        public bool CanExecute(object parameter)
        {
            return (parameter is MainWindow);
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            var w = parameter as MainWindow;
            w.ToggleRecording();
        }
    }
}
