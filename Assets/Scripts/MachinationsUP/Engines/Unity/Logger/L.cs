using System;
using System.IO;
using UnityEngine;

namespace MachinationsUP.Logger
{
    public enum LogLevel
    {

        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4,
        Trace = 5
    }

    static public class L
    {

        static public LogLevel Level;
        static public string LogFilePath;

        static public void I (string text, UnityEngine.Object context = null)
        {
            if (Level < LogLevel.Info) return;
            Debug.Log(text, context);
        }

        static public void D (string text, UnityEngine.Object context = null)
        {
            if (Level < LogLevel.Debug) return;
            Debug.Log(text, context);
        }
        
        static public void T (string text, UnityEngine.Object context = null)
        {
            if (Level < LogLevel.Trace) return;
            Debug.Log(text, context);
        }

        static public void W (string text, UnityEngine.Object context = null)
        {
            if (Level < LogLevel.Warning) return;
            Debug.LogWarning(text, context);
        }

        static public void E (string text, UnityEngine.Object context = null)
        {
            if (Level < LogLevel.Error) return;
            Debug.LogError(text, context);
        }

        static public void Ex (Exception ex, UnityEngine.Object context = null)
        {
            if (Level < LogLevel.Error) return;
            Debug.LogError(ex, context);
        }

        static public void ExToLogFile (Exception ex, UnityEngine.Object context = null)
        {
            if (Level < LogLevel.Error) return;
            StreamWriter sw = new StreamWriter(LogFilePath, true);
            sw.WriteLine(DateTime.Now.ToString("u") + ": " + ex.Message);
            sw.WriteLine(ex.Source);
            sw.WriteLine(ex.StackTrace);
            sw.Close();
            while (ex.InnerException != null)
                ExToLogFile(ex.InnerException, context);
        }

        static public void ToLogFile (string text, UnityEngine.Object context = null)
        {
            if (Level < LogLevel.Error) return;
            StreamWriter sw = new StreamWriter(LogFilePath, true);
            sw.WriteLine(text);
            sw.Close();
        }

    }
}