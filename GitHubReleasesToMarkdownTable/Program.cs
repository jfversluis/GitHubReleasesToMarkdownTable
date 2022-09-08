
using GitHubReleasesToMarkdownTable;
using Newtonsoft.Json;

Console.WriteLine("Please input GitHub API releases URL, for example: https://api.github.com/repos/jfversluis/Plugin.Maui.Audio/releases");

Console.WriteLine("There is very crude URL validation, if the URL is wrong it will produce unexpected results.");

var url = Console.ReadLine();

Console.WriteLine("Include prereleases? (y/n, default n)");

var includePrereleasesInput = Console.ReadKey();

while (includePrereleasesInput.Key != ConsoleKey.N
    && includePrereleasesInput.Key != ConsoleKey.Y
    && includePrereleasesInput.Key != ConsoleKey.Enter)
{
    Console.WriteLine();
    Console.BackgroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Only options are y or n or Enter for default (n)");
    Console.ResetColor();

    includePrereleasesInput = Console.ReadKey();

    Console.WriteLine();
}

var includePrereleases = includePrereleasesInput.Key == ConsoleKey.Y;

if (string.IsNullOrWhiteSpace(url))
{
    Console.BackgroundColor = ConsoleColor.Red;
    Console.ForegroundColor = ConsoleColor.White;

    Console.WriteLine("No URL entered, exiting.");
    return;
}

if (url.EndsWith("/"))
{
    url = url.Substring(0, url.Length - 1);
}

if (!url.StartsWith("https://api.github.com/repos/", StringComparison.InvariantCultureIgnoreCase) || !url.EndsWith("/releases", StringComparison.InvariantCultureIgnoreCase))
{
    Console.BackgroundColor = ConsoleColor.Red;
    Console.ForegroundColor = ConsoleColor.White;

    Console.WriteLine("URL seems invalid, exiting.");
    return;
}

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("GeraldsAmazingReleaseInfoCollector", "1.0"));

var textResult = await httpClient.GetStringAsync(url);

var releases = JsonConvert.DeserializeObject<Release[]>(textResult) ?? throw new NullReferenceException();

if (!includePrereleases)
{
    releases = releases.Except(releases.Where(r => r.prerelease)).ToArray();
}

if (releases.Length == 0)
{
    Console.BackgroundColor = ConsoleColor.Yellow;

    Console.WriteLine("No records received from GitHub, possibly there are no releases (yet) or you have excluded prereleases while there are only prereleases in the results. Exiting.");

    return;
}

DateTime lastRelease = default;
List<string> lines = new();

foreach (var release in releases.OrderBy(r => r.published_at))
{
    if (release.draft)
        continue;

    var daysSinceLastRelease = Convert.ToInt32(lastRelease == default ? 0 : release.published_at.Subtract(lastRelease).TotalDays);

    lines.Add($"| [{release.name}]({release.html_url}) | {release.published_at.ToString("yyyy/MM/dd")} | {daysSinceLastRelease} |");

    lastRelease = release.published_at;
}

lines.Reverse();

lines.Insert(0, "| Release Name | Release Date | Days Since Last Release |");
lines.Insert(1, "|--------------|--------------|-------------------------|");

foreach (var line in lines)
{
    Console.WriteLine(line);
}