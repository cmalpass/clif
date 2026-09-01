using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Caliburn.Micro;

namespace TestWpfApp.Views;

public partial class ShellView : Window
{
    private Window _compatibilityWindow;

    public ShellView()
    {
        InitializeComponent();
    }

    private async void LoadDelayedContent_Click(object sender, RoutedEventArgs e)
    {
        DelayedContentText.Text = "Loading delayed content...";
        await Task.Delay(500);
        DelayedContentText.Text = "Delayed content is ready.";
    }

    private void OpenCompatibilityWindow_Click(object sender, RoutedEventArgs e)
    {
        var textBox = new TextBox
        {
            Text = "Child window is ready",
            Margin = new Thickness(16),
        };
        AutomationProperties.SetAutomationId(textBox, "ChildWindowTextBox");

        _compatibilityWindow = new Window
        {
            Title = "CLIF Compatibility Child Window",
            Width = 360,
            Height = 180,
            Owner = this,
            Content = textBox,
        };
        _compatibilityWindow.Closed += (_, _) => _compatibilityWindow = null;
        _compatibilityWindow.Show();
        DelayedContentText.Text = "Compatibility child window shown.";
    }
}
