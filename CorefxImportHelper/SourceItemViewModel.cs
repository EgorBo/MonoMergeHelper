using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;

namespace CorefxImportHelper
{
    public class SourceItemViewModel : ViewModelBase
    {
        private CandidateItemViewModel _selectedCandidateItem;
        private ObservableCollection<CandidateItemViewModel> _candidates;

        public SourceItemViewModel(string path, MainViewModel mainViewModel, bool isNew = false)
        {
            MainViewModel = mainViewModel;
            Path = path;
            IsNew = isNew;
        }

        public bool IsNew { get; }

        public bool HasCandidates => !IsNetCore && Candidates.Any();

        public bool IsNetCore => Path.Contains("../external/core");

        public bool IsNotValidFile => string.IsNullOrWhiteSpace(Path) || Path.StartsWith("#") || Path.Contains("/*");

        public bool IsNamespaceOfInterest
        {
            get
            {
                if (IsNotValidFile)
                    return false;

                var c = Content;
                string[] endings = {"\n", "\t", " ", "\r"};
                foreach (var ending in endings)
                {
                    if (c.Contains(MainViewModel.NamespaceOfInterest + ending))
                        return true;
                }

                return false;
            }
        }

        public MainViewModel MainViewModel { get; }

        public string Content
        {
            get
            {
                if (IsNotValidFile)
                    return "<NOT A VALID FILE>";

                if (string.IsNullOrEmpty(Path))
                    return string.Empty;
                var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(MainViewModel.SelectedRootFile), Path.ToOsPath());
                return File.ReadAllText(path);
            }
            set { }
        }

        public string Comments
        {
            get
            {
                if (IsNotValidFile)
                    return "";
                var c = Content;
                if (c.Contains("MONO") || c.Contains("MOBILE") || c.Contains("_AOT_")) //todo: regex it
                    return "(contains `MONO` #ifdefs)";
                return "";
            }
        }

        public string Path { get; set; }

        public string ShortForm => Path.ShortUnixPathToLong(75);

        public override string ToString() => ShortForm;

        public ObservableCollection<CandidateItemViewModel> Candidates => FindCorertAndCorefxCandidates(Path);

        public CandidateItemViewModel SelectedCandidateItem
        {
            get => _selectedCandidateItem;
            set => Set(nameof(SelectedCandidateItem), ref _selectedCandidateItem, value);
        }

        ObservableCollection<CandidateItemViewModel> FindCorertAndCorefxCandidates(string monoFile)
        {
            if (_candidates != null)
                return _candidates;

            var fileName = System.IO.Path.GetFileName(monoFile);
            var similarFiles = MainViewModel.AllCorefxAndCorertFiles
                .Where(f => System.IO.Path.GetFileName(f).Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
                .Where(f => !f.Contains("tests" + System.IO.Path.DirectorySeparatorChar))
                .Select(f => new CandidateItemViewModel(PathExtensions.GetRelativePath(f, System.IO.Path.GetDirectoryName(MainViewModel.SelectedRootFile)).ToUnixPath(), f, OnRunExternalDiff, OnUseMe))
                .ToArray();
            // TODO: parse CS files, find structs/enums/classes
            return _candidates =  new ObservableCollection<CandidateItemViewModel>(similarFiles);
        }

        void OnUseMe(string candidate)
        {
            var candidateMonoStyle = PathExtensions
                .GetRelativePath(candidate, System.IO.Path.GetDirectoryName(MainViewModel.SelectedRootFile))
                .ToUnixPath();

            var fileLines = File.ReadAllLines(MainViewModel.SelectedRootFile);
            int index = -1;
            for (int i = 0; i < fileLines.Length; i++)
            {
                if (fileLines[i] == Path)
                {
                    fileLines[i] = candidateMonoStyle;
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                MessageBox.Show("oops something went wrong :(");
                return;
            }

            File.WriteAllText(MainViewModel.SelectedRootFile, string.Join("\n", fileLines) + "\n");
            MainViewModel.SourceItems[index] = new SourceItemViewModel(candidateMonoStyle, MainViewModel, true);
            MainViewModel.DumpRootFile();
        }

        void OnRunExternalDiff(string candidate)
        {
            string left = Path;
            string right = candidate;
            MessageBox.Show("External editor is not set yet.");
        }
    }
}