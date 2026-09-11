namespace Library.Pg
{
    internal class Book
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Pages { get; set; }
        public decimal Price { get; set; }
        public bool IsRead { get; set; }
        public DateTime AddedAt { get; set; }
    }
}
