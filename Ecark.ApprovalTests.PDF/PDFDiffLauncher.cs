using System.Diagnostics;
using ApprovalTests.Core;
using Microsoft.Win32;

namespace Ecark.ApprovalTests.PDF;

public class PDFDiffLauncher : IApprovalFailureReporter
{
    public void Report(string approved, string received)
    {
        OpenInEdge(approved);
        OpenInEdge(received);
    }

    private void OpenInEdge(string filePath)
    {
        
        string reporterPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe", "", null)?.ToString();
            
        var process = new ProcessStartInfo
        {
            FileName = reporterPath, // Edge executable
            Arguments = $"\"{filePath}\"",
            UseShellExecute = true
        };

        Process.Start(process);
    }
}


