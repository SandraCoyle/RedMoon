using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RedMoon.Core.Services;
using RedMoon.UnityApp.Navigation;
using RedMoon.UnityApp.Platform;
using RedMoon.UnityApp.UI;
using UnityEngine.UIElements;

namespace RedMoon.UnityApp.Screens
{
    /// <summary>
    /// Hent konto og registreringer fra en anden telefon via en krypteret overførselsfil + kode.
    /// Unity har ingen indbygget filvælger, så filen lægges i appens overførselsmappe, og appen viser filerne derfra.
    /// </summary>
    internal sealed class ImportScreen : ScreenBase
    {
        private const long MaxFileBytes = 20 * 1024 * 1024;
        private readonly VisualElement _fileList;
        private readonly Label _selectedLabel;
        private readonly TextField _code;
        private string? _selectedPath;

        public ImportScreen(ScreenContext context) : base(context, "Hent fra anden telefon")
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("rm-stretch");
            Root.Add(scroll);

            var directory = PlatformStorage.TransferDirectory();
            scroll.Add(Ui.Label("1. Læg filen i overførselsmappen", "rm-section-title"));
            scroll.Add(Ui.Label("Filen hedder fx redmoon-2026-10-05" + TransferService.FileExtension + ". Mappen er:", "rm-muted"));
            scroll.Add(Ui.Label(directory, "rm-path"));
            if (PlatformStorage.CanOpenFolder)
            {
                scroll.Add(Ui.Button("Åbn mappen", () => PlatformStorage.OpenFolder(directory), "rm-btn--secondary"));
            }

            scroll.Add(Ui.Spacer(12));
            scroll.Add(Ui.Label("2. Vælg filen", "rm-section-title"));
            scroll.Add(Ui.Button("Find filer igen", RefreshFiles, "rm-btn--secondary", "rm-btn--small"));
            _fileList = Ui.Box();
            scroll.Add(_fileList);
            _selectedLabel = Ui.Label(string.Empty, "rm-muted");
            scroll.Add(_selectedLabel);

            scroll.Add(Ui.Spacer(12));
            scroll.Add(Ui.Label("3. Skriv overførselskoden", "rm-section-title"));
            _code = Ui.TextField(scroll, "Koden fra den gamle telefon (fx ABCD-EFGH-JKLM-NPQR)", 30);

            CreateErrorLabel(scroll);
            scroll.Add(Ui.Button("Hent mine data", () => Fire(ImportAsync)));
        }

        public override void OnShow()
        {
            ClearError();
            RefreshFiles();
        }

        public override void OnHide()
        {
            _code.value = string.Empty;
        }

        private void RefreshFiles()
        {
            _fileList.Clear();
            var directory = PlatformStorage.TransferDirectory();
            string[] files;
            try
            {
                Directory.CreateDirectory(directory);
                files = Directory.GetFiles(directory, "*" + TransferService.FileExtension)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .ToArray();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                ShowError("Mappen kunne ikke læses.");
                return;
            }

            if (files.Length == 0)
            {
                _fileList.Add(Ui.Label("Ingen filer fundet endnu.", "rm-muted"));
            }
            foreach (var path in files)
            {
                var file = path;
                _fileList.Add(Ui.Button(Path.GetFileName(file), () => Select(file), "rm-btn--secondary", "rm-btn--small"));
            }
            if (_selectedPath != null && !files.Contains(_selectedPath)) Select(null);
        }

        private void Select(string? path)
        {
            _selectedPath = path;
            _selectedLabel.text = path == null ? string.Empty : "Valgt: " + Path.GetFileName(path);
        }

        private async Task ImportAsync()
        {
            ClearError();
            if (_selectedPath == null)
            {
                ShowError("Vælg først en fil.");
                return;
            }

            var path = _selectedPath;
            var ok = await RunAsync(async () =>
            {
                var info = new FileInfo(path);
                if (!info.Exists) throw new Core.Common.ValidationException("Filen findes ikke længere.");
                if (info.Length > MaxFileBytes) throw new Core.Common.ValidationException("Filen er for stor.");
                var content = File.ReadAllBytes(path);
                await Context.Services.Transfer.ImportAsync(content, _code.value);
            });
            if (!ok) return;

            _code.value = string.Empty;
            await Context.Dialogs.AlertAsync("Velkommen tilbage", "Dine data er hentet. Log ind med dit brugernavn og dit mønster.\n\nSlet overførselsfilen fra mappen, når du er færdig.");
            Context.Navigator.Back();
        }
    }
}
