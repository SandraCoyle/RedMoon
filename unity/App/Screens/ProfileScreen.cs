using System;
using System.Linq;
using System.Threading.Tasks;
using RedMoon.Core.Models;
using RedMoon.Core.Services;
using RedMoon.UnityApp.Controls;
using RedMoon.UnityApp.Graphics;
using RedMoon.UnityApp.Navigation;
using RedMoon.UnityApp.UI;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Screens
{
    /// <summary>
    /// Profil/indstillinger: dagens humør, brugernavn, fødselsår/-måned, skift mønster,
    /// overfør til ny telefon, log ud og slet alle data.
    /// </summary>
    internal sealed class ProfileScreen : ScreenBase
    {
        /// <summary>Ordet brugeren skal skrive for at bekræfte sletning.</summary>
        public const string DeleteConfirmationWord = "SLET";

        private readonly ChoiceRow<Mood> _mood;
        private readonly Label _status;
        private readonly TextField _username;
        private readonly Label _birth;
        private readonly Label _keyStore;

        public ProfileScreen(ScreenContext context) : base(context, "Profil")
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("rm-stretch");
            Root.Add(scroll);

            scroll.Add(Ui.Label("Profil", "rm-title"));

            var moodCard = Ui.Box("rm-card");
            moodCard.Add(Ui.Label("Dagens humør", "rm-section-title"));
            _mood = new ChoiceRow<Mood>(Texts.Moods.Select(m => new Choice<Mood>(m, Texts.MoodName(m), Icons.Mood(m))), 40f);
            _mood.Selected += value => Fire(() => SetMoodAsync(value));
            moodCard.Add(_mood);
            _status = Ui.Label(string.Empty, "rm-success");
            moodCard.Add(_status);
            scroll.Add(moodCard);

            var aboutCard = Ui.Box("rm-card");
            aboutCard.Add(Ui.Label("Om dig", "rm-section-title"));
            _username = Ui.TextField(aboutCard, "Brugernavn (valgfrit)", AccountService.MaxUsernameLength);
            aboutCard.Add(Ui.Button("Gem brugernavn", () => Fire(SaveUsernameAsync), "rm-btn--secondary", "rm-btn--small"));
            _birth = Ui.Label(string.Empty, "rm-text");
            aboutCard.Add(Ui.Spacer(8));
            aboutCard.Add(_birth);
            CreateErrorLabel(aboutCard);
            scroll.Add(aboutCard);

            var securityCard = Ui.Box("rm-card");
            securityCard.Add(Ui.Label("Sikkerhed og data", "rm-section-title"));
            securityCard.Add(Ui.Button("Skift mønster", () => Context.Navigator.Push(new ChangePatternScreen(Context)), "rm-btn--secondary"));
            securityCard.Add(Ui.Button("Overfør til ny telefon", () => Context.Navigator.Push(new ExportScreen(Context)), "rm-btn--secondary"));
            securityCard.Add(Ui.Button("Log ud", () => Fire(LogoutAsync), "rm-btn--secondary"));
            securityCard.Add(Ui.Button("Slet alle data", () => Fire(DeleteAllAsync), "rm-btn--danger"));
            _keyStore = Ui.Label(string.Empty, "rm-muted");
            securityCard.Add(_keyStore);
            scroll.Add(securityCard);

            scroll.Add(Ui.Label("Rød Måne gemmer kun brugernavn, mønster (krypteret), fødselsår, fødselsmåned, humør og menstruation – og kun på denne telefon.", "rm-muted", "rm-center"));
        }

        public override void OnShow()
        {
            var accounts = Context.Services.Accounts;
            if (!accounts.IsLoggedIn) return;
            var account = accounts.CurrentAccount;
            _username.value = account.Username;
            _birth.text = $"Født {Texts.MonthName(account.BirthMonth).ToLowerInvariant()} {account.BirthYear}";
            var mood = Context.Services.Diary.GetEntry(Context.Services.Diary.Today).Mood;
            _mood.SetSelected(mood == Mood.None ? (Mood?)null : mood);
            _status.text = string.Empty;
            _keyStore.text = Context.Services.KeyStore.Description;
            Ui.SetClass(_keyStore, "rm-warning", !Context.Services.KeyStore.IsHardwareBacked);
            ClearError();
        }

        private async Task SetMoodAsync(Mood mood)
        {
            var diary = Context.Services.Diary;
            var current = diary.GetEntry(diary.Today).Mood;
            var next = current == mood ? Mood.None : mood;
            if (await RunAsync(() => diary.SetMoodAsync(diary.Today, next), showBusy: false))
            {
                _mood.SetSelected(next == Mood.None ? (Mood?)null : next);
                _status.text = next == Mood.None ? "Humør fjernet" : "Dagens humør: " + Texts.MoodName(next);
            }
        }

        private async Task SaveUsernameAsync()
        {
            ClearError();
            _status.text = string.Empty;
            if (await RunAsync(() => Context.Services.Accounts.ChangeUsernameAsync(_username.value)))
            {
                _username.value = Context.Services.Accounts.CurrentAccount.Username;
                await Context.Dialogs.AlertAsync("Gemt", "Dit brugernavn er gemt.");
            }
        }

        private async Task LogoutAsync()
        {
            if (await RunAsync(() => Context.Services.Accounts.LogoutAsync()))
            {
                Context.Navigator.ShowLogin();
            }
        }

        private async Task DeleteAllAsync()
        {
            var answer = await Context.Dialogs.PromptAsync(
                "Slet alle data?",
                "Dette sletter din bruger og ALLE registreringer af humør og menstruation fra telefonen. " +
                "Det kan ikke fortrydes.\n\n" +
                $"Skriv {DeleteConfirmationWord} for at bekræfte.",
                "Slet permanent", destructive: true);

            if (answer == null) return;
            if (!string.Equals(answer.Trim(), DeleteConfirmationWord, StringComparison.OrdinalIgnoreCase))
            {
                await Context.Dialogs.AlertAsync("Ikke slettet", $"Du skrev ikke {DeleteConfirmationWord}, så intet er slettet.");
                return;
            }

            if (await RunAsync(() => Context.Services.Accounts.DeleteAllDataAsync()))
            {
                await Context.Dialogs.AlertAsync("Slettet", "Alle data er slettet fra telefonen.");
                Context.Navigator.ShowLogin();
            }
        }
    }
}
