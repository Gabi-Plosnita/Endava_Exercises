using AirportTool.Application;
using AirportTool.WebApi;
using AwesomeAssertions;
using System.Net;

namespace AirportTool.Tests.Api.Helpers;

public class ResultSeverityResolverTests
{
    private readonly ResultSeverityResolver _sut = new();

    #region GetSeverity Tests

    [Fact]
    public void GetSeverity_WhenResultIsNull_ReturnsNull()
    {
        // Arrange
        Result result = null!;

        // Act
        var severity = _sut.GetSeverity(result);

        // Assert
        severity.Should().BeNull();
    }

    [Fact]
    public void GetSeverity_WhenResultHasNoErrors_ReturnsNull()
    {
        // Arrange
        var result = CreateResultWithErrors(); 

        // Act
        var severity = _sut.GetSeverity(result);

        // Assert
        severity.Should().BeNull();
    }

    [Theory]
    [InlineData(ErrorType.Validation, ErrorType.Validation)]
    [InlineData(ErrorType.NotFound, ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict, ErrorType.Conflict)]
    [InlineData(ErrorType.Unexpected, ErrorType.Unexpected)]
    public void GetSeverity_WhenResultHasOnlyOneErrorType_ReturnsThatType(ErrorType errorType, ErrorType expected)
    {
        // Arrange
        var result = CreateResultWithErrors(errorType);

        // Act
        var severity = _sut.GetSeverity(result);

        // Assert
        severity.Should().Be(expected);
    }

    [Fact]
    public void GetSeverity_WhenResultHasMultipleErrorTypes_ReturnsHighestPriorityBasedOnOrder()
    {
        // Arrange
        var result = CreateResultWithErrors(ErrorType.Validation, ErrorType.NotFound, ErrorType.Conflict);

        // Act
        var severity = _sut.GetSeverity(result);

        // Assert
        severity.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void GetSeverity_WhenResultHasUnexpectedAlongWithOthers_ReturnsUnexpected()
    {
        // Arrange
        var result = CreateResultWithErrors(ErrorType.Validation, ErrorType.NotFound, ErrorType.Unexpected);

        // Act
        var severity = _sut.GetSeverity(result);

        // Assert
        severity.Should().Be(ErrorType.Unexpected);
    }

    #endregion

    #region GetHttpStatusCode Tests

    [Fact]
    public void GetHttpStatusCode_WhenResultIsNull_ReturnsOk()
    {
        // Arrange
        Result result = null!;

        // Act
        var status = _sut.GetHttpStatusCode(result);

        // Assert
        status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void GetHttpStatusCode_WhenResultHasNoErrors_ReturnsOk()
    {
        // Arrange
        var result = CreateResultWithErrors();

        // Act
        var status = _sut.GetHttpStatusCode(result);

        // Assert
        status.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(ErrorType.Validation, HttpStatusCode.BadRequest)]
    [InlineData(ErrorType.NotFound, HttpStatusCode.NotFound)]
    [InlineData(ErrorType.Conflict, HttpStatusCode.Conflict)]
    [InlineData(ErrorType.Unexpected, HttpStatusCode.InternalServerError)]
    public void GetHttpStatusCode_WhenResultHasSeverity_ReturnsMappedStatusCode(ErrorType errorType, HttpStatusCode expected)
    {
        // Arrange
        var result = CreateResultWithErrors(errorType);

        // Act
        var status = _sut.GetHttpStatusCode(result);

        // Assert
        status.Should().Be(expected);
    }

    [Fact]
    public void GetHttpStatusCode_WhenResultHasMultipleErrorTypes_ReturnsStatusCodeForHighestPrioritySeverity()
    {
        // Arrange
        var result = CreateResultWithErrors(ErrorType.Validation, ErrorType.NotFound);

        // Act
        var status = _sut.GetHttpStatusCode(result);

        // Assert
        status.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    private static Result CreateResultWithErrors(params ErrorType[] errors)
    {
        var result = new Result();

        if (errors is null || errors.Length == 0)
            return result;

        foreach (var t in errors)
        {
            result.Errors.Add(new Error { Type = t });
        }

        return result;
    }
}
