using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class TMDbImporter
    {
        private readonly ApplicationDbContext _context;
        private readonly string _apiKey;

        public TMDbImporter(ApplicationDbContext context, string apiKey)
        {
            _context = context;
            _apiKey = apiKey;
        }

        public async Task ImportMoviesAsync(int count = 1000)
        {
            using var client = new HttpClient();

            int page = 1;
            int imported = 0;

            while (imported < count)
            {
                try
                {
                    string url = $"https://api.themoviedb.org/3/movie/popular?api_key={_apiKey}&language=en-US&page={page}";
                    string json = await client.GetStringAsync(url);
                    await Task.Delay(200);

                    var data = JObject.Parse(json);
                    var results = data["results"];

                    foreach (var item in results)
                    {
                        if (imported >= count)
                            break;

                        string title = item["title"]?.ToString();
                        if (string.IsNullOrEmpty(title))
                            continue;

                        // -----------------------------
                        // Sprawdzenie duplikatów
                        // -----------------------------
                        if (_context.Movies.Any(m => m.Title == title))
                            continue;

                        string movieId = item["id"]?.ToString();
                        if (movieId == null)
                            continue;

                        JObject details = null;
                        JObject credits = null;

                        // -----------------------------
                        // Detale filmu (po polsku)
                        // -----------------------------
                        try
                        {
                            string detailsUrl = $"https://api.themoviedb.org/3/movie/{movieId}?api_key={_apiKey}&language=pl-PL";
                            string detailsJson = await client.GetStringAsync(detailsUrl);
                            details = JObject.Parse(detailsJson);
                            await Task.Delay(200);
                        }
                        catch
                        {
                            continue;
                        }


                        // -----------------------------
                        // Aktorzy i reżyser
                        // -----------------------------
                        try
                        {
                            string creditsUrl = $"https://api.themoviedb.org/3/movie/{movieId}/credits?api_key={_apiKey}";
                            string creditsJson = await client.GetStringAsync(creditsUrl);
                            credits = JObject.Parse(creditsJson);
                            await Task.Delay(200);
                        }
                        catch
                        {
                            continue;
                        }

                        // Aktorzy
                        string actors = "";
                        try
                        {
                            var cast = credits["cast"]
                                .Take(5)
                                .Select(c => c["name"]?.ToString());
                            actors = string.Join(", ", cast);
                        }
                        catch { }

                        // Reżyser
                        string director = "Unknown";
                        try
                        {
                            director = credits["crew"]
                                .FirstOrDefault(c => c["job"]?.ToString() == "Director")?["name"]?.ToString()
                                ?? "Unknown";
                        }
                        catch { }

                        // Gatunki
                        string genresStr = "";
                        try
                        {
                            var genres = details["genres"]
                                .Select(g => g["name"]?.ToString());
                            genresStr = string.Join(", ", genres);
                        }
                        catch { }

                        // Ocena
                        double rating = 0;
                        try
                        {
                            rating = details["vote_average"]?.ToObject<double>() ?? 0;
                        }
                        catch { }

                        // Plakat
                        string posterUrl = null;
                        try
                        {
                            string posterPath = item["poster_path"]?.ToString();
                            if (posterPath != null)
                                posterUrl = $"https://image.tmdb.org/t/p/w500{posterPath}";
                        }
                        catch { }

                        // Rok
                        int? year = null;
                        try
                        {
                            string date = details["release_date"]?.ToString();
                            if (!string.IsNullOrEmpty(date) && date.Length >= 4)
                                year = int.Parse(date.Substring(0, 4));
                        }
                        catch { }

                        // -----------------------------
                        // Dodajemy film
                        // -----------------------------
                        var movie = new Movie
                        {
                            Title = title,
                            Genre = genresStr,
                            Director = director,
                            Actors = actors,
                            Description = item["overview"]?.ToString(),
                            PosterUrl = posterUrl,
                            Year = year,
                            Rating = rating
                        };

                        _context.Movies.Add(movie);
                        imported++;
                    }

                    await _context.SaveChangesAsync();
                    page++;
                }
                catch
                {
                    await Task.Delay(1000);
                }
            }
        }
    }
}
