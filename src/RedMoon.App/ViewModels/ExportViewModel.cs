using System.Windows.Input;
using RedMoon.App.Infrastructure;
using RedMoon.Core.Services;

namespace RedMoon.App.ViewModels;

/// <summary>
/// Overfør til ny telefon: laver en krypteret fil og viser en engangskode.
/// Brugeren deler selv filen (AirDrop, Nearby Share, kabel ...) – appen sender intet over nettet.
/// </summary>
public sealed class ExportViewModel : ViewModelBase
{
    private readonly TransferService _transfer;
    private string _code = string.Empty;
    private string? _filePath;

    public ExportViewModel(TransferService transfer)
    {
        _transfer = transfer;
        CreateCommand = new AsyncCommand(CreateAsync);
        ShareCommand = new AsyncCommand(ShareAsync);
    }

    /// <summary>Overførselskoden. Vises kun her og gemmes aldrig.</summary>
    public string Code
    {
        get => _code;
        private set
        {
            if (SetProperty(ref _code, value))
            {
                OnPropertyChanged(nameof(HasPackage));
                OnPropertyChanged(nameof(HasNoPackage));
            }
        }
    }

    public bool HasPackage => !string.IsNullOrEmpty(Code);
    public bool HasNoPackage => !HasPackage;

    public ICommand CreateCommand { get; }
    public ICommand ShareCommand { get; }

    /// <summary>Glemmer koden og sletter filen (kaldes når siden lukkes).</summary>
    public void Reset()
    {
        Code = string.Empty;
        _filePath = null;
        TransferFiles.CleanUp();
    }

    private Task CreateAsync() => RunAsync(async () =>
    {
        var package = await _transfer.ExportAsync();
        _filePath = await TransferFiles.WriteAsync(package.SuggestedFileName, package.Content);
        Code = package.Code;
    });

    private Task ShareAsync() => RunAsync(async () =>
    {
        if (_filePath == null || !File.Exists(_filePath)) return;
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Rød Måne – overførselsfil",
            File = new ShareFile(_filePath, "application/octet-stream"),
        });
    }, showBusy: false);
}
