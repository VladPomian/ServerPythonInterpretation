using Python_Interpretation.Repository;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using Serilog;
using System.Text;

namespace Python_Interpretation.Services
{
    public class RabbitService
    {
        private static string lastRecordSource = "Actual";

        public async Task EstablishRabbitConnectionAsync()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "<YOUR_RABBITMQ_HOST>",
				Port = 5672,
				UserName = "<YOUR_RABBITMQ_USERNAME>",
				Password = "<YOUR_RABBITMQ_PASSWORD>"
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                Log.Information("RabbitMQ подключен.");
                channel.QueueDeclare(queue: "request_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
                Log.Information("Ожидание сообщений в очереди request_queue.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (sender, e) =>
                {
                    var body = e.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Log.Information("Получено сообщение: {Message}", message);

                    try
                    {
                        SendResponseMessage(channel, e);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Ошибка при обработке сообщения");
                    }
                };

                channel.BasicConsume(queue: "request_queue", autoAck: true, consumer: consumer);
                await Task.Delay(Timeout.Infinite);
            }
        }

        private static void SendResponseMessage(IModel channel, BasicDeliverEventArgs e)
        {
            var response = "Нет записей в таблице";
            StormPredictionModel? lastRecord = null;

            try
            {
                lastRecord = RetrieveLastForecastData();
                if (lastRecord != null)
                {
                    response = $"{lastRecord.TimeEnter} {lastRecord.Data} {lastRecord.SizeData} {lastRecord.Status}";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при получении данных");
            }

            var body = Encoding.UTF8.GetBytes(response);
            var replyTo = e.BasicProperties.ReplyTo;
            channel.BasicPublish(exchange: "", routingKey: replyTo, basicProperties: null, body: body);

            Log.Information("[{RetrievedFrom}] Ответ отправлен: {TimeEnter} {SizeData} {Status}",
                lastRecordSource, lastRecord?.TimeEnter.ToString("yyyy-MM-dd HH:mm:ss"), lastRecord?.SizeData, lastRecord?.Status);
        }

        private static StormPredictionModel RetrieveLastForecastData()
        {
            var archiveRepository = new ArchiveRepository();
            var data = archiveRepository.RetrieveLastRecordFromTable("Actual");

            if (data.Status != "Success")
            {
                PythonScript.Decoder.DecodeErrorDataAndSaveToFile(data.Data);
                data = archiveRepository.RetrieveLastRecordFromTable("LastSuccess");
                lastRecordSource = "LastSuccess";
            }

            return data;
        }
    }
}
