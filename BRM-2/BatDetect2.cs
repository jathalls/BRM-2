namespace BRM_2;
public class BatDetect2
{
	/*
	This code calls Python via subprocess for cross-platform compatibility.
	On Windows, it uses 'python' command; on macOS and Linux, it uses 'python3'.
	Ensure the batdetect2 Python package is installed in the target Python environment.
	*/
    private static BatDetect2? _instance = null;
    public static BatDetect2 Instance 
    { 
        get 
        { 
            return _instance ??= new BatDetect2(); 
        } 
    }

    public BatDetect2()
    {
       
    }


   

    private static void LogMessage(string obj)
    {
        Debug.WriteLine($"Log: {obj}");
    }

    internal async Task<string> ProcessFile(string destination)
    {
        string summary = "";
        List<BD2Classification> classifications = new List<BD2Classification>();
        var venv=Preferences.Get("python_venv_path", "");
        
        if (string.IsNullOrWhiteSpace(venv))
        {
            await Application.Current.MainPage.DisplayAlertAsync("Identify Python Environment",
                "Please identify the virtual environment folder in which BatDetect2 has been installed", "OK");
#if MACCATALYST
            string folder = await getFolder();
            if (Directory.Exists(folder))
            {
                NSUrl url = new NSUrl(folder);
                MauiLib1.SecurityScopedBookmarks.SaveFolderBookmark(folder, url);
                url=MauiLib1.SecurityScopedBookmarks.TryRestoreFolderFromBookmark(folder);
                venv = url.Path;
                Preferences.Set("python_venv_path", venv);
            }
#elif WINDOWS
            var folder = await getFolder();
            if (Directory.Exists(folder))
            {
                MauiLib1.SecurityScopedBookmarks.SaveFolderBookmark(folder, folder);
                folder=MauiLib1.SecurityScopedBookmarks.TryRestoreFolderFromBookmark(folder);
                venv = folder;
                Preferences.Set("python_venv_path", venv);
            }
#endif
        }
#if WINDOWS
       
        venv=MauiLib1.SecurityScopedBookmarks.TryRestoreFolderFromBookmark(venv) ;
#elif MACCATALYST
    venv=MauiLib1.SecurityScopedBookmarks.TryRestoreFolderFromBookmark(venv).Path ;
#endif
        if (!Directory.Exists(venv))
        {
            await Application.Current.MainPage.DisplayAlertAsync("Invalid Folder",
                "The specified folder does not exist", "OK");
            return "";
        }
        

        try
        {
            // Call Python via subprocess instead of embedding
            var pythonCode = $@"
import json
from batdetect2.api import process_file
try:
    results = process_file(r'{destination}')
    print(json.dumps(str(results)))
except Exception as e:
    print(json.dumps({{'error': str(e)}}))
";

            // Use Python from .venv directory
            string venvPath = venv;
            string pythonPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                pythonPath = Path.Combine(venvPath, "Scripts", "python.exe");
            }
            else // macOS and Linux
            {
                pythonPath = Path.Combine(venvPath, "bin", "python3");
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"-c \"{pythonCode}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.WriteLine($"Python error: {error}");
                    return summary;
                } 

                if (!string.IsNullOrEmpty(output))
                {
                    classifications = ProcessResults(output);
                    Debug.WriteLine($"\n\nResults:-\n {string.Join(", ", classifications)}\n");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ProcessFile error: {ex.Message}");
        }

        summary = GenerateSummary(classifications);
        return summary;
    }


    private async Task<string?> getFolder()

    {
        var folder=await FolderPicker.PickFolderAsync();
        return folder;
    }

    private string GenerateSummary(List<BD2Classification> classifications)
    {
        string summary = "";
        if(!classifications.Any()) return "No bat calls detected";
        
        var bats = classifications.Select(cl => cl.classification).Distinct().ToList();
        foreach(var bat in bats ?? new List<string>())
        {
            var prob = classifications
                .Where(cl => cl.classification.Equals(bat, StringComparison.OrdinalIgnoreCase))
                .Select(cl => cl.overall_prob)
                .Average();
            summary += $"{bat} ({prob:P1}), ";
        }
        return summary;
    }

    private List<BD2Classification> ProcessResults(string results)
    {
        List<BD2Classification> classifications = new List<BD2Classification>();
        var lines = results.Split("{");
        string id = "";
        
        foreach (var line in lines)
        {
            if(line.Contains("pred_dict")) continue;
            if (line.Contains("'id':")) { id = getId(line); }
            
            var bd2 = new BD2Classification(line, id);
            if (bd2.overall_prob > 0.3 && !string.IsNullOrWhiteSpace(bd2.classification))
            {
                classifications.Add(bd2);
            }
        }
        return classifications;
    }

    private string getId(string line)
    {
        var cleaned = line.Replace("{", "").Replace("}", "").Replace("'", "").Trim();
        var parts = cleaned.Split(",");
        
        foreach (var part in parts)
        {
            var kv = part.Split(":");
            if (kv.Length == 2 && kv[0].Trim().Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                return kv[1].Trim();
            }
        }
        return "";
    }

    private double GetValue(string v, dynamic tLine)
    {
        var parts = tLine.Split(",");
        foreach(var part in parts)
        {
            var kv = part.Split(":");
            if(kv.Length == 2 && kv[0].Trim().Replace("'","").Equals(v, StringComparison.OrdinalIgnoreCase))
            {
                if(double.TryParse(kv[1].Trim(), out double res))
                {
                    return res;
                }
            }
        }
        return 0.0;
    }

}

public class BD2Classification
{
    public double start_time { get; set; }
    public double end_time { get; set; }
    public int low_freq { get; set; }
    public int high_freq { get; set; }
    public string classification { get; set; }
    public double class_prob { get; set; }
    public double det_prob { get; set; }
    public int individual { get; set; }
    public string call_event { get; set; }

    public double overall_prob => class_prob * det_prob;
    public string id { get; set; }

    public override string ToString() =>
        $"start_time: {start_time}, end_time: {end_time}, low_freq: {low_freq}, high_freq: {high_freq}, classification: {classification}, class_prob: {class_prob}, det_prob: {det_prob}, individual: {individual}, call_event: {call_event}";

    public BD2Classification(string resultLine, string id)
    {
        this.id = id;
        if (!resultLine.Contains("start_time")) return;
        
        var parts = resultLine.Split(",");
        foreach (var part in parts)
        {
            var cleaned = part.Replace("{", "").Replace("}", "").Replace("'", "").Trim();
            var kv = cleaned.Split(":");
            
            if (kv.Length == 2)
            {
                var key = kv[0].Trim();
                var value = kv[1].Trim();
                
                switch (key)
                {
                    case "start_time":
                        if (double.TryParse(value, out double st)) start_time = st;
                        break;
                    case "end_time":
                        if (double.TryParse(value, out double et)) end_time = et;
                        break;
                    case "low_freq":
                        if (int.TryParse(value, out int lf)) low_freq = lf;
                        break;
                    case "high_freq":
                        if (int.TryParse(value, out int hf)) high_freq = hf;
                        break;
                    case "class":
                        classification = value;
                        break;
                    case "class_prob":
                        if (double.TryParse(value, out double cp)) class_prob = cp;
                        break;
                    case "det_prob":
                        if (double.TryParse(value, out double dp)) det_prob = dp;
                        break;
                    case "individual":
                        if (int.TryParse(value, out int ind)) individual = ind;
                        break;
                    case "event":
                        call_event = value;
                        break;
                }
            }
        }
    }
}
