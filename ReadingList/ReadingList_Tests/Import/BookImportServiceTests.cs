using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ReadingList.App;
using ReadingList.Domain;
using ReadingList.Infrastructure;
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

        var file1Line1 = "Id,Title,Author,Year,Pages,Genre,Finished,Rating";
        var file1Line2 = "1,Title1,Alice,2020,100,1,true,4.0";
        var file1Line3 = "2,Title2,Bob,2020,120,1,true,3.0";
        var file1Line4 = "BADLINE";

        var file2Line1 = "Id,Title,Author,Year,Pages,Genre,Finished,Rating";
        var file2Line2 = "2,Title2-DUP,Bob,2020,120,1,true,3.0";
        var file2Line3 = "3,Title3,Cat,2022,200,2,false,4.5";

        var file1Content = string.Join('\n', new[]
        {
            file1Line1,
            file1Line2,
            file1Line3,
            file1Line4,
        });

        var file2Content = string.Join('\n', new[]
        {
            file2Line1,
            file2Line2,
            file2Line3,
        });

        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [file1Path] = new MockFileData(file1Content),
            [file2Path] = new MockFileData(file2Content),
        });

        var repo = new InMemoryRepository<Book, int>(b => b.Id);

        var parser = new Mock<IBookParser>();
        Result<Book> Ok(Book b) => new(b);
        Result<Book> Fail(string err) => new(new[] { err });

        var map = new Dictionary<string, Result<Book>>
        {
            [file1Line2] = Ok(new Book
            {
                Id = 1,
                Title = "Title1",
                Author = "Alice",
                Year = 2020,
                Pages = 100,
                Genre = (Genre)1,
                Finished = true,
                Rating = 4.0
            }),

            ["BADLINE"] = Fail("CSV parse error: malformed"),

            [file1Line3] = Ok(new Book
            {
                Id = 2,
                Title = "Title2",
                Author = "Bob",
                Year = 2020,
                Pages = 120,
                Genre = (Genre)1,
                Finished = true,
                Rating = 3.0
            }),

            [file2Line2] = Ok(new Book
            {
                Id = 2,
                Title = "Title2-DUP",
                Author = "Bob",
                Year = 2020,
                Pages = 120,
                Genre = (Genre)1,
                Finished = true,
                Rating = 3.0
            }),

            [file2Line3] = Ok(new Book
            {
                Id = 3,
                Title = "Title3",
                Author = "Cat",
                Year = 2022,
                Pages = 200,
                Genre = (Genre)2,
                Finished = false,
                Rating = 4.5
            }),
        };

        parser.Setup(p => p.TryParse(It.Is<string>(s => map.ContainsKey(s))))
              .Returns((string s) => map[s]);

        parser.Setup(p => p.TryParse(It.Is<string>(s => !map.ContainsKey(s))))
              .Returns<string>(s => Fail($"Unexpected line in test: '{s}'"));

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