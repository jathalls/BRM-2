namespace BRM_2;

#if MACCATALYST
using Foundation;
#endif

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
        Debug.WriteLine($"[BatDetect2.ProcessFile] ===== STARTING =====");
        Debug.WriteLine($"[BatDetect2.ProcessFile] Current thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        Debug.WriteLine($"[BatDetect2.ProcessFile] Application.Current: {Application.Current}");
        Debug.WriteLine($"[BatDetect2.ProcessFile] Application.Current?.MainPage: {Application.Current?.MainPage}");
        NSUrl? url = null;
        string summary = "";
        List<BD2Classification> classifications = new List<BD2Classification>();
        var venv=Preferences.Get("python_venv_path", "");
        Debug.WriteLine($"[BatDetect2.ProcessFile] saved venv is '{venv}'");
        try
        {
            if (string.IsNullOrWhiteSpace(venv))
            {
                Debug.WriteLine($"[BatDetect2.ProcessFile] venv is empty, calling SafeDisplayAlertAsync");
                await SafeDisplayAlertAsync("Identify Python Environment",
                    "Please identify the virtual environment folder in which BatDetect2 has been installed", "OK");
                Debug.WriteLine($"[BatDetect2.ProcessFile] SafeDisplayAlertAsync returned");
                Debug.WriteLine($"[BatDetect2-Mac] get the folder");
                string folder = await getFolder();
#if MACCATALYST
               
                if (Directory.Exists(folder))
                {
                    Debug.WriteLine($"[BatDetect2-Mac] got {folder}");
                    url = NSUrl.CreateFileUrl( folder , true);
                    MauiLib1.SecurityScopedBookmarks.SaveFolderBookmark(folder, url);
                   
                    url.StartAccessingSecurityScopedResource();
                    venv = url.Path;
                    Preferences.Set("python_venv_path", venv);
                }
#elif WINDOWS
            
            if (Directory.Exists(folder))
            {
                venv = folder;
                Preferences.Set("python_venv_path", venv);
            }
#endif
            }

            if (!Directory.Exists(venv))
            {
                Debug.WriteLine($"[BatDetect2] not found {venv}");
                await SafeDisplayAlertAsync("Invalid Folder",
                    "The specified folder does not exist", "OK");
                return "";
            }

#if MACCATALYST
            // For MacCatalyst, restore and activate security-scoped bookmark if not already done
            if (url == null)
            {
                Debug.WriteLine($"[BatDetect2.ProcessFile] Restoring bookmark from preferences");
                url = MauiLib1.SecurityScopedBookmarks.TryRestoreFolderFromBookmark(venv);
                if (url != null)
                {
                    Debug.WriteLine($"[BatDetect2.ProcessFile] Successfully restored bookmark");
                    url.StartAccessingSecurityScopedResource();
                }
            }
#endif

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

#if MACCATALYST
                // On MacCatalyst, use the venv's Python since its compiled extensions (like CFFI) 
                // are built for that specific Python version. The sandbox is disabled so this works now.
                string pythonPath = Path.Combine(venvPath, "bin", "python3");
                var processInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"-c \"{pythonCode.Replace("\"", "\\\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                Debug.WriteLine($"[BatDetect2.ProcessFile] Executing venv Python at {pythonPath}");
#elif WINDOWS
                string pythonPath = Path.Combine(venvPath, "Scripts", "python.exe");
                var processInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"-c \"{pythonCode}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
#else
                // Linux and other Unix-like systems
                string pythonPath = Path.Combine(venvPath, "bin", "python3");
                var processInfo = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = $"-c \"{pythonCode}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
#endif

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
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"BatDetect2 error: {ex}");
        }
        finally
        {
#if MACCATALYST
            url?.StopAccessingSecurityScopedResource();
#endif
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
    
    
    /// <summary>
    /// Safe helper method to display alerts on all platforms including MacCatalyst.
    /// Handles potential null MainPage scenarios and threading issues.
    /// </summary>
    public static async Task SafeDisplayAlertAsync(string title, string message, string cancel)
    {
        Debug.WriteLine($"[SafeDisplayAlertAsync] Title: {title}");
        Debug.WriteLine($"[SafeDisplayAlertAsync] Message: {message}");
        Debug.WriteLine($"[SafeDisplayAlertAsync] Current thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        Debug.WriteLine($"[SafeDisplayAlertAsync] Is main thread: {MainThread.IsMainThread}");
        Debug.WriteLine($"[SafeDisplayAlertAsync] Application.Current: {Application.Current}");
        Debug.WriteLine($"[SafeDisplayAlertAsync] Application.Current?.MainPage: {Application.Current?.MainPage}");
        
        try
        {
            Debug.WriteLine($"[SafeDisplayAlertAsync] Checking if MainPage is not null...");
            if (Application.Current?.MainPage != null)
            {
                Debug.WriteLine($"[SafeDisplayAlertAsync] MainPage is NOT null");
                Debug.WriteLine($"[SafeDisplayAlertAsync] Checking if we're on main thread...");
                
                if (MainThread.IsMainThread)
                {
                    Debug.WriteLine($"[SafeDisplayAlertAsync] Already on main thread, calling DisplayAlertAsync directly");
                    await Application.Current.MainPage.DisplayAlertAsync(title, message, cancel);
                    Debug.WriteLine($"[SafeDisplayAlertAsync] DisplayAlertAsync completed successfully");
                }
                else
                {
                    Debug.WriteLine($"[SafeDisplayAlertAsync] NOT on main thread, using MainThread.BeginInvokeOnMainThread");
                    bool completed = false;
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            Debug.WriteLine($"[SafeDisplayAlertAsync.MainThread] Now on main thread, calling DisplayAlertAsync");
                            await Application.Current.MainPage.DisplayAlertAsync(title, message, cancel);
                            Debug.WriteLine($"[SafeDisplayAlertAsync.MainThread] DisplayAlertAsync completed");
                            completed = true;
                        }
                        catch (Exception innerEx)
                        {
                            Debug.WriteLine($"[SafeDisplayAlertAsync.MainThread] Exception: {innerEx.Message}");
                        }
                    });
                    
                    // Wait a bit for the main thread operation to complete
                    int waitCount = 0;
                    while (!completed && waitCount < 50)
                    {
                        await Task.Delay(100);
                        waitCount++;
                    }
                    Debug.WriteLine($"[SafeDisplayAlertAsync] Main thread operation completed after {waitCount * 100}ms");
                }
            }
            else
            {
                Debug.WriteLine($"[SafeDisplayAlertAsync] ERROR: MainPage is NULL!");
                Debug.WriteLine($"[SafeDisplayAlertAsync] Cannot display alert: {title}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SafeDisplayAlertAsync] EXCEPTION CAUGHT!");
            Debug.WriteLine($"[SafeDisplayAlertAsync] Exception Type: {ex.GetType().Name}");
            Debug.WriteLine($"[SafeDisplayAlertAsync] Error Message: {ex.Message}");
            Debug.WriteLine($"[SafeDisplayAlertAsync] StackTrace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Safe helper method to display confirmation dialogs on all platforms including MacCatalyst.
    /// </summary>
    public static async Task<bool> SafeDisplayAlertAsync(string title, string message, string accept, string cancel)
    {
        try
        {
            if (Application.Current?.MainPage != null)
            {
                return await Application.Current.MainPage.DisplayAlertAsync(title, message, accept, cancel);
            }
            else
            {
                Debug.WriteLine($"Confirmation (MainPage is null): {title} - {message}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error displaying confirmation: {ex.Message}");
            return false;
        }
    }

    private static string EscapeForShell(string input)
    {
        // Escape special characters for shell execution
        return input.Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("$", "\\$")
                   .Replace("`", "\\`")
                   .Replace("\n", "\\n")
                   .Replace("\r", "\\r");
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