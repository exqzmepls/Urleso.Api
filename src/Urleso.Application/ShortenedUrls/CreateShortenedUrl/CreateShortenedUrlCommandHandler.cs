﻿using Urleso.Application.Abstractions.Data;
using Urleso.Application.Abstractions.Data.Repositories;
using Urleso.Application.Abstractions.Messaging;
using Urleso.Application.Abstractions.Services;
using Urleso.Application.Abstractions.Time;
using Urleso.Domain.Results;
using Urleso.Domain.ShortenedUrls;

namespace Urleso.Application.ShortenedUrls.CreateShortenedUrl;

internal sealed class CreateShortenedUrlCommandHandler(
        ICodeGeneratingService codeGeneratingService,
        IShortenedUrlRepository shortenedUrlRepository,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    : ICommandHandler<CreateShortenedUrlCommand, ShortenedUrl>
{
    public async Task<TypedResult<ShortenedUrl>> Handle(CreateShortenedUrlCommand command,
        CancellationToken cancellationToken)
    {
        var longUrlResult = LongUrl.Create(command.LongUrl);
        if (!longUrlResult.IsSuccess)
        {
            return TypedResult<ShortenedUrl>.Failure(longUrlResult.Error);
        }

        var urlCodeResult = await GenerateUrlCodeAsync(cancellationToken);
        if (!urlCodeResult.IsSuccess)
        {
            return TypedResult<ShortenedUrl>.Failure(urlCodeResult.Error);
        }

        var longUrl = longUrlResult.Value;
        var urlCode = urlCodeResult.Value;
        var shortenedUrl = CreateShortenedUrl(longUrl, urlCode);

        var addResult = await shortenedUrlRepository.AddAsync(shortenedUrl, cancellationToken);
        if (!addResult.IsSuccess)
        {
            return TypedResult<ShortenedUrl>.Failure(addResult.Error);
        }

        var saveChangesResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        return !saveChangesResult.IsSuccess
            ? TypedResult<ShortenedUrl>.Failure(saveChangesResult.Error)
            : TypedResult<ShortenedUrl>.Success(shortenedUrl);
    }

    private async Task<TypedResult<UrlCode>> GenerateUrlCodeAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var shortCodeResult = codeGeneratingService.GenerateUniqueCode(UrlCode.CodeDefaultLength);
            if (!shortCodeResult.IsSuccess)
            {
                return TypedResult<UrlCode>.Failure(shortCodeResult.Error);
            }

            var shortCode = shortCodeResult.Value;
            var urlCodeResult = UrlCode.Create(shortCode);
            if (!urlCodeResult.IsSuccess)
            {
                return urlCodeResult;
            }

            var code = urlCodeResult.Value;
            var isCodeExistResult = await shortenedUrlRepository.IsCodeExistAsync(code, cancellationToken);
            if (!isCodeExistResult.IsSuccess)
            {
                return TypedResult<UrlCode>.Failure(isCodeExistResult.Error);
            }

            var isCodeExist = isCodeExistResult.Value;
            if (!isCodeExist)
            {
                return urlCodeResult;
            }
        }

        var canceledTask = Task.FromCanceled<TypedResult<UrlCode>>(cancellationToken);
        return await canceledTask;
    }

    private ShortenedUrl CreateShortenedUrl(LongUrl longUrl, UrlCode urlCode)
    {
        var id = new ShortenedUrlId(Guid.NewGuid());
        var createOnUtc = clock.GetUtcNow();
        var shortenedUrl = ShortenedUrl.Create(id, longUrl, urlCode, createOnUtc);
        return shortenedUrl;
    }
}