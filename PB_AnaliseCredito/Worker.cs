namespace PB_AnaliseCredito
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) 
        { 
            _logger.LogInformation("Worker de Análise de Crédito iniciado e escutando RabbitMQ..."); 
            while (!stoppingToken.IsCancellationRequested) 
            { 
                await Task.Delay(10000, stoppingToken); 
            } 
        }
    }
}
