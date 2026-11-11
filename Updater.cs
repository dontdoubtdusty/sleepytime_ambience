using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

// Looks for a newer version on GitHub Releases and installs it in place: downloads the release zip,
// swaps the app's files (the running ones are renamed to .old first, which Windows allows), then the
// app restarts itself. Network and file work run on a background task; the main thread only reads
// State and the strings, and calls Check, Install and Relaunch.
public class Updater
{
    public enum Status { Idle, Checking, UpToDate, NoRelease, Available, Downloading, Installing, Restart, Failed }

    const string Repo = "dontdoubtdusty/sleepytime_ambience";
    public const string HomePage = "https://github.com/" + Repo;
    public const string ReleasesPage = HomePage + "/releases/latest";

    public volatile Status State = Status.Idle;
    public volatile float Progress; // download progress, 0..1
    public string Latest = "", Error = "";
    readonly string current;
    string assetUrl = "";

    public Updater(string currentVersion) => current = currentVersion;

    public bool Busy => State is Status.Checking or Status.Downloading or Status.Installing or Status.Restart;
    public bool Available => State == Status.Available;
    static string AppDir => AppContext.BaseDirectory;

    // One line for the menu describing where things stand.
    public string StatusText => State switch
    {
        Status.Checking => "Checking for a new version...",
        Status.UpToDate => "You have the newest version.",
        Status.NoRelease => "No versions have been published yet.",
        Status.Available => $"Version {Latest} is ready to install.",
        Status.Downloading => $"Downloading... {Progress * 100:0}%",
        Status.Installing => "Installing...",
        Status.Restart => "Restarting...",
        Status.Failed => Error,
        _ => "Not checked yet.",
    };

    // Asks GitHub for the latest release (after an optional delay) and compares it with this version.
    public void Check(int delayMs = 0)
    {
        if (Busy) return;
        State = Status.Checking;
        Task.Run(async () =>
        {
            try
            {
                if (delayMs > 0) await Task.Delay(delayMs);
                using var http = Client();
                using var resp = await http.GetAsync($"https://api.github.com/repos/{Repo}/releases/latest");
                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) { State = Status.NoRelease; return; }
                resp.EnsureSuccessStatusCode();
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                // A release can carry a small "update" zip (no sound files) next to the full zip; the update zip is preferred.
                string url = "", full = "";
                foreach (var a in doc.RootElement.GetProperty("assets").EnumerateArray())
                {
                    string name = a.GetProperty("name").GetString() ?? "";
                    if (!name.EndsWith("-win-x64.zip", StringComparison.OrdinalIgnoreCase)) continue;
                    string link = a.GetProperty("browser_download_url").GetString() ?? "";
                    if (name.Contains("-update-", StringComparison.OrdinalIgnoreCase)) url = link; else full = link;
                }
                if (url == "") url = full;
                Latest = (doc.RootElement.GetProperty("tag_name").GetString() ?? "").TrimStart('v', 'V');
                assetUrl = url;
                if (!IsNewer(Latest, current)) State = Status.UpToDate;
                else if (url == "") { Error = $"Version {Latest} is out, but its download is not up yet."; State = Status.Failed; }
                else State = Status.Available;
            }
            catch (Exception ex) { Fail("Could not check for updates", ex); }
        });
    }

    // Downloads the new version, swaps the files and sets State to Restart when the app should relaunch.
    public void Install()
    {
        if (State != Status.Available) return;
        if (AppDir.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase))
        {
            Error = "This is a development build; updates only install into a downloaded copy.";
            State = Status.Failed;
            return;
        }
        Progress = 0;
        State = Status.Downloading;
        Task.Run(async () =>
        {
            string work = Path.Combine(Path.GetTempPath(), "DrowseUpdate");
            try
            {
                if (Directory.Exists(work)) Directory.Delete(work, true);
                Directory.CreateDirectory(work);
                string zip = Path.Combine(work, "update.zip");
                using (var http = Client())
                using (var resp = await http.GetAsync(assetUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    resp.EnsureSuccessStatusCode();
                    long total = resp.Content.Headers.ContentLength ?? 0, got = 0;
                    await using var src = await resp.Content.ReadAsStreamAsync();
                    await using var dst = File.Create(zip);
                    var buf = new byte[81920];
                    int n;
                    while ((n = await src.ReadAsync(buf)) > 0)
                    {
                        await dst.WriteAsync(buf.AsMemory(0, n));
                        got += n;
                        if (total > 0) Progress = (float)got / total;
                    }
                }
                State = Status.Installing;
                string unpacked = Path.Combine(work, "new");
                ZipFile.ExtractToDirectory(zip, unpacked);
                string exe = Directory.GetFiles(unpacked, "Drowse.exe", SearchOption.AllDirectories).FirstOrDefault()
                    ?? throw new InvalidDataException("the download did not contain the app");
                Swap(Path.GetDirectoryName(exe)!, AppDir);
                State = Status.Restart;
            }
            catch (Exception ex) { Fail("Could not install the update", ex); }
        });
    }

    // Starts the freshly installed copy; call after the window has closed.
    public static void Relaunch()
    {
        try { Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "--updated") { UseShellExecute = false, WorkingDirectory = AppDir }); }
        catch { }
    }

    // Deletes the .old files a previous update left behind (a few seconds in, once the old copy has exited).
    public static void CleanupSoon()
    {
        Task.Run(async () =>
        {
            await Task.Delay(5000);
            try { foreach (var f in Directory.GetFiles(AppDir, "*.old", SearchOption.AllDirectories)) try { File.Delete(f); } catch { } }
            catch { }
        });
    }

    // Copies every file from newDir over appDir, renaming existing files to .old first; undoes it all on failure.
    static void Swap(string newDir, string appDir)
    {
        var moved = new List<(string Dest, string Old)>();
        var added = new List<string>();
        try
        {
            foreach (var file in Directory.GetFiles(newDir, "*", SearchOption.AllDirectories))
            {
                string dest = Path.Combine(appDir, Path.GetRelativePath(newDir, file));
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                if (File.Exists(dest)) { File.Move(dest, dest + ".old", true); moved.Add((dest, dest + ".old")); }
                else added.Add(dest);
                File.Copy(file, dest);
            }
        }
        catch
        {
            foreach (var d in added) try { File.Delete(d); } catch { }
            foreach (var (dest, old) in moved)
                try { if (File.Exists(dest)) File.Delete(dest); File.Move(old, dest); } catch { }
            throw;
        }
    }

    // True when version a is higher than version b ("1.10" beats "1.9").
    static bool IsNewer(string a, string b)
    {
        static System.Version? P(string v) => System.Version.TryParse(v.Contains('.') ? v : v + ".0", out var r) ? r : null;
        var va = P(a); var vb = P(b);
        return va != null && vb != null && va > vb;
    }

    HttpClient Client()
    {
        var h = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        h.DefaultRequestHeaders.UserAgent.ParseAdd("Drowse/" + current);
        h.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return h;
    }

    // Records a readable error; network errors get a hint about the connection.
    void Fail(string what, Exception ex)
    {
        Error = ex is HttpRequestException or TaskCanceledException ? what + " (no internet connection?)." : $"{what}: {ex.Message}";
        State = Status.Failed;
    }
}
