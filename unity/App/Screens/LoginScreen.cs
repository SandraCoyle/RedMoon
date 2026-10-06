using System.Collections.Generic;
using RedMoon.Core.Security;
using RedMoon.Core.Services;
using RedMoon.UnityApp.Controls;
using RedMoon.UnityApp.Navigation;
using RedMoon.UnityApp.UI;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Screens
{
    /// <summary>
    /// Velkomst/login. Findes der en konto, logger brugeren ind med brugernavn + mønster.
    /// Ellers vises "Opret bruger" og "Hent fra anden telefon".
    /// </summary>
    internal sealed class LoginScreen : ScreenBase
    {
        private readonly VisualElement _accountPanel;
        private readonly VisualElement _noAccountPanel;
        private readonly TextField _username;
        private readonly PatternLockElement _pattern;
        private bool _busy;

        public LoginScreen(ScreenContext context) : base(context, "Log ind")
        {
            Root.AddToClassList("rm-screen--center");

            var moon = new MoonElement(110);
            moon.SetCycle(28, 5, 0);
            Root.Add(moon);
            Root.Add(Ui.Label("Rød Måne", "rm-title", "rm-center"));

            _accountPanel = Ui.Box("rm-stretch");
            Root.Add(_accountPanel);
            _accountPanel.Add(Ui.Label("Velkommen tilbage", "rm-subtitle", "rm-center"));
            _username = Ui.TextField(_accountPanel, "Brugernavn (lad stå tomt, hvis du ikke har et)", AccountService.MaxUsernameLength);
            _accountPanel.Add(Ui.Label("Tegn dit mønster", "rm-field-caption", "rm-center"));
            _pattern = new PatternLockElement();
            _pattern.Completed += points => Fire(() => LoginAsync(points));
            _accountPanel.Add(_pattern);
            CreateErrorLabel(_accountPanel);
            _accountPanel.Add(Ui.Button("Glemt mønster?", () => Fire(ForgotPatternAsync), "rm-btn--link"));

            _noAccountPanel = Ui.Box("rm-stretch");
            Root.Add(_noAccountPanel);
            _noAccountPanel.Add(Ui.Label("Din private dagbog for humør og cyklus. Alt bliver på din telefon.", "rm-subtitle", "rm-center"));
            _noAccountPanel.Add(Ui.Spacer(24));
            _noAccountPanel.Add(Ui.Button("Opret bruger", () => Context.Navigator.Push(new RegisterScreen(Context))));
            _noAccountPanel.Add(Ui.Button("Hent fra anden telefon", () => Context.Navigator.Push(new ImportScreen(Context)), "rm-btn--secondary"));
        }

        public override void OnShow()
        {
            var hasAccount = Context.Services.Accounts.HasAccount;
            Ui.SetVisible(_accountPanel, hasAccount);
            Ui.SetVisible(_noAccountPanel, !hasAccount);
            _pattern.ResetPattern();
            ClearError();
        }

        public override void OnHide() => _pattern.ResetPattern();

        private async System.Threading.Tasks.Task LoginAsync(IReadOnlyList<int> points)
        {
            if (_busy) return;
            ClearError();

            var pattern = PatternPassword.TryCreate(points);
            if (pattern == null)
            {
                _pattern.ShowError();
                ShowError($"Forbind mindst {PatternPassword.MinimumLength} punkter.");
                return;
            }

            _busy = true;
            try
            {
                LoginResult? result = null;
                var ok = await RunAsync(async () => result = await Context.Services.Accounts.LoginAsync(_username.value, pattern));
                if (!ok || result == null)
                {
                    _pattern.ShowError();
                    return;
                }

                if (result.IsSuccess)
                {
                    _username.value = string.Empty;
                    Context.Navigator.ShowMain();
                    return;
                }

                _pattern.ShowError();
                ShowError(result.Message);
                if (result.Outcome == LoginOutcome.NoAccount) OnShow();
            }
            finally
            {
                _busy = false;
            }
        }

        private async System.Threading.Tasks.Task ForgotPatternAsync()
        {
            var confirmed = await Context.Dialogs.ConfirmAsync(
                "Glemt mønster?",
                "Dine data er kun gemt på denne telefon og kan ikke gendannes uden dit mønster.\n\n" +
                "Du kan nulstille appen, men så bliver ALLE dine registreringer slettet permanent.",
                "Slet alt og start forfra", destructive: true);
            if (!confirmed) return;

            if (await RunAsync(() => Context.Services.Accounts.DeleteAllDataAsync()))
            {
                OnShow();
                await Context.Dialogs.AlertAsync("Nulstillet", "Alle data er slettet. Du kan nu oprette en ny bruger.");
            }
        }
    }
}
