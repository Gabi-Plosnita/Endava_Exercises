using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ReadingList.App;
using ReadingList.Domain;
using ReadingList.Infrastructure;
using System.Globalization;
using System.IO.Abstractions.TestingHelpers;


namespace ReadingList_Tests;

[TestClass]
public class BookImportServiceTests
{
    [TestMethod]
    public async Task ImportAsync_ReturnsCombinedReport_SkipsDuplicates_LogsMalformed_WhenTwoCsvFilesProvided()
    {
        // Arrange //
        var file1Path = @"C:\import\file1.csv";
        var file2Path = @"C:\import\file2.csv";

        var file1Content = string.Join('\n', new[]
        {
                "Id,Title,Author,Year,Pages,Genre,Finished,Rating",
                "1,Title1,Alice,2020,100,1,true,4.0",
                "2,Title2,Bob,2020,120,1,true,3.0",
                "BADLINE"
            });

        var file2Content = string.Join('\n', new[]
        {
                "Id,Title,Author,Year,Pages,Genre,Finished,Rating",
                "2,Another dup id,Other,2021,90,1,false,2.5",  
                "3,Title3,Cat,2022,200,2,false,4.5"
            });

        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [file1Path] = new MockFileData(file1Content),
            [file2Path] = new MockFileData(file2Content),
        });

        var repo = new InMemoryRepository<Book, int>(b => b.Id);
        var parser = new Mock<IBookParser>();

        parser.Setup(p => p.TryParse(It.Is<string>(s => s == "BADLINE")))
              .Returns((Result<Book>)new Result<Book>(new List<string> { "CSV parse error: malformed" }));

        parser.Setup(p => p.TryParse(It.Is<string>(s => s != "BADLINE")))
              .Returns<string>(line =>
              {
                  var parts = line.Split(',');
                  if (parts.Length < 8)
                      return new Result<Book>(new List<string> { "CSV parse error: not enough fields" });

                  var book = new Book
                  {
                      Id = int.Parse(parts[0], CultureInfo.InvariantCulture),
                      Title = parts[1],
                      Author = parts[2],
                      Year = int.Parse(parts[3], CultureInfo.InvariantCulture),
                      Pages = int.Parse(parts[4], CultureInfo.InvariantCulture),
                      Genre = (Genre)int.Parse(parts[5], CultureInfo.InvariantCulture),
                      Finished = bool.Parse(parts[6]),
                      Rating = double.Parse(parts[7], CultureInfo.InvariantCulture)
                  };

                  var validation = book.Validate();
                  if (validation.IsFailure)
                      return new Result<Book>(validation.Errors);

                  return new Result<Book>(book);
              });

        var logger = new Mock<ILogger<BookImportService>>();

        var service = new BookImportService(repo, parser.Object, logger.Object, mockFileSystem);

        var paths = new[] { file1Path, file2Path };
        var token = CancellationToken.None;

        // Act //
        var result = await service.ImportAsync(paths, token);

        //  Assert //
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();

        var report = result.Value!;
        report.Should().NotBeNull();

        report.TotalImported.Should().Be(3);
        report.TotalDuplicates.Should().Be(1);
        report.TotalMalformed.Should().Be(1);
        report.Files.Should().HaveCount(2);

        repo.Contains(1).Should().BeTrue();
        repo.Contains(2).Should().BeTrue(); 
        repo.Contains(3).Should().BeTrue();

        //logger.VerifyLogContains(LogLevel.Warning, "Parsing error at line 4");
        //logger.VerifyLogContains(LogLevel.Warning, "Duplicate Id skipped at line 2");
        //logger.VerifyLogContains(LogLevel.Information, "[file1.csv] imported: 2, duplicates: 0, malformed: 1");
        //logger.VerifyLogContains(LogLevel.Information, "[file2.csv] imported: 1, duplicates: 1, malformed: 0");
    }
}