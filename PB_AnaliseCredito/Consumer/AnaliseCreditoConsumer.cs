using MassTransit;
using PB_Common.Events;
using System.Diagnostics;
using System.Text.Json;

namespace PB_AnaliseCredito.Consumer
{
    public class AnaliseCreditoConsumer : IConsumer<ClienteCadastradoEvent>
    {
        private readonly ILogger<AnaliseCreditoConsumer> _logger;
        private static readonly ActivitySource Activity = new("PB_AnaliseCredito.Worker");

        public AnaliseCreditoConsumer(ILogger<AnaliseCreditoConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ClienteCadastradoEvent> context)
        {
            var traceparent = context.Headers.Get<string>("traceparent");
            var traceState = context.Headers.Get<string>("tracestate");

            ActivityContext parentContext = default;
            if (!string.IsNullOrEmpty(traceparent))
            {
                if (ActivityContext.TryParse(traceparent, traceState, out var ctx))
                    parentContext = ctx;
            }

            using var activity = Activity.StartActivity("ProcessarCreditoCliente", ActivityKind.Consumer, parentContext);
            activity?.SetTag("cliente.id", context.Message.ClienteId);
            

            try
            {
                var cliente = context.Message;
                int score = CalcularScore();
                activity?.SetTag("analise.score", score);
                int limite1 = 0;
                int? limite2 = null;
                if (score >= 101 && score <= 500) limite1 = 1000;
                else if (score >= 501) { limite1 = 5000; limite2 = 5000; }
                var proposta = new PropostaCreditoCriadaEvent(cliente.ClienteId, score, limite1, limite2);
                await context.Publish(proposta, publishContext =>
                {
                    if (activity != null)
                    {
                        publishContext.Headers.Set("traceparent", activity.Id);

                        if (!string.IsNullOrEmpty(activity.TraceStateString))
                            publishContext.Headers.Set("tracestate", activity.TraceStateString);

                        foreach (var item in activity.Baggage)
                            publishContext.Headers.Set(item.Key, item.Value);
                    }
                });
                _logger.LogInformation($"Proposta criada para cliente {context.Message.ClienteId}, score {score}");
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, $"Erro ao processar cliente {context.Message.ClienteId}");

                var payload = JsonSerializer.Serialize(context.Message);
                var failure = new PropostaFalhaEvent(
                    Guid.NewGuid(),
                    context.Message.ClienteId,
                    typeof(ClienteCadastradoEvent).AssemblyQualifiedName!,
                    payload,
                    Attempt: 1,
                    Reason: ex.ToString(),
                    OccurredUtc: DateTime.UtcNow
                );

                await context.Publish(failure, publishContext =>
                {
                    if (activity != null)
                    {
                        publishContext.Headers.Set("traceparent", activity.Id);
                        foreach (var item in activity.Baggage)
                            publishContext.Headers.Set(item.Key, item.Value);
                    }
                });
            }
        }

        private int CalcularScore() 
        { 
            var random = new Random(); 
            return random.Next(0, 1001); 
        }
    }
}
