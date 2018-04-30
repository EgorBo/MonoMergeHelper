using System;
using System.IO;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace CorefxImportHelper
{
    public class CandidateItemViewModel : ViewModelBase
    {
        private readonly Action<string> _runExternalDiff;
        private readonly Action<string> _useMe;

        public string MonoPath { get; set; }

        public string OriginalPath { get; set; }

        public CandidateItemViewModel(string monoPath, string originalPath, Action<string> runExternalDiff, Action<string> useMe)
        {
            _runExternalDiff = runExternalDiff;
            _useMe = useMe;
            MonoPath = monoPath;
            OriginalPath = originalPath;
        }

        public string Content
        {
            get => File.ReadAllText(OriginalPath);
            set {}
        }

        public ICommand RunExternalDiff => new RelayCommand(() => _runExternalDiff(OriginalPath));

        public ICommand UseMe => new RelayCommand(() => _useMe(OriginalPath));
    }
}