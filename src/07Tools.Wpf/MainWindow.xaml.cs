using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RunescapeTools.Application.Training;
using RunescapeTools.Wpf.ViewModels;

namespace RunescapeTools.Wpf;

public partial class MainWindow : Window
{
    private readonly ITrainingPlanPersistenceService plans;
    private bool preparingToClose;
    private bool closeApproved;

    public MainWindow(ShellViewModel viewModel, ITrainingPlanPersistenceService plans)
    {
        InitializeComponent();
        DataContext = viewModel;
        this.plans = plans;
    }

    // Keep the dispatcher alive until pending planner edits have been saved.
    protected override async void OnClosing(CancelEventArgs e)
    {
        if (closeApproved)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        base.OnClosing(e);
        if (preparingToClose)
            return;

        preparingToClose = true;
        try
        {
            // Commit a valid edit even if its TextBox normally waits for focus to leave.
            if (Keyboard.FocusedElement is TextBox editor)
                editor.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            IsEnabled = false;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await plans.FlushAsync(timeout.Token).WaitAsync(timeout.Token);
            closeApproved = true;
            // Defer even a synchronous flush: Close cannot be called from inside OnClosing.
            _ = Dispatcher.BeginInvoke(new Action(Close));
        }
        catch (Exception)
        {
            MessageBox.Show(this,
                "Your XP Planner changes could not be saved. 07Tools will stay open so you can retry closing. " +
                "Check that your data folder is writable; your pending edits have been kept.",
                "07Tools — changes not saved", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            preparingToClose = false;
            if (!closeApproved)
                IsEnabled = true;
        }
    }
}
