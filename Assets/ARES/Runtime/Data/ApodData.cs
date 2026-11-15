using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ares.Data
{
    [Serializable]
    public sealed class ApodRecord
    {
        public string date, title, explanation, media_type, url, hdurl, permalink, copyright;

        public string ImageUrl
        {
            get
            {
                // Since the 2026 API migration, url can be an HTML permalink rather than an image.
                if (ImageAddress(hdurl)) return hdurl;
                return ImageAddress(url) ? url : null;
            }
        }

        public bool ValidFor(string expectedDate)
        {
            return date == expectedDate && DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _) && media_type == "image" && !string.IsNullOrWhiteSpace(title)
                && title != "NASA Science" && !string.IsNullOrWhiteSpace(explanation) && ImageUrl != null;
        }

        public static bool ImageAddress(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https") return false;
            string path = uri.AbsolutePath.ToLowerInvariant();
            return !path.Contains("logo") && (path.EndsWith(".jpg") || path.EndsWith(".jpeg") || path.EndsWith(".png"));
        }

        public static string PlainText(string value) => System.Net.WebUtility.HtmlDecode(
            Regex.Replace(value ?? "", "<[^>]*>", " ")).Trim();
    }

    [Serializable]
    public sealed class ObservatoryEntry
    {
        public string date, title, summary, question, credit, sourceUrl, imageUrl, resource, licenseUrl;
    }

    [Serializable]
    public sealed class ObservatoryCollection
    {
        public string endpoint = "https://api.nasa.gov/planetary/apod";
        public ObservatoryEntry[] entries;
    }
}
