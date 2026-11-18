# Transferências entre contas

API em ASP.NET Core (.NET 10) com SQL Server, e um frontend em React, para transferências entre contas: cheque especial, limite de valor e de tentativas por hora (com regras diferentes de dia e de noite) e transferências agendadas.

> Projeto em construção: este README cresce junto com o código.

## Como rodar

Pré-requisitos: .NET SDK 10, Node.js 20 ou mais novo, e Docker.

1. Suba o SQL Server:

   ```bash
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Simulado@2026" -p 1433:1433 --name sql -d mcr.microsoft.com/mssql/server:2022-latest
   ```

2. Crie o banco e as contas de exemplo:

   ```bash
   dotnet tool install --global dotnet-ef
   dotnet ef database update -p src/Transfers.Infrastructure -s src/Transfers.Api
   ```

3. Suba a API (fica em `http://localhost:5009`, com o Swagger em `/swagger`):

   ```bash
   dotnet run --project src/Transfers.Api --launch-profile http
   ```

4. Em outro terminal, suba o frontend (fica em `http://localhost:5173`):

   ```bash
   cd frontend
   npm install
   npm run dev
   ```

5. Rode os testes:

   ```bash
   dotnet test
   ```

O frontend chama a API em `http://localhost:5009` por padrão. Para mudar o endereço, crie um `frontend/.env.local` a partir do `.env.example`. A API só aceita chamadas do navegador vindas das origens listadas em `Cors:AllowedOrigins`, que no ambiente de desenvolvimento é `http://localhost:5173`.

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

## Endpoints

Com a Api rodando (`dotnet run --project src/Transfers.Api --launch-profile http`), o Swagger fica em `http://localhost:5009/swagger`.

| Método | Rota | O que faz |
|---|---|---|
| `POST` | `/api/transfers` | Transferência imediata |
| `POST` | `/api/transfers/scheduled` | Agenda uma transferência (`scheduledFor` no futuro) |
| `POST` | `/api/transfers/{id}/cancel` | Cancela uma transferência agendada que ainda não rodou |
| `GET` | `/api/transfers/{id}` | Consulta uma transferência |
| `GET` | `/api/accounts` | Lista as contas |
| `GET` | `/api/accounts/{id}` | Consulta saldo, disponível e limites de uma conta |

### Respostas

- **201 Created**: a transferência foi concluída, ou agendada. O corpo traz a transferência, e o header `Location` aponta para ela.
- **422 Unprocessable Entity**: a requisição está correta, mas uma regra de negócio barrou. A resposta é um `ProblemDetails` com o campo `reason` (por exemplo `InsufficientFunds` ou `HourlyAttemptsExceeded`). Quando a recusa vem de uma tentativa que foi registrada, a resposta também traz o `transferId`, porque a tentativa ficou gravada como `Failed` e pode ser consultada.
- **404 Not Found**: alguma das contas, ou a transferência, não existe.
- **409 Conflict**: a transferência mudou de status por outra operação enquanto esta acontecia (por exemplo, um cancelamento ao mesmo tempo em que o agendador começou a processá-la).
- **400 Bad Request**: JSON malformado ou campo com tipo errado.

Escolhi responder 422 na recusa, em vez de 201 com `status: Failed`. Para quem consome a API, "deu certo ou não" fica no status HTTP, sem precisar abrir o corpo. Como a tentativa foi registrada, o `transferId` continua disponível para consulta.

## Como uma transferência é processada

Tudo acontece dentro de uma única transação:

1. Valido a entrada: valor maior que zero e contas diferentes. Se isso falhar, a requisição nem chega a virar tentativa.
2. Travo as duas contas, sempre em ordem crescente de Id. Se duas transferências A→B e B→A chegarem juntas, as duas pedem os locks na mesma ordem, e isso evita deadlock.
3. Confiro, nessa ordem: as duas contas estão ativas, o número de tentativas na janela, o valor na janela e o saldo com o cheque especial.
4. Se tudo passou, debito, credito e marco a transferência como `Completed`. Se alguma regra falhou, ela vira `Failed` com o motivo.
5. Salvo e faço o commit. A tentativa rejeitada também é gravada, porque ela conta para o limite de tentativas.

O limite usado é o do período (dia ou noite) do momento da tentativa, mesmo que a janela de 60 minutos tenha começado no outro período.

O lock na conta de origem serializa as transferências dela. Por isso a contagem da janela e o saldo não mudam entre a checagem e o débito.

## Transferências agendadas

Ao agendar, só confiro o que não muda com o tempo: valor positivo, contas diferentes, data no futuro e contas existentes. Saldo, conta ativa e limites por hora são conferidos **na hora da execução**, porque até lá tudo isso pode mudar. Por isso a agendada também só entra na contagem da janela de 60 minutos quando roda.

O cancelamento só vale para transferências em `Scheduled`. Uma transferência que já começou a ser processada não pode mais ser cancelada.

**Cancelamento e execução ao mesmo tempo.** As duas operações partem de `Scheduled`. O `Status` da transferência é um *concurrency token*: o `UPDATE` sai com `WHERE Status = 'Scheduled'`. Se outra operação mudou o status antes, nenhuma linha é afetada, o EF lança `DbUpdateConcurrencyException` e a API responde 409. Só uma das duas vence.

Aqui o controle é otimista, diferente do lock pessimista das contas. O conflito entre cancelar e executar é raro e não precisa de retry: quem perdeu só precisa saber que perdeu.

**Quem executa as agendadas.** Um `BackgroundService` (`ScheduledTransfersWorker`) acorda a cada 10 segundos e busca as transferências em `Scheduled` com `ScheduledFor` já vencido, em lotes de 50. O intervalo e o tamanho do lote ficam na seção `Scheduler` do `appsettings`. Cada transferência roda na sua própria transação, com exatamente o mesmo fluxo da imediata: lock nas contas, checagens, débito e crédito. A única diferença é que ela passa de `Scheduled` para `Processing` em vez de nascer em `Processing`. Se falhar na execução (sem saldo, conta bloqueada, limite), ela fica `Failed` com o motivo, como qualquer tentativa.

**Mais de uma instância rodando.** Se duas instâncias da API pegarem a mesma transferência, as duas tentam, mas o `WHERE Status = 'Scheduled'` deixa só uma gravar. A outra recebe o conflito, a transação dela é desfeita (inclusive o débito) e ela segue para a próxima. Não existe processamento em dobro.

**Uma transferência não derruba as outras.** Um erro em uma transferência não interrompe o lote, e um erro no ciclo inteiro (banco fora do ar, por exemplo) é registrado no log e tentado de novo no próximo ciclo.

O worker fica na Api, ao lado dos controllers, porque os dois são pontos de entrada: um recebe HTTP, o outro recebe o tempo passando. Ambos só chamam o `TransferService`.

## Frontend

React com TypeScript, criado com Vite, sem biblioteca de componentes nem de estado. Para quatro telas pequenas, `useState` e `fetch` resolvem, e o código fica fácil de ler.

- Toda chamada passa por um único cliente (`src/api.ts`). Quando a resposta não é 2xx, ele lança um `ApiError` com o `ProblemDetails` da API. Assim, as telas mostram o motivo de uma recusa (`reason`) sem precisar tratar cada status separadamente.
- Os tipos em `src/types.ts` espelham os contratos da API.
- O formulário faz a transferência imediata e a agendada. Uma recusa aparece com o motivo em português e, quando a tentativa ficou registrada, com o Id dela. Depois de cada envio, a lista de contas é recarregada para mostrar os saldos novos.
- Abaixo do formulário fica a consulta por Id. A última transferência enviada já aparece ali, sem precisar copiar o Id. Se ela estiver agendada, aparece o botão de cancelar. O botão só existe nesse status, mas a API continua sendo quem decide: se o worker pegar a transferência no meio do caminho, o cancelamento volta 409 ou 422 e a tela pede para consultar de novo.
- A data do agendamento é digitada no horário local do navegador e enviada à API em UTC (`toISOString()`). A API guarda tudo em UTC e só converte para o horário de Brasília para decidir se é dia ou noite.
- As versões dos pacotes estão fixadas no `package.json`, para o `npm install` instalar sempre a mesma coisa.

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
