using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TransparentTwitchChatWPF
{
    /// <summary>
    /// Interaction logic for Input.xaml
    /// </summary>
    public partial class Input_Custom : Window
    {
        public Func<string, string> ValidateInput { get; set; }
        public Input_Custom(string Url = "", string displayName = "", string css = null, bool allowInteraction = false)
        {
            InitializeComponent();
            tbCustomCSS.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("CSS");
            txtUrl.Text = Url;
            txtName.Text = displayName;
            if (css != null) tbCustomCSS.Text = css;
            cbAllowInteraction.IsChecked = allowInteraction;
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            var error = ValidateInput?.Invoke(txtUrl.Text);
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(this, error, "Edit Widget", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtUrl.SelectAll();
            txtUrl.Focus();
        }

        public string Url
        {
            get { return txtUrl.Text; }
        }

        public string DisplayName
        {
            get { return txtName.Text; }
        }

        public string CustomCSS
        {
            get { return tbCustomCSS.Text; }
        }

        public bool AllowInteraction
        {
            get { return cbAllowInteraction.IsChecked ?? false; }
        }
    }
}
