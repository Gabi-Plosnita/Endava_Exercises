using CsvHelper.Configuration;
using ReadingList.Domain;
using System.Globalization;

namespace ReadingList.Infrastructure;

public class BookMap : ClassMap<Book>
{
    public BookMap()
    {
        Map(m => m.Id).Index(0).TypeConverterOption.NumberStyles(NumberStyles.Integer);
        Map(m => m.Title).Index(1);
        Map(m => m.Author).Index(2);
        Map(m => m.Year).Index(3).TypeConverterOption.NumberStyles(NumberStyles.Integer);
        Map(m => m.Pages).Index(4).TypeConverterOption.NumberStyles(NumberStyles.Integer);
        Map(m => m.Genre).Index(5);       
        Map(m => m.Finished).Index(6);    
        Map(m => m.Rating).Index(7).TypeConverterOption.NumberStyles(NumberStyles.Float);
    }
}
