# Transferências entre contas

API em ASP.NET Core (.NET 10) com SQL Server para transferências entre contas, com cheque especial, limite de valor e de tentativas por hora (com regras diferentes de dia e de noite) e transferências agendadas.

> Projeto em construção: este README cresce junto com o código.

## Como rodar

Pré-requisitos: .NET SDK 10 e Docker.

1. Suba o SQL Server:

   ```bash
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Simulado@2026" -p 1433:1433 --name sql -d mcr.microsoft.com/mssql/server:2022-latest
   ```

2. Crie o banco e as contas de exemplo:

   ```bash
   dotnet tool install --global dotnet-ef
   dotnet ef database update -p src/Transfers.Infrastructure -s src/Transfers.Api
   ```

3. Rode os testes:

   ```bash
   dotnet test
   ```

A connection string fica em `src/Transfers.Api/appsettings.Development.json` e aponta para esse container. A senha está ali só porque é um banco local e descartável. Fora daqui ela iria para variável de ambiente ou para um cofre de segredos.

## Contas de exemplo

O banco já nasce com quatro contas, pensadas para cobrir os cenários do enunciado. Todas têm os limites padrão: de dia, até R$ 5.000 e 5 tentativas por hora; de noite (20h às 6h), até R$ 1.000 e 3 tentativas por hora.

| Titular | Id | Saldo | Cheque especial | Situação |
|---|---|---|---|---|
| Ana Souza | `11111111-1111-1111-1111-111111111111` | 500 | 1.000 | ativa |
| Bruno Lima | `22222222-2222-2222-2222-222222222222` | -800 | 1.000 | ativa, já usando o cheque especial |
| Carla Mendes | `33333333-3333-3333-3333-333333333333` | 10.000 | 1.000 | ativa, saldo alto para testar os limites por hora |
| Diego Rocha | `44444444-4444-4444-4444-444444444444` | 0 | 0 | bloqueada |

## Como o projeto está organizado

São quatro projetos, e a regra é que a dependência sempre aponta para dentro:

- **Domain**: as regras de negócio. Não conhece banco, HTTP nem framework nenhum.
- **Application**: os casos de uso, que orquestram o domínio.
- **Infrastructure**: EF Core, SQL Server e tudo o que fala com o mundo de fora.
- **Api**: os controllers e a montagem da aplicação no `Program.cs`.

Pensei em separar mais (CQRS, MediatR, um projeto por feature), mas para o tamanho do problema seria desnecessário. Quatro camadas já deixam claro onde mora cada coisa, e o domínio fica testável com testes de unidade puros, sem banco e sem mock.

## Decisões de domínio

**Saldo com sinal.** Em vez de guardar "saldo" e "cheque especial usado" em campos separados, o saldo pode ficar negativo, e o disponível é `saldo + limite do cheque especial`. A regra de que um crédito cobre primeiro o cheque especial sai de graça: quem está com -800 e recebe 1.000 fica com 200.

**Erros de negócio têm motivo, não só mensagem.** Uma regra violada lança `DomainException` com um `FailureReason` (saldo insuficiente, limite por hora excedido, conta bloqueada...). O código decide pelo enum, e a mensagem fica para quem lê.

**Período noturno que cruza a meia-noite.** A noite vai das 20h às 6h, então não é um intervalo contínuo no relógio. O `NightPeriod` trata isso e usa intervalo meio aberto: 20:00 já é noite e 06:00 já é dia, sem buraco nem sobreposição na fronteira.

**Limites por hora em janela móvel.** "Por hora" aqui quer dizer os últimos 60 minutos, não a hora cheia do relógio. Na hora cheia, dava para fazer 5 transferências às 13:59 e mais 5 às 14:00.

**Toda tentativa vira registro.** Uma transferência rejeitada também é gravada, com status `Failed` e o motivo. Sem isso não daria para contar tentativas, e ainda fica o histórico do que aconteceu.

**Ciclo de vida explícito.** A transferência tem uma máquina de estados pequena, e qualquer transição fora dela é recusada:

```
Scheduled → Processing → Completed
                       → Failed
Scheduled → Cancelled
```

**O relógio vem de fora.** As factories e transições recebem o "agora" como parâmetro (`DateTimeOffset`) em vez de chamar `DateTime.Now` lá dentro. Isso torna o domínio determinístico e permite testar 19:59 e 20:00 sem depender da hora em que o teste roda.

## Decisões da camada de aplicação

**A Application define o que precisa, a Infrastructure entrega.** As interfaces dos repositórios e da unidade de trabalho moram na Application, e as implementações com EF Core ficam na Infrastructure. É inversão de dependência: o caso de uso não sabe que existe SQL Server.

**Interface só onde existe fronteira.** Repositórios têm interface porque são a fronteira com o banco. Os serviços da Application não têm, porque cada um tem uma única implementação e nada precisa trocá-la. Criar `ITransferService` só por hábito seria cerimônia.

**Uma transação, do começo ao fim.** `IUnitOfWork.ExecuteInTransactionAsync` abre a transação, roda o trabalho, salva e faz o commit. Se algo lançar exceção no meio, nada é gravado.

**O que conta na janela de 60 minutos.** Nas tentativas, contam as transferências concluídas e as rejeitadas. No valor, só as concluídas, porque uma rejeitada não movimentou dinheiro. Agendada e cancelada não contam em nenhum dos dois.

**Noite no horário de Brasília, configurável.** O início e o fim da noite e o fuso ficam na seção `TransferRules` do `appsettings`. O servidor pode rodar em UTC, mas "20h" é 20h para o cliente.

**Não encontrado não é regra de negócio.** Uma conta que não existe gera `NotFoundException` (404). Uma regra violada gera `DomainException` (422).

## Como uma transferência é processada

Tudo acontece dentro de uma única transação:

1. Valido a entrada: valor maior que zero e contas diferentes. Se isso falhar, a requisição nem chega a virar tentativa.
2. Travo as duas contas, sempre em ordem crescente de Id. Se duas transferências A→B e B→A chegarem juntas, as duas pedem os locks na mesma ordem, e isso evita deadlock.
3. Confiro, nessa ordem: as duas contas estão ativas, o número de tentativas na janela, o valor na janela e o saldo com o cheque especial.
4. Se tudo passou, debito, credito e marco a transferência como `Completed`. Se alguma regra falhou, ela vira `Failed` com o motivo.
5. Salvo e faço o commit. A tentativa rejeitada também é gravada, porque ela conta para o limite de tentativas.

O limite usado é o do período (dia ou noite) do momento da tentativa, mesmo que a janela de 60 minutos tenha começado no outro período.

O lock na conta de origem serializa as transferências dela. Por isso a contagem da janela e o saldo não mudam entre a checagem e o débito.

## Decisões de persistência

**Mapeamento fora do domínio.** As entidades não têm atributo nenhum do EF. Cada uma tem sua classe `IEntityTypeConfiguration` na Infrastructure.

**Limites como owned type.** Os limites por hora são um value object: não têm identidade nem existem sem a conta. Por isso viram colunas na própria tabela `Accounts`, sem tabela nem join extra.

**Ids `Guid` gerados pela aplicação.** O Id existe antes de ir ao banco, o que permite devolvê-lo na resposta sem esperar o `INSERT`.

**Transferência sem navegação para a conta.** No código, a transferência guarda só os Ids das contas, para manter os dois agregados independentes. No banco, as duas chaves estrangeiras existem e garantem a integridade. Elas usam `Restrict`: apagar uma conta nunca pode apagar o histórico de transferências, e duas FKs em cascata para a mesma tabela nem são aceitas pelo SQL Server.

**Índices pensados nas consultas.** Os dois índices em `Transfers` atendem as duas consultas quentes do sistema: `(SourceAccountId, ProcessedAt)` para a janela de 60 minutos, e `(Status, ScheduledFor)` para encontrar as transferências agendadas que já venceram.

**Lock com `UPDLOCK, ROWLOCK`.** A conta é lida com `SELECT ... WITH (UPDLOCK, ROWLOCK)` dentro da transação. O `UPDLOCK` faz uma segunda transação que queira a mesma conta esperar na leitura, e não só na escrita. Sem ele, as duas leriam o mesmo saldo antes de qualquer uma debitar. O `ROWLOCK` trava só a linha da conta, não a página inteira. O lock dura até o commit.

**Consultas de leitura sem rastreamento.** Consultar uma conta ou listar contas usa `AsNoTracking`, porque nada ali vai ser alterado.

**Enums como texto.** `Active` e `Failed` no banco são mais legíveis que `0` e `3`, e reordenar um enum não corrompe os dados.

**Dinheiro em `decimal(18,2)`.** Nunca `float`.
