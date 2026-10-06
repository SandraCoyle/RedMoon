using System.Collections.Generic;
using System.Threading.Tasks;
using RedMoon.Core.Security;
using RedMoon.UnityApp.Controls;
using RedMoon.UnityApp.Navigation;
using RedMoon.UnityApp.UI;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Screens
{
    /// <summary>
    /// Skift mønster i tre trin: nuværende mønster → nyt mønster → gentag nyt mønster.
    /// Kan gøres helt lokalt, fordi hashen blot erstattes i den krypterede datafil.
    /// </summary>
    internal sealed class ChangePatternScreen : ScreenBase
    {
        private enum Step { Current, New, Confirm }

        private readonly Label _title;
        private readonly Label _stepText;
        private readonly PatternLockElement _pattern;
        private Step _step = Step.Current;
        private PatternPassword? _current;
        private PatternPassword? _new;
        private bool _busy;

        public ChangePatternScreen(ScreenContext context) : base(context, "Skift mønster")
        {
            Root.AddToClassList("rm-screen--center");
            _stepText = Ui.Label(string.Empty, "rm-muted", "rm-center");
            _title = Ui.Label(string.Empty, "rm-title", "rm-center");
            _pattern = new PatternLockElement();
            _pattern.Completed += points => Fire(() => OnPatternAsync(points));

            Root.Add(_stepText);
            Root.Add(_title);
            Root.Add(Ui.Spacer(12));
            Root.Add(_pattern);
            CreateErrorLabel(Root);
            SetStep(Step.Current);
        }

        public override void OnHide()
        {
            // Glem mønstrene fra hukommelsen, når skærmen lukkes.
            _current = null;
            _new = null;
            _pattern.ResetPattern();
            SetStep(Step.Current);
            ClearError();
        }

        private async Task OnPatternAsync(IReadOnlyList<int> points)
        {
            if (_busy) return;
            ClearError();

            var pattern = PatternPassword.TryCreate(points);
            if (pattern == null)
            {
                _pattern.ShowError();
                ShowError($"Forbind mindst {PatternPassword.MinimumLength} forskellige punkter.");
                return;
            }

            switch (_step)
            {
                case Step.Current:
                    _current = pattern;
                    SetStep(Step.New);
                    return;
                case Step.New:
                    _new = pattern;
                    SetStep(Step.Confirm);
                    return;
            }

            if (_new == null || _current == null || !_new.SameAs(pattern))
            {
                _pattern.ShowError();
                ShowError("De nye mønstre var ikke ens. Tegn det nye mønster igen.");
                _new = null;
                SetStep(Step.New);
                return;
            }

            _busy = true;
            try
            {
                var current = _current;
                var replacement = _new;
                var ok = await RunAsync(() => Context.Services.Accounts.ChangePatternAsync(current, replacement));
                if (!ok)
                {
                    // Fx forkert nuværende mønster: start forfra.
                    _pattern.ShowError();
                    _current = null;
                    _new = null;
                    SetStep(Step.Current);
                    return;
                }

                await Context.Dialogs.AlertAsync("Mønster skiftet", "Brug dit nye mønster næste gang du logger ind.");
                Context.Navigator.Back();
            }
            finally
            {
                _busy = false;
            }
        }

        private void SetStep(Step step)
        {
            _step = step;
            _stepText.text = $"Trin {(int)step + 1} af 3";
            switch (step)
            {
                case Step.Current:
                    _title.text = "Tegn dit nuværende mønster";
                    break;
                case Step.New:
                    _title.text = "Tegn dit nye mønster";
                    break;
                default:
                    _title.text = "Gentag dit nye mønster";
                    break;
            }
        }
    }
}
