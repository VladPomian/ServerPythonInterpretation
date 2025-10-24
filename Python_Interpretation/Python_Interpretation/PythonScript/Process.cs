using Serilog;
using System.Diagnostics;

namespace Python_Interpretation.PythonScript
{
    public class Process
    {
        private static string _pythonExePath = @"<PATH_TO_PYTHON_EXECUTABLE>";
        private static string _pythonScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Prediction_MoreInfo.py");

        public static string ExecutePythonScript()
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo()
                {
                    FileName = _pythonExePath,
                    Arguments = _pythonScriptPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                System.Diagnostics.Process process = new System.Diagnostics.Process { StartInfo = start };
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при запуске Python скрипта");
                return string.Empty;
            }
        }
    }
}
