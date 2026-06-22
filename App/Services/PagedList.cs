namespace App.Services;

public class PagedList<T, TCursor>
{
    public List<T> Items { get; set; } = [];
    public TCursor? NextCursor { get; set; }
}
