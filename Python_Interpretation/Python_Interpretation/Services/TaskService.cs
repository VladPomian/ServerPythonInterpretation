using Python_Interpretation.PythonScript;
using Python_Interpretation.Repository;
using Serilog;

namespace Python_Interpretation.Services
{
    public class TaskService
    {
        private Timer _timer;

        public TaskService()
        {
            _timer = new Timer(ExecuteMonthlyUpdate, null, TimeSpan.Zero, TimeSpan.FromDays(30));
        }

        private void ExecuteMonthlyUpdate(object? state)
        {
            try
            {
                Log.Information("Запуск ежемесячного обновления данных прогнозирования...");

                string scriptOutput = Process.ExecutePythonScript();

                if (!string.IsNullOrEmpty(scriptOutput))
                {
                    var outputParts = scriptOutput.Split(' ');
                    string forecastData = outputParts[0];
                    int forecastDataSize = int.Parse(outputParts[1]);
                    string forecastStatus = outputParts[2];

                    var archiveRepository = new ArchiveRepository();
                    archiveRepository.ArchiveForecastData(forecastData, forecastDataSize, forecastStatus);

                    Log.Information("Добавлены данные в архив: {Size}, {Status}", forecastDataSize, forecastStatus);
                }
                else
                {
                    Log.Warning("Ошибка: Вывод из Python скрипта пуст.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при добавлении данных в таблицу Archive");
            }
        }
    }
}
