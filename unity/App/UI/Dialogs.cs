using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.UI
{
    /// <summary>
    /// Dialogbokse (besked, bekræftelse, tekst-input) og "Et øjeblik..."-overlay.
    /// Vises oven på alt andet i et separat lag og returnerer en Task, så skærmene kan await'e svaret.
    /// </summary>
    internal sealed class Dialogs
    {
        private readonly VisualElement _layer;
        private readonly VisualElement _busy;
        private int _busyCount;

        public Dialogs(VisualElement layer)
        {
            _layer = layer;
            _busy = Ui.Box("rm-overlay", "rm-busy");
            _busy.Add(Ui.Label("Et øjeblik …", "rm-busy-text"));
            Ui.SetVisible(_busy, false);
            _layer.Add(_busy);
        }

        /// <summary>Viser en besked med én knap.</summary>
        public Task AlertAsync(string title, string message, string ok = "OK") =>
            ShowAsync(title, message, ok, null, withInput: false);

        /// <summary>Spørger om bekræftelse. Sand hvis brugeren valgte <paramref name="accept"/>.</summary>
        public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel = "Fortryd", bool destructive = false)
        {
            var result = await ShowAsync(title, message, accept, cancel, withInput: false, destructive: destructive);
            return result != null;
        }

        /// <summary>Beder om tekst. Null hvis brugeren fortrød.</summary>
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel = "Fortryd", bool destructive = false) =>
            ShowAsync(title, message, accept, cancel, withInput: true, destructive: destructive);

        /// <summary>Viser "Et øjeblik ..." (fx mens mønsteret kontrolleres). Kald HideBusy samme antal gange.</summary>
        public void ShowBusy()
        {
            _busyCount++;
            Ui.SetVisible(_busy, true);
            _busy.BringToFront();
        }

        public void HideBusy()
        {
            _busyCount = System.Math.Max(0, _busyCount - 1);
            if (_busyCount == 0) Ui.SetVisible(_busy, false);
        }

        private Task<string?> ShowAsync(string title, string message, string accept, string? cancel, bool withInput, bool destructive = false)
        {
            var completion = new TaskCompletionSource<string?>();
            var overlay = Ui.Box("rm-overlay");
            var dialog = Ui.Box("rm-dialog");
            overlay.Add(dialog);

            dialog.Add(Ui.Label(title, "rm-dialog-title"));
            dialog.Add(Ui.Label(message, "rm-text"));

            TextField? input = null;
            if (withInput)
            {
                dialog.Add(Ui.Spacer(12));
                input = new TextField { maxLength = 40 };
                input.AddToClassList("rm-field");
                dialog.Add(input);
            }

            dialog.Add(Ui.Spacer(16));
            void Close(string? result)
            {
                overlay.RemoveFromHierarchy();
                completion.TrySetResult(result);
            }

            dialog.Add(Ui.Button(accept, () => Close(input?.value ?? string.Empty), destructive ? "rm-btn--danger-filled" : string.Empty));
            if (cancel != null)
            {
                dialog.Add(Ui.Button(cancel, () => Close(null), "rm-btn--secondary"));
            }

            _layer.Add(overlay);
            input?.Focus();
            return completion.Task;
        }
    }
}
