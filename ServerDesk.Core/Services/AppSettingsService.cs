using ServerDesk.Core.Errors;
using ServerDesk.Core.Interfaces;

namespace ServerDesk.Core.Services;

public sealed class AppSettingsService
{
    private readonly IAppSettingsRepository _repository;

    public AppSettingsService(IAppSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> GetLanguageAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await _repository.LoadAsync(cancellationToken);

        return settings.Language is "en" or "pl"
            ? settings.Language
            : "pl";
    }

    public async Task SetLanguageAsync(
        string language,
        CancellationToken cancellationToken = default)
    {
        if (language is not ("en" or "pl"))
            throw new AppException(AppErrorCode.InvalidLanguage);

        var settings = await _repository.LoadAsync(cancellationToken);

        await _repository.SaveAsync(
            settings with { Language = language },
            cancellationToken);
    }
}