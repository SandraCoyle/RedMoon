using System.Collections.Generic;
using System.Threading.Tasks;
using RedMoon.Core.Common;
using RedMoon.Core.Security;
using RedMoon.Core.Services;
using RedMoon.UnityApp.Controls;
using RedMoon.UnityApp.Navigation;
using RedMoon.UnityApp.UI;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Screens
{
    /// <summary>
    /// Opret bruger i tre trin:
    /// 1) brugernavn (valgfrit), fødselsår og fødselsmåned,
    /// 2) tegn mønster,
    /// 3) gentag mønster.
    /// </summary>
    internal sealed class RegisterScreen : ScreenBase
    {
        private const string ChooseYear = "Vælg år";
        private const string ChooseMonth = "Vælg måned";

        private readonly VisualElement _detailsPanel;
        private readonly VisualElement _patternPanel;
        private readonly TextField _username;
        private readonly DropdownField _year;
        private readonly DropdownField _month;
        private readonly List<string> _yearChoices = new List<string> { ChooseYear };
        private readonly List<string> _monthChoices = new List<string> { ChooseMonth };
        private readonly Label _patternTitle;
        private readonly Label _patternHint;
        private readonly PatternLockElement _pattern;
        private readonly Label _detailsError;
        private readonly Label _patternError;
        private PatternPassword? _firstPattern;
        private bool _busy;

        public RegisterScreen(ScreenContext context) : base(context, "Opret bruger")
        {
            // Trin 1: oplysninger.
            _detailsPanel = new ScrollView(ScrollViewMode.Vertical);
            _detailsPanel.AddToClassList("rm-stretch");
            Root.Add(_detailsPanel);

            _detailsPanel.Add(Ui.Label("Om dig", "rm-title"));
            _detailsPanel.Add(Ui.Label("Appen gemmer kun det her – og kun på din telefon.", "rm-muted"));
            _detailsPanel.Add(Ui.Spacer(12));

            _username = Ui.TextField(_detailsPanel, "Brugernavn (valgfrit – brug et kaldenavn, ikke dit rigtige navn)", AccountService.MaxUsernameLength);

            var newestYear = context.Services.Clock.Today.Year - AccountService.MinimumAgeYears;
            for (var year = newestYear; year >= AccountService.OldestBirthYear; year--) _yearChoices.Add(year.ToString());
            for (var month = 1; month <= 12; month++) _monthChoices.Add(Texts.MonthName(month));

            _detailsPanel.Add(Ui.Label("Fødselsår", "rm-field-caption"));
            _year = new DropdownField(_yearChoices, 0);
            _year.AddToClassList("rm-field");
            _detailsPanel.Add(_year);

            _detailsPanel.Add(Ui.Label("Fødselsmåned", "rm-field-caption"));
            _month = new DropdownField(_monthChoices, 0);
            _month.AddToClassList("rm-field");
            _detailsPanel.Add(_month);

            _detailsError = Ui.Label(string.Empty, "rm-error");
            _detailsPanel.Add(_detailsError);
            _detailsPanel.Add(Ui.Button("Næste", GoToPattern));

            // Trin 2 + 3: mønster.
            _patternPanel = Ui.Box("rm-stretch", "rm-screen--center");
            Root.Add(_patternPanel);
            _patternTitle = Ui.Label(string.Empty, "rm-title", "rm-center");
            _patternHint = Ui.Label(string.Empty, "rm-muted", "rm-center");
            _pattern = new PatternLockElement();
            _pattern.Completed += points => Fire(() => OnPatternAsync(points));
            _patternError = Ui.Label(string.Empty, "rm-error");
            _patternPanel.Add(_patternTitle);
            _patternPanel.Add(_patternHint);
            _patternPanel.Add(Ui.Spacer(12));
            _patternPanel.Add(_pattern);
            _patternPanel.Add(_patternError);
            _patternPanel.Add(Ui.Button("Tilbage til oplysninger", ShowDetails, "rm-btn--link"));

            ShowDetails();
        }

        public override void OnHide()
        {
            // Glem det første mønster, hvis brugeren forlader skærmen.
            _firstPattern = null;
            _pattern.ResetPattern();
        }

        private void ShowDetails()
        {
            _firstPattern = null;
            _patternError.text = string.Empty;
            Ui.SetVisible(_detailsPanel, true);
            Ui.SetVisible(_patternPanel, false);
        }

        private void ShowPatternStep(bool confirm)
        {
            Ui.SetVisible(_detailsPanel, false);
            Ui.SetVisible(_patternPanel, true);
            _patternTitle.text = confirm ? "Tegn mønsteret igen" : "Tegn dit hemmelige mønster";
            _patternHint.text = confirm
                ? "Så er vi sikre på, at du kan huske det."
                : $"Forbind mindst {PatternPassword.MinimumLength} punkter. Flere punkter = sværere at gætte.";
        }

        private void GoToPattern()
        {
            _detailsError.text = string.Empty;
            try
            {
                _username.value = AccountService.NormalizeUsername(_username.value);
                if (SelectedYear() == null) throw new ValidationException("Vælg dit fødselsår.");
                if (SelectedMonth() == null) throw new ValidationException("Vælg din fødselsmåned.");
                AccountService.ValidateBirth(SelectedYear()!.Value, SelectedMonth()!.Value, Context.Services.Clock.Today);
            }
            catch (ValidationException ex)
            {
                _detailsError.text = ex.Message;
                return;
            }

            _firstPattern = null;
            ShowPatternStep(confirm: false);
        }

        private async Task OnPatternAsync(IReadOnlyList<int> points)
        {
            if (_busy) return;
            _patternError.text = string.Empty;

            var pattern = PatternPassword.TryCreate(points);
            if (pattern == null)
            {
                _pattern.ShowError();
                _patternError.text = $"Forbind mindst {PatternPassword.MinimumLength} forskellige punkter.";
                return;
            }

            if (_firstPattern == null)
            {
                _firstPattern = pattern;
                ShowPatternStep(confirm: true);
                return;
            }

            if (!_firstPattern.SameAs(pattern))
            {
                _pattern.ShowError();
                _patternError.text = "Mønstrene var ikke ens. Prøv igen fra start.";
                _firstPattern = null;
                ShowPatternStep(confirm: false);
                return;
            }

            _busy = true;
            try
            {
                var created = await Context.RunAsync(
                    () => Context.Services.Accounts.CreateAccountAsync(_username.value, pattern, SelectedYear()!.Value, SelectedMonth()!.Value),
                    message => _patternError.text = message);
                _firstPattern = null;
                if (created) Context.Navigator.ShowMain();
            }
            finally
            {
                _busy = false;
            }
        }

        private int? SelectedYear()
        {
            var index = _yearChoices.IndexOf(_year.value);
            return index > 0 && int.TryParse(_yearChoices[index], out var year) ? year : (int?)null;
        }

        private int? SelectedMonth()
        {
            var index = _monthChoices.IndexOf(_month.value);
            return index > 0 ? index : (int?)null;
        }
    }
}
