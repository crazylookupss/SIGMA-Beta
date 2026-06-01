using SIGMA.Domain.Common;
using Xunit;

namespace SIGMA.Application.Tests.Domain;

public sealed class ResultTests
{
    [Fact]
    public void Success_WithValue_ExposesValueAndNoError()
    {
        var result = Result.Success("ok");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("ok", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_WithValidationError_ExposesErrorAndNoValue()
    {
        var error = Error.Validation("Input.Invalid", "Input is invalid.");

        Result<string> result = Result.Failure<string>(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
        Assert.Same(error, result.Error);
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
    }
}
