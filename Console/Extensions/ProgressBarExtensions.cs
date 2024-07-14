using Terminal.Gui;

namespace Console.Extensions;

public static class ProgressBarExtensions
{
    public static async Task Start(this ProgressBar progressBar, CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            Application.Invoke(() =>
            {
                if (progressBar.Fraction >= 1f)
                    progressBar.Fraction = 0f;

                progressBar.Fraction += 0.05f;
                Application.Refresh();
            });
            await Task.Delay(50);
        }
        Application.Invoke(() => progressBar.Visible = false);
    }
}
