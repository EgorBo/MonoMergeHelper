using System.Windows;
using System.Windows.Input;
using WpfDiffView;

namespace CorefxImportHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                ((MainViewModel) DataContext).UndoLastAction();
            }
        }
    }
}
