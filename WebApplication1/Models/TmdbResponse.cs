public class TmdbResponse
{
    public int page { get; set; }
    public List<TmdbMovie> results { get; set; }
}

public class TmdbMovie
{
    public string title { get; set; }
    public string overview { get; set; }
    public string poster_path { get; set; }
    public List<int> genre_ids { get; set; }
    public double vote_average { get; set; }
    public string release_date { get; set; }
}
