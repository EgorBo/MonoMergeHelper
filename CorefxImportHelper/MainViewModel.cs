using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;

namespace CorefxImportHelper
{
    public class MainViewModel : ViewModelBase
    {
        private ObservableCollection<SourceItemViewModel> _sourceItems = new ObservableCollection<SourceItemViewModel>();
        private string _selectedRootFile;
        private SourceItemViewModel _selectedSourceItem;

        public MainViewModel()
        {
            //SelectedRootFile = @"C:\prj\mono\mono-master\mcs\class\corlib\corlib.dll.sources";
        }

        public List<string> AllCorefxAndCorertFiles { get; private set; }

        public string MonoRootFolder { get; private set; }

        public ObservableCollection<SourceItemViewModel> SourceItems
        {
            get => _sourceItems;
            set => Set(nameof(SourceItems), ref _sourceItems, value);
        }

        public ICommand Browse => new RelayCommand(OnBrowseSourcesFile);

        public string SelectedRootFile
        {
            get => _selectedRootFile;
            set
            {
                Set(nameof(SelectedRootFile), ref _selectedRootFile, value);
                RootFileContentHistory = new Stack<List<string>>();
                DumpRootFile();
                ReloadSourceFiles();
            }
        }

        public SourceItemViewModel SelectedSourceItem
        {
            get => _selectedSourceItem;
            set
            {
                Set(nameof(SelectedSourceItem), ref _selectedSourceItem, value);
                if (SelectedSourceItem != null)
                    SelectedSourceItem.SelectedCandidateItem = SelectedSourceItem.Candidates.FirstOrDefault();
            }
        }

        void ReloadSourceFiles()
        {
            if (string.IsNullOrEmpty(SelectedRootFile) || !File.Exists(SelectedRootFile))
            {
                SourceItems.Clear();
                return;
            }

            MonoRootFolder = SelectedRootFile.GetMonoRootPath();
            AllCorefxAndCorertFiles = Directory.GetFiles(Path.Combine(MonoRootFolder, "external", "corefx"), "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Path.Combine(MonoRootFolder, "external", "corert"), "*.cs", SearchOption.AllDirectories))
                .ToList();

            var fileItems = File.ReadAllLines(SelectedRootFile).ToList();
            var items = fileItems.Select(i => new SourceItemViewModel(i, this));
            SourceItems = new ObservableCollection<SourceItemViewModel>(items);
        }

        void OnBrowseSourcesFile()
        {
            var dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            dlg.Filter = "Mono sources|*.sources";
            if (dlg.ShowDialog() == true)
            {
                SelectedRootFile = dlg.FileName;
            }
        }

        public Stack<List<string>> RootFileContentHistory { get; private set; }

        public void DumpRootFile()
        {
            RootFileContentHistory.Push(File.ReadAllLines(SelectedRootFile).ToList());
        }

        public void UndoLastAction()
        {
            List<string> content;

            if (RootFileContentHistory.Count > 1)
                RootFileContentHistory.Pop();

            if (RootFileContentHistory.Count > 1)
                content = RootFileContentHistory.Pop();
            else
                content = RootFileContentHistory.Peek();

            var srf = SelectedRootFile;
            File.WriteAllText(srf, string.Join("\n", content) + "\n");
            ReloadSourceFiles();
        }
    }
}