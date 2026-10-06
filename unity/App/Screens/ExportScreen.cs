using System.IO;
using System.Threading.Tasks;
using RedMoon.UnityApp.Navigation;
using RedMoon.UnityApp.Platform;
using RedMoon.UnityApp.UI;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Screens
{
    /// <summary>
    /// Overfør til ny telefon: laver en krypteret fil og viser en engangskode.
    /// Filen lægges i appens overførselsmappe; brugeren flytter den selv. Appen sender intet over nettet.
    /// </summary>
    internal sealed class ExportScreen : ScreenBase
    {
        private readonly VisualElement _before;
        private readonly VisualElement _after;
        private readonly TextField _code;
        private readonly Label _fileLabel;
        private string? _filePath;

        public ExportScreen(ScreenContext context) : base(context, "Overfør til ny telefon")
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("rm-stretch");
            Root.Add(scroll);

            scroll.Add(Ui.Label("Sådan virker det", "rm-section-title"));
            scroll.Add(Ui.Label(
                "Appen laver en krypteret fil med din bruger og dine registreringer, og en kode der låser den op. " +
                "Flyt filen til den nye telefon, og skriv koden dér.", "rm-text"));
            scroll.Add(Ui.Label("Send aldrig filen og koden samme vej (fx i samme besked).", "rm-warning"));

            _before = Ui.Box();
            _before.Add(Ui.Spacer(12));
            _before.Add(Ui.Button("Lav overførselsfil", () => Fire(CreateAsync)));
            scroll.Add(_before);

            _after = Ui.Box();
            _after.Add(Ui.Spacer(12));
            _after.Add(Ui.Label("Din kode (vises kun nu – skriv den ned):", "rm-field-caption"));
            _code = new TextField { isReadOnly = true };
            _code.AddToClassList("rm-field");
            _code.AddToClassList("rm-code");
            _after.Add(_code);
            _after.Add(Ui.Label("Filen ligger her:", "rm-field-caption"));
            _fileLabel = Ui.Label(string.Empty, "rm-path");
            _after.Add(_fileLabel);
            if (PlatformStorage.CanOpenFolder)
            {
                _after.Add(Ui.Button("Åbn mappen", () => PlatformStorage.OpenFolder(PlatformStorage.TransferDirectory()), "rm-btn--secondary"));
            }
            _after.Add(Ui.Button("Slet filen igen", DeleteFile, "rm-btn--danger", "rm-btn--small"));
            scroll.Add(_after);

            CreateErrorLabel(scroll);
        }

        public override void OnShow()
        {
            ClearError();
            ShowResult(null, null);
        }

        public override void OnHide()
        {
            // Koden glemmes, når skærmen lukkes. Filen bliver liggende, så den kan flyttes.
            ShowResult(null, null);
        }

        private async Task CreateAsync()
        {
            ClearError();
            await RunAsync(async () =>
            {
                var package = await Context.Services.Transfer.ExportAsync();
                var directory = PlatformStorage.TransferDirectory();
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, Path.GetFileName(package.SuggestedFileName));
                File.WriteAllBytes(path, package.Content);
                ShowResult(package.Code, path);
            });
        }

        private void DeleteFile()
        {
            if (_filePath != null && File.Exists(_filePath)) File.Delete(_filePath);
            ShowResult(null, null);
        }

        private void ShowResult(string? code, string? path)
        {
            _filePath = path;
            _code.value = code ?? string.Empty;
            _fileLabel.text = path ?? string.Empty;
            Ui.SetVisible(_before, code == null);
            Ui.SetVisible(_after, code != null);
        }
    }
}
