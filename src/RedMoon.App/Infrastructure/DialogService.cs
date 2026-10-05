namespace RedMoon.App.Infrastructure;

/// <summary>
/// Dialoger (beskeder, bekræftelser, tekst-input). Abstraheret så ViewModels ikke kender til sider.
/// </summary>
public interface IDialogService
{
    Task AlertAsync(string title, string message, string ok = "OK");
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel = "Fortryd");
    Task<string?> PromptAsync(string title, string message, string accept, string cancel = "Fortryd");
}

/// <summary>MAUI-implementering der viser dialoger oven på den aktuelle side.</summary>
public sealed class DialogService : IDialogService
{
    public Task AlertAsync(string title, string message, string ok = "OK") =>
        CurrentPage?.DisplayAlertAsync(title, message, ok) ?? Task.CompletedTask;

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel = "Fortryd") =>
        CurrentPage?.DisplayAlertAsync(title, message, accept, cancel) ?? Task.FromResult(false);

    public Task<string?> PromptAsync(string title, string message, string accept, string cancel = "Fortryd") =>
        CurrentPage?.DisplayPromptAsync(title, message, accept, cancel) ?? Task.FromResult<string?>(null);

    private static Page? CurrentPage
    {
        get
        {
            var root = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (root == null) return null;
            // Vis dialogen på den øverste side (modal side, hvis der er en).
            return root.Navigation.ModalStack.LastOrDefault() ?? root;
        }
    }
}
