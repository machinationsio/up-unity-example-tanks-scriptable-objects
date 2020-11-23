using System;
using UnityEngine;

namespace MachinationsUP.Logger
{
    public enum LogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Debug = 3,
        Info = 4
    }
    
    static public class L
    {
        
        static public LogLevel Level;
        
        static public void I(string text, UnityEngine.Object context = null)
        {
            if (Level < LogLevel.Info) return;
            Debug.Log(text, context);
        }
        
        static public void D (string text, UnityEngine.Object context = null)
        {
            if (Level < LogLevel.Debug) return;
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
            Debug.LogException(ex, context);
        }
        
    }
}