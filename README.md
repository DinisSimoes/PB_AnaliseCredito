# PB_AnaliseCredito

Worker responsável por escutar a fila analise-credito-queue, processar as mensagens com a análise de crédito e, ao final, enviar o resultado para a fila cartao-credito-queue.

## Lógica da análise

O cálculo do score de crédito foi implementado de forma simplificada, utilizando Math.Random, apenas para fins de demonstração.

>Em um ambiente real, essa lógica seria substituída por regras de negócio específicas e cálculos baseados em dados reais do cliente.

## Como rodar localmente

1. Associe o pacote PB_Common

2. Siga os mesmos passos descritos no README do projeto PB_Clientes para adicionar o pacote local.

3. Execute a sln normalmente após configurar o ambiente e as dependências.
