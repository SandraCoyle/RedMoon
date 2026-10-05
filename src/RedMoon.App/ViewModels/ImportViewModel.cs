using System.Windows.Input;
using RedMoon.App.Infrastructure;
using RedMoon.App.Navigation;
using RedMoon.Core.Common;
using RedMoon.Core.Services;

namespace RedMoon.App.ViewModels;

/// <summary>
/// Hent konto og registreringer fra en anden telefon via en krypteret overførselsfil + kode.
/// </summary>
public sealed class ImportViewModel : ViewModelBase
{
    private const long MaxFileBytes = 20 * 1024 * 1024;

    private readonly TransferService _transfer;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private byte[]? _fileContent;
    private string _fileName = string.Empty;
    private string _code = string.Empty;

    public ImportViewModel(TransferService transfer, INavigationService navigation, IDialogService dialogs)
    {
        _transfer = transfer;
        _navigation = navigation;
        _dialogs = dialogs;
        PickFileCommand = new AsyncCommand(PickFileAsync);
        ImportCommand = new AsyncCommand(ImportAsync);
    }

    /// <summary>Navnet på den valgte fil (tomt hvis ingen er valgt).</summary>
    public string FileName
    {
        get => _fileName;
        private set
        {
            if (SetProperty(ref _fileName, value)) OnPropertyChanged(nameof(HasFile));
        }
    }

    public bool HasFile => _fileContent != null;

    /// <summary>Overførselskoden fra den gamle telefon.</summary>
    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value ?? string.Empty);
    }

    public ICommand PickFileCommand { get; }
    public ICommand ImportCommand { get; }

    private async Task PickFileAsync()
    {
        await RunAsync(async () =>
        {
            // Systemets egen filvælger: kræver ingen tilladelser, og appen får kun adgang til den valgte fil.
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Vælg din Rød Måne-fil" });
            if (result == null) return;

            await using var stream = await result.OpenReadAsync();
            if (stream.CanSeek && stream.Length > MaxFileBytes) throw new ValidationException("Filen er for stor.");

            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            if (memory.Length > MaxFileBytes) throw new ValidationException("Filen er for stor.");

            _fileContent = memory.ToArray();
            FileName = result.FileName;
        });
    }

    private async Task ImportAsync()
    {
        if (_fileContent == null)
        {
            ErrorMessage = "Vælg først en fil.";
            return;
        }

        var ok = await RunAsync(() => _transfer.ImportAsync(_fileContent, Code));
        if (!ok) return;

        _fileContent = null;
        Code = string.Empty;
        await _dialogs.AlertAsync("Velkommen tilbage 🌙", "Dine data er hentet. Log ind med dit brugernavn og dit mønster.");
        await _navigation.PopAsync();
    }
}
