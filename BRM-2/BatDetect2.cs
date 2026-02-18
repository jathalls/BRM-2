#if WINDOWS
using Python.Deployment;
using System.Diagnostics;
#endif

namespace BRM_2;
public class BatDetect2
{
	/*
	Since the Python.Runtime package is not available for .NET 10 this codes is
	conditional for Windows only, and will not compile on other platforms. 
	We will need to find a cross platform way to do this in the future, 
	but for now this is good enough for our needs.
	*/
    private static BatDetect2? _instance = null;
    public static BatDetect2 Instance 
    { 
        get 
        { 
            return _instance ??= new BatDetect2(); 
        } 
    }

    public bool isBatDetect2Installed()
    {
		#if WINDOWS
        return Installer.IsModuleInstalled("batdetect2");
		#else
		return false;
		#endif
    }

    public BatDetect2()
    {
       
    }

#if WINDOWS
    public async Task InstallPython()
    {
        await InstallPythonAsync();
    }

    private async Task InstallPythonAsync()
    {
        string pythonZip = "python-3.10.0-embed-amd64-bd2.zip";
        Debug.WriteLine($"PythonZip={pythonZip}");
        var assembly = typeof(BatDetect2).Assembly;
        Python.Deployment.Installer.Source = new Python.Deployment.Installer.EmbeddedResourceInstallationSource()
        {
            Assembly = assembly,
            ResourceName = pythonZip,
        };

        Debug.WriteLine($"Assembly={assembly.FullName}");
        Debug.WriteLine($"Resource={assembly.GetManifestResourceNames().FirstOrDefault(x => x.Contains(pythonZip))}");
        
        try
        {
            Python.Deployment.Installer.LogMessage += LogMessage;
            Debug.WriteLine($"Install to {Python.Deployment.Installer.InstallPath}");
            var installDirectory = Path.Combine(Python.Deployment.Installer.InstallPath, "python-3.10.0-embed-amd64-bd2");
            var dllPath = Path.Combine(installDirectory, "python310.dll");
            Debug.WriteLine($"dllPath={dllPath}");
            
            if (Directory.Exists(installDirectory))
            {
                if (!File.Exists(dllPath))
                {
                    Directory.Delete(installDirectory, true);
                    await Python.Deployment.Installer.SetupPython();
                }
            }
            else
            {
                await Python.Deployment.Installer.SetupPython();
            }

            if (!Installer.IsPythonInstalled())
            {
                Debug.WriteLine($"SetupPython Failed {Installer.EmbeddedPythonHome}");
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"SetupPython: {e}");
        }
        finally
        {
            Python.Deployment.Installer.LogMessage -= LogMessage;
        }
    }

    private static void LogMessage(string obj)
    {
        Debug.WriteLine($"Log: {obj}");
    }

    internal string ProcessFile(string destination)
    {
        string summary = "";
        List<BD2Classification> classifications = new List<BD2Classification>();
        
        if (!Installer.IsModuleInstalled("batdetect2"))
        {
            Debug.WriteLine("ProcessFile failed - no batdetect2");
            return summary;
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
            
            var processInfo = new ProcessStartInfo
            {
                FileName = "python",
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
#endif
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
