using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Linq;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MoviesController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task ImportMovies(int count)
        {
            int moviesPerPage = 20;
            int pages = count / moviesPerPage;

            using var http = new HttpClient();

            for (int page = 1; page <= pages; page++)
            {
                var url = $"https://api.themoviedb.org/3/movie/popular?api_key=c3a968ccde40c74dd498d3a0b363088f&page={page}";

                var json = await http.GetStringAsync(url);

                var data = JsonConvert.DeserializeObject<TmdbResponse>(json);

                foreach (var m in data.results)
                {
                    if (!_context.Movies.Any(x => x.Title == m.title))
                    {
                        _context.Movies.Add(new Movie
                        {
                            Title = m.title,
                            Description = m.overview,
                            PosterUrl = "https://image.tmdb.org/t/p/w500" + m.poster_path,
                            Genre = string.Join(", ", m.genre_ids),
                            Rating = m.vote_average,
                            Year = string.IsNullOrEmpty(m.release_date)
        ? null
        : int.Parse(m.release_date.Split('-')[0])
                        });

                    }
                }

                await _context.SaveChangesAsync();

                await Task.Delay(300); // пауза, щоб не перевищити ліміт API
            }
        }


        // -------------------------------
        // СТОРІНКА ФІЛЬМУ (Details)
        // -------------------------------
        public IActionResult Details(int id)
        {
            var movie = _context.Movies.FirstOrDefault(m => m.Id == id);
            if (movie == null)
                return NotFound();

            // Завантажуємо всі фільми в пам’ять
            var allMovies = _context.Movies.ToList();

            // Схожі за жанром
            ViewBag.SameGenre = allMovies
                .Where(m => m.Id != id &&
                            m.Genre != null &&
                            movie.Genre != null &&
                            m.Genre.Split(", ").Any(g => movie.Genre.Contains(g)))
                .Take(5)
                .ToList();

            // Схожі за режисером
            ViewBag.SameDirector = allMovies
                .Where(m => m.Id != id && m.Director == movie.Director)
                .Take(5)
                .ToList();

            // Схожі за акторами
            ViewBag.SameActors = allMovies
                .Where(m => m.Id != id &&
                            m.Actors != null &&
                            movie.Actors != null &&
                            m.Actors.Split(", ").Any(a => movie.Actors.Contains(a)))
                .Take(5)
                .ToList();

            // Схожі за сюжетом (AI-подібний пошук)
            ViewBag.SimilarPlot = FindSimilarMovies(movie);

            return View(movie);
        }


        // -------------------------------
        // AI-подібний пошук схожих фільмів
        // -------------------------------
        private List<Movie> FindSimilarMovies(Movie movie)
        {
            var allMovies = _context.Movies
                .Where(m => m.Id != movie.Id && m.Description != null)
                .ToList();

            var words = movie.Description
                .ToLower()
                .Split(' ', '.', ',', '!', '?', ':', ';')
                .Where(w => w.Length > 3)
                .Distinct()
                .ToList();

            var scored = allMovies
                .Select(m => new
                {
                    Movie = m,
                    Score = words.Count(w => m.Description.ToLower().Contains(w))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(5)
                .Select(x => x.Movie)
                .ToList();

            return scored;
        }
        private static readonly List<string> BackupPosters = new List<string>
        {
    "https://picsum.photos/300/450?random=1",
    "https://picsum.photos/300/450?random=2",
    "https://picsum.photos/300/450?random=3",
    "https://picsum.photos/300/450?random=4",
    "https://picsum.photos/300/450?random=5",
    "https://picsum.photos/300/450?random=6",
    "https://picsum.photos/300/450?random=7",
    "https://picsum.photos/300/450?random=8",
    "https://picsum.photos/300/450?random=9",
    "https://picsum.photos/300/450?random=10"
        };
        private void FixDuplicatePosters()
        {
            var movies = _context.Movies.ToList();

            var duplicates = movies
                .GroupBy(m => m.PosterUrl)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in duplicates)
            {
                bool first = true;

                foreach (var movie in group)
                {
                    if (first)
                    {
                        first = false; // перший залишаємо
                        continue;
                    }

                    // замінюємо постер на випадковий
                    movie.PosterUrl = BackupPosters[new Random().Next(BackupPosters.Count)];
                }
            }

            _context.SaveChanges();
        }


        // -------------------------------
        // Список фільмів
        // -------------------------------
        public async Task<IActionResult> Index()
        {
            FixDuplicatePosters();
            await ImportMovies(15000);

            return View(await _context.Movies.ToListAsync());
        }

        // -------------------------------
        // Список жанрів
        // -------------------------------
        public IActionResult Genre()
        {
            var genres = _context.Movies
                .Select(m => m.Genre)
                .ToList();

            var list = genres
                .SelectMany(g => g.Split(", "))
                .Distinct()
                .OrderBy(g => g)
                .ToList();

            return View(list);
        }
  


        ///////////////////////
        public IActionResult Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return View(new List<Movie>());

            var movies = _context.Movies
                .Where(m => m.Title.ToLower().Contains(query.ToLower()))
                .ToList();

            ViewBag.Query = query;

            return View(movies);
        }

        public IActionResult Years()
        {
            var years = _context.Movies
                .Where(m => m.Year != null)
                .Select(m => m.Year.Value)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            return View(years);
        }

        public IActionResult ByYear(int year)
        {
            var movies = _context.Movies
                .Where(m => m.Year == year)
                .ToList();

            ViewBag.Year = year;

            return View(movies);
        }




        // -------------------------------
        // Фільми за жанром
        // -------------------------------
        public IActionResult ByGenre(string genre)
        {
            var movies = _context.Movies
                .Where(m => m.Genre.Contains(genre))
                .ToList();

            ViewBag.Genre = genre;

            return View(movies);
        }

        private bool MovieExists(int id)
        {
            return _context.Movies.Any(e => e.Id == id);
        }
    }
}
