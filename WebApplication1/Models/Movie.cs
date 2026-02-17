namespace WebApplication1.Models
{
    public class Movie
    {
        public int Id { get; set; }

        public string Title { get; set; }


        // Жанри (через кому)
        public string Genre { get; set; }

        // Режисер
        public string Director { get; set; }

        // Актори (через кому)
        public string Actors { get; set; }

        // Опис
        public string Description { get; set; }

        // Постер (URL)
        public string PosterUrl { get; set; }

        // Рік
        public int? Year { get; set; }

        public double Rating { get; set; }
    }
}


