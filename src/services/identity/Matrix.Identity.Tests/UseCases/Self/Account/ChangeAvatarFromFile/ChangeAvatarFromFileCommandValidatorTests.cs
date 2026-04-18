using FluentAssertions;
using Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile;
using Xunit;

namespace Matrix.Identity.Tests.UseCases.Self.Account.ChangeAvatarFromFile;

public sealed class ChangeAvatarFromFileCommandValidatorTests
{
    private readonly ChangeAvatarFromFileCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldRejectHtmlExtension()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        ChangeAvatarFromFileCommand command = CreateCommand(
            fileStream: stream,
            fileName: "avatar.html",
            contentType: "image/png",
            fileSize: stream.Length);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(ChangeAvatarFromFileCommand.FileName));
    }

    [Fact]
    public void Validate_ShouldRejectSvgExtension()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        ChangeAvatarFromFileCommand command = CreateCommand(
            fileStream: stream,
            fileName: "avatar.svg",
            contentType: "image/png",
            fileSize: stream.Length);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(ChangeAvatarFromFileCommand.FileName));
    }

    [Fact]
    public void Validate_ShouldRejectFakeContentType()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        ChangeAvatarFromFileCommand command = CreateCommand(
            fileStream: stream,
            fileName: "avatar.png",
            contentType: "text/html",
            fileSize: stream.Length);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(ChangeAvatarFromFileCommand.ContentType));
    }

    [Fact]
    public void Validate_ShouldRejectOversizedUpload()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        ChangeAvatarFromFileCommand command = CreateCommand(
            fileStream: stream,
            fileName: "avatar.png",
            contentType: "image/png",
            fileSize: AvatarUploadConstraints.MaxFileBytes + 1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(ChangeAvatarFromFileCommand.FileSize));
    }

    [Fact]
    public void Validate_ShouldAcceptSupportedUploadEnvelope()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        ChangeAvatarFromFileCommand command = CreateCommand(
            fileStream: stream,
            fileName: "avatar.webp",
            contentType: "image/webp",
            fileSize: stream.Length);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    private static ChangeAvatarFromFileCommand CreateCommand(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize)
    {
        return new ChangeAvatarFromFileCommand(
            FileStream: fileStream,
            FileName: fileName,
            ContentType: contentType,
            FileSize: fileSize);
    }
}
