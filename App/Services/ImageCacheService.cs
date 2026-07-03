using System.Security.Cryptography;
using System.Text;
using App.Configuration;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace App.Services;

public class ImageCacheService(IHttpClientFactory httpFactory, IOptions<ImageCacheSettings> opts)
{
    // Generous fetch cap — sources serve raw camera files; we downscale before saving.
    private const long MaxBytes = 20 * 1024 * 1024;
    private const int MaxWidth = 1200;

    private readonly string _cacheDir = Directory.CreateDirectory(opts.Value.Path).FullName;

    private static string HashKey(string imageUrl)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(imageUrl)))[..16];
    }

    public async Task<string?> GetOrFetchAsync(string imageUrl, CancellationToken ct)
    {
        var path = Path.Combine(_cacheDir, HashKey(imageUrl));

        if (File.Exists(path))
        {
            return path;
        }

        var tmp = path + ".tmp" + Guid.NewGuid().ToString("N")[..8];
        try
        {
            var client = httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; FindEvents/1.0)");

            using var resp = await client.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode || resp.Content.Headers.ContentLength > MaxBytes)
                return null;

            // Content-Length can be absent or lie — enforce the cap while buffering.
            using var buffered = new MemoryStream();
            await using (var src = await resp.Content.ReadAsStreamAsync(ct))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await src.ReadAsync(buffer, ct)) > 0)
                {
                    if (buffered.Length + read > MaxBytes)
                        return null;
                    buffered.Write(buffer, 0, read);
                }
            }
            buffered.Position = 0;

            // Downscale + re-encode: cards show ~400px, detail ~1200px. Decode
            // also validates the content — SKBitmap.Decode returns null (does
            // not throw) for non-images.
            using var bitmap = SKBitmap.Decode(buffered);
            if (bitmap is null)
                return null;

            var toEncode = bitmap;
            SKBitmap? resized = null;
            try
            {
                if (bitmap.Width > MaxWidth)
                {
                    var info = new SKImageInfo(MaxWidth, bitmap.Height * MaxWidth / bitmap.Width);
                    resized = bitmap.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell));
                    if (resized is not null)
                        toEncode = resized;
                }

                using var data = toEncode.Encode(SKEncodedImageFormat.Jpeg, 80);
                if (data is null)
                    return null;
                await using var fs = File.Create(tmp);
                data.SaveTo(fs);
            }
            finally
            {
                resized?.Dispose();
            }

            File.Move(tmp, path, overwrite: true);
            return path;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // Fetch failed (timeout, DNS, connection reset) — caller falls back
            // to redirecting; the next request simply retries.
            return null;
        }
        finally
        {
            if (File.Exists(tmp))
                File.Delete(tmp);
        }
    }
}
