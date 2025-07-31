using System;
using System.IO;
using UnityEngine;

public static class DistanceLogger
{
    private static StreamWriter _writer;
    private static bool _initialized = false;
    private static string _filePath;

    public static void Initialize()
    {
        if (_initialized) return;
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logsDir = Path.Combine(Application.dataPath, "Logs");
        Directory.CreateDirectory(logsDir);
        _filePath = Path.Combine(logsDir, $"distance_log_{timestamp}.csv");
        _writer = new StreamWriter(_filePath, false);
        // Write header for full-trial format with efficiency
        _writer.WriteLine("trial,mode,hand,direct_distance,actual_path_distance,efficiency,hold_time,path_points");
        _writer.Flush();
        _initialized = true;
    }

    public static void LogFullTrial(int trialIndex, string mode, string hand, float directDist, float actualDist, float holdTime, string pathPoints)
    {
        if (!_initialized) Initialize();
        float efficiency = (actualDist > 0f) ? directDist / actualDist : 0f;
        string line = $"{trialIndex},{mode},{hand},{directDist:F3},{actualDist:F3},{efficiency:F3},{holdTime:F3},\"{pathPoints}\"";
        Debug.Log($"[DistanceLogger] {line}");
        _writer.WriteLine(line);
        _writer.Flush();
    }

    public static void Close()
    {
        if (_writer != null)
        {
            _writer.Flush();
            _writer.Close();
            _writer = null;
            _initialized = false;
        }
    }
} 