using Library.Pg;
using Library.Pg.Data;
using Microsoft.EntityFrameworkCore;

public static class Program
{
    public static void Main(string[] args)
    {
        using var db = new LibraryContext();

        // ---------- 3.1 Recreate the schema ----------
        Section("3.1 Recreate schema");
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        // ---------- 3.2 / 3.3 Seed ----------
        Section("3.2/3.3 Seed six books");
        Seed(db);

        // ---------- Section 4: queries ----------
        Section("4.1 All books by year");
        AllBooksByYear(db);

        Section("4.2 Books published after 2005");
        BooksAfter2005(db);

        Section("4.3 Books by Martin Fowler");
        BooksByFowler(db);

        Section("4.4 Longest book");
        LongestBook(db);

        Section("4.5 Any unread book?");
        AnyUnread(db);

        Section("4.6 Count of read books");
        CountRead(db);

        Section("4.7 Titles/years projection");
        TitlesAndYears(db);

        Section("4.8 Page 2, 3 per page");
        SecondPage(db);

        Section("4.9 Find vs First");
        FindVsFirst(db);

        Section("4.10 Total price of unread books");
        TotalUnreadPrice(db);

        Section("4.11 Author search: Contains vs ILike");
        AuthorSearch(db);

        // ---------- Section 5: update / delete ----------
        Section("5.1 Mark a book as read");
        MarkAsRead(db);

        Section("5.2 Delete a book");
        DeleteBook(db);

        Section("5.3 Break it on Purpose");
        try
        {
            BreakItOnPurpose(db);
        } catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        // ---------- Section 6: bonus ----------
        Section("6.1 ToQueryString for 4.2");
        PrintQueryString(db);

        Section("6.2 AsNoTracking comparison");
        NoTrackingComparison(db);

        Section("6.3 Optional-filter search");
        var results = Search(db, minYear: 2000, author: null, isRead: false);
        Console.WriteLine($"Search returned {results.Count} row(s)");

        // ---------- Section 7: PostgreSQL specifics ----------
        Section("7.1 UTC timestamp requirement");
        AddWithLocalTime(db);

        Section("7.3 Generated DDL");
        Console.WriteLine(db.Database.GenerateCreateScript());

        Section("7.4 Server-side grouping");
        GroupByAuthor(db);
    }

    private static void Section(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }

    // ---------------- 3.2 / 3.3 ----------------

    private static void Seed(LibraryContext db)
    {
        var books = new List<Book>
        {
            new() { Title = "Clean Code", Author = "Robert Martin", Year = 2008, Pages = 464, Price = 42.50m, IsRead = true, AddedAt = DateTime.UtcNow },
            new() { Title = "The Pragmatic Programmer", Author = "Andrew Hunt", Year = 1999, Pages = 352, Price = 38.00m, IsRead = true, AddedAt = DateTime.UtcNow },
            new() { Title = "Designing Data-Intensive Applications", Author = "Martin Kleppmann", Year = 2017, Pages = 616, Price = 55.90m, IsRead = false, AddedAt = DateTime.UtcNow },
            new() { Title = "Refactoring", Author = "Martin Fowler", Year = 2018, Pages = 448, Price = 47.25m, IsRead = false, AddedAt = DateTime.UtcNow },
            new() { Title = "Code Complete", Author = "Steve McConnell", Year = 2004, Pages = 960, Price = 51.00m, IsRead = true, AddedAt = DateTime.UtcNow },
            new() { Title = "SQL Antipatterns", Author = "Bill Karwin", Year = 2010, Pages = 328, Price = 34.75m, IsRead = false, AddedAt = DateTime.UtcNow },
        };

        db.Books.AddRange(books);

        Console.WriteLine($"BookId before SaveChanges: {books[0].BookId}"); // 0 - no value from the DB yet

        db.SaveChanges();

        Console.WriteLine($"BookId after SaveChanges: {books[0].BookId}"); // populated via RETURNING
    }

    // ---------------- Section 4 ----------------

    private static void AllBooksByYear(LibraryContext db)
    {
        var books = db.Books.OrderBy(b => b.Year).ToList();
        foreach (var b in books)
            Console.WriteLine($"{b.Year} — {b.Title} — {b.Author}");
    }

    private static void BooksAfter2005(LibraryContext db)
    {
        var books = db.Books
            .Where(b => b.Year > 2005)
            .OrderBy(b => b.Title)
            .ToList();

        foreach (var b in books)
            Console.WriteLine($"{b.Title} ({b.Year})");
    }

    private static void BooksByFowler(LibraryContext db)
    {
        var books = db.Books
            .Where(b => b.Author == "Martin Fowler")
            .ToList();

        foreach (var b in books)
            Console.WriteLine(b.Title);
    }

    private static void LongestBook(LibraryContext db)
    {
        var longest = db.Books
            .OrderByDescending(b => b.Pages)
            .First(); // throws if the table is empty; FirstOrDefault would return null

        Console.WriteLine($"{longest.Title} — {longest.Pages} pages");
    }

    private static void AnyUnread(LibraryContext db)
    {
        bool anyUnread = db.Books.Any(b => !b.IsRead);
        Console.WriteLine($"Any unread: {anyUnread}");
    }

    private static void CountRead(LibraryContext db)
    {
        int readCount = db.Books.Count(b => b.IsRead);
        Console.WriteLine($"Read count: {readCount}");
    }

    private static void TitlesAndYears(LibraryContext db)
    {
        var projected = db.Books
            .Select(b => new { b.Title, b.Year })
            .ToList();

        foreach (var p in projected)
            Console.WriteLine($"{p.Title} ({p.Year})");

        Console.WriteLine($"Tracked entries: {db.ChangeTracker.Entries().Count()}");
    }

    private static void SecondPage(LibraryContext db)
    {
        var page2 = db.Books
            .OrderBy(b => b.Title)
            .Skip(3)
            .Take(3)
            .ToList();

        foreach (var b in page2)
            Console.WriteLine(b.Title);
    }

    private static void FindVsFirst(LibraryContext db)
    {
        var anyBook = db.Books.First();
        int id = anyBook.BookId;

        var viaFind = db.Books.Find(id);
        var viaFirst = db.Books.First(b => b.BookId == id);

        Console.WriteLine($"Same instance: {ReferenceEquals(viaFind, viaFirst)}");
    }

    private static void TotalUnreadPrice(LibraryContext db)
    {
        decimal total = db.Books
            .Where(b => !b.IsRead)
            .Sum(b => b.Price);

        Console.WriteLine($"Total unread price: {total}");
    }

    private static void AuthorSearch(LibraryContext db)
    {
        var viaContains = db.Books
            .Where(b => b.Author.Contains("martin"))
            .ToList();
        Console.WriteLine($"Contains(\"martin\") rows: {viaContains.Count}");

        var viaILike = db.Books
            .Where(b => EF.Functions.ILike(b.Author, "%martin%"))
            .ToList();
        Console.WriteLine($"ILike rows: {viaILike.Count}");
    }

    // ---------------- Section 5 ----------------

    private static void MarkAsRead(LibraryContext db)
    {
        var book = db.Books.First(b => b.Title == "SQL Antipatterns");

        Console.WriteLine($"State before: {db.Entry(book).State}");
        book.IsRead = true;
        Console.WriteLine($"State after: {db.Entry(book).State}");

        db.SaveChanges(); // no Add, no Update - EF diffs against its snapshot
    }

    private static void DeleteBook(LibraryContext db)
    {
        var book = db.Books.First(b => b.Title == "The Pragmatic Programmer");
        db.Books.Remove(book);
        db.SaveChanges();

        int remaining = db.Books.Count();
        Console.WriteLine($"Remaining books: {remaining}");
    }

    private static void BreakItOnPurpose(LibraryContext db)
    {
        var book = db.Books.First(b => b.Title == "SQL Antipatterns");
        book.IsRead = true;
        db.Books.Add(book); // wrong on purpose - book is already tracked as Modified
        db.SaveChanges();
    }

    // ---------------- Section 6: bonus ----------------

    private static void PrintQueryString(LibraryContext db)
    {
        var query = db.Books.Where(b => b.Year > 2005).OrderBy(b => b.Title);
        Console.WriteLine(query.ToQueryString());
    }

    private static void NoTrackingComparison(LibraryContext db)
    {
        var tracked = db.Books.OrderBy(b => b.Year).ToList();
        Console.WriteLine($"Tracked entries after normal query: {db.ChangeTracker.Entries().Count()}");

        var untracked = db.Books.AsNoTracking().OrderBy(b => b.Year).ToList();
        Console.WriteLine($"Tracked entries after AsNoTracking query: {db.ChangeTracker.Entries().Count()}");
    }

    private static List<Book> Search(LibraryContext db, int? minYear, string? author, bool? isRead)
    {
        IQueryable<Book> query = db.Books;

        if (minYear.HasValue)
            query = query.Where(b => b.Year >= minYear.Value);

        if (!string.IsNullOrEmpty(author))
            query = query.Where(b => b.Author == author);

        if (isRead.HasValue)
            query = query.Where(b => b.IsRead == isRead.Value);

        return query.ToList(); // executes once, regardless of how many filters were applied
    }

    // ---------------- Section 7 ----------------

    private static void AddWithLocalTime(LibraryContext db)
    {
        var bad = new Book
        {
            Title = "Bad Timestamp Test",
            Author = "N/A",
            Year = 2024,
            Pages = 1,
            Price = 0m,
            IsRead = false,
            AddedAt = DateTime.Now
        };

        db.Books.Add(bad);

        try
        {
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
            db.ChangeTracker.Clear();
        }
    }

    private static void GroupByAuthor(LibraryContext db)
    {
        var grouped = db.Books
            .GroupBy(b => b.Author)
            .Select(g => new { Author = g.Key, Count = g.Count(), Total = g.Sum(x => x.Price) })
            .OrderByDescending(x => x.Count)
            .ToList();

        foreach (var g in grouped)
            Console.WriteLine($"{g.Author}: {g.Count} book(s), total {g.Total}");
    }
}