# Projeto Clubes - Plano de Implementação Completo

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Construir o sistema completo **Projeto Clubes** em C# (.NET) composto por Backend RESTful Web API, Painel Web Administrativo em Blazor para gestão de federações e clubes, e Aplicativo Móvel Dedicado em .NET MAUI Nativo (XAML/MVVM) com arquitetura Offline-First (SQLite local e motor de sincronização), cobrindo aprovação de sócios, controle de reservas com bloqueio de datas contínuas, eventos com público-alvo e RSVP, e módulo de tesouraria com mensalidades e PIX.

**Architecture:** Solução baseada em Clean Architecture desacoplada em C# (.NET 10 / 11): `Clube.Domain` (entidades puras e regras de negócio), `Clube.Shared` (DTOs e validações), `Clube.Application` (casos de uso e sincronização), `Clube.Infrastructure` (EF Core multi-tenant, SQLite e JWT), `Clube.Api` (endpoints RESTful Swagger), `Clube.WebAdmin` (painel Blazor reativo com perfis SuperAdmin, Presidente e Tesoureiro) e `Clube.Mobile` (app MAUI nativo em XAML com banco SQLite local e sincronização transparente).

**Tech Stack:** C# 10/11, .NET 10/11, ASP.NET Core Web API, Blazor Web App, .NET MAUI (Android & Windows Desktop WinUI 3), Entity Framework Core, SQLite (`Microsoft.EntityFrameworkCore.Sqlite` e `sqlite-net-pcl`), JWT Bearer Authentication, xUnit, FluentAssertions.

**Spec:** `docs/superpowers/specs/2026-11-09-projeto-clube-design.md`

---

## Global Constraints

- Arquitetura 100% C# (.NET ecosystem).
- Multi-tenant com isolamento absoluto de dados entre clubes no EF Core via filtros globais de consulta.
- Suporte a três níveis de sócios (Fundador com voto, Convidado sem voto em avaliação, Simpatizante para eventos) e dependentes vinculados.
- Bloqueio contínuo de datas/horários para locação do Salão Social (data/hora início até data/hora fim).
- Aplicativo móvel dedicado nativo com suporte a modo offline via banco SQLite local e sincronização bidirecional.
- Mensalidades com valor fixado pelo tesoureiro, status financeiro transparente e código PIX Copia-e-Cola/QR Code gerado com a chave do clube.

---

## Review Focus

1. **Tentativa de reserva de salão social com datas sobrepostas:** O validador de domínio deve rejeitar qualquer solicitação cuja faixa de data/hora colida com uma reserva já confirmada ou evento do clube.
2. **Aprovação de novo sócio:** Um sócio cadastrado pelo app permanece com status `Pendente` e só ganha acesso a reservas e votações após aprovação explícita do Presidente do clube.
3. **Isolamento Multi-Clube:** Uma requisição com token de um presidente ou tesoureiro do Clube A jamais pode listar ou alterar dados do Clube B.
4. **Sincronização de reserva offline com conflito:** Quando duas reservas pendentes para a mesma data forem criadas offline, o servidor deve sinalizar concorrência para que o Presidente decida qual homologar, rejeitando a concorrente por conflito assim que uma for aprovada.
5. **Operação Offline no App Móvel:** Caso o dispositivo esteja sem conexão, consultas a mensalidades, eventos e dados do clube devem ser servidas instantaneamente pelo SQLite local, e novas ações enfileiradas no Outbox.

---

## Estrutura de Tarefas

### Task 1: Scaffolding da Solução .NET e Estrutura de Projetos
**Files:**
- Create: `ProjetoClube.sln`
- Create: `src/Clube.Domain/Clube.Domain.csproj`
- Create: `src/Clube.Shared/Clube.Shared.csproj`
- Create: `src/Clube.Application/Clube.Application.csproj`
- Create: `src/Clube.Infrastructure/Clube.Infrastructure.csproj`
- Create: `src/Clube.Api/Clube.Api.csproj`
- Create: `src/Clube.WebAdmin/Clube.WebAdmin.csproj`
- Create: `src/Clube.Mobile/Clube.Mobile.csproj`
- Create: `tests/Clube.Domain.Tests/Clube.Domain.Tests.csproj`
- Create: `tests/Clube.Application.Tests/Clube.Application.Tests.csproj`
- Create: `tests/Clube.Api.IntegrationTests/Clube.Api.IntegrationTests.csproj`
- Create: `.gitignore`

**Interfaces:**
- Produces: Estrutura modular da Clean Architecture compilável via `dotnet build`.

- [ ] **Step 1: Criar arquivos de projeto e vincular à solution**
- [ ] **Step 2: Configurar referências entre projetos (Domain -> Shared -> Application -> Infrastructure -> Api / WebAdmin / Mobile)**
- [ ] **Step 3: Criar .gitignore adequado para .NET e MAUI**
- [ ] **Step 4: Executar `dotnet build ProjetoClube.sln` e verificar sucesso**
- [ ] **Step 5: Inicializar git e criar commit inicial de scaffolding**

---

### Task 2: Entidades de Domínio, Enums e Regras de Negócio (Clube.Domain)
**Files:**
- Create: `src/Clube.Domain/Enums/*.cs` (Perfis, Tipos de Sócios, Status de Reserva, Status de Mensalidade, etc.)
- Create: `src/Clube.Domain/Entities/*.cs` (`Clube`, `Usuario`, `Associado`, `Dependente`, `Evento`, `InscricaoEvento`, `Espaco`, `ReservaEspaco`, `Mensalidade`, `SolicitacaoReclamacao`)
- Create: `src/Clube.Domain/Validation/DomainValidationException.cs`
- Test: `tests/Clube.Domain.Tests/ReservaEspacoTests.cs`
- Test: `tests/Clube.Domain.Tests/AssociadoTests.cs`
- Test: `tests/Clube.Domain.Tests/MensalidadeTests.cs`

**Interfaces:**
- Produces: Entidades ricas com validação de regras de negócio (sobreposição de horários, regras de voto, elegibilidade de dependentes).

- [ ] **Step 1: Escrever testes unitários para regras de reserva (colisão de horário contínuo)**
- [ ] **Step 2: Executar testes e verificar falha esperada**
- [ ] **Step 3: Implementar enums e entidades em `Clube.Domain`**
- [ ] **Step 4: Executar testes e verificar aprovação 100%**
- [ ] **Step 5: Commit atômico do domínio**

---

### Task 3: Contratos Compartilhados, DTOs e Validações (Clube.Shared)
**Files:**
- Create: `src/Clube.Shared/DTOs/AuthDTOs.cs`
- Create: `src/Clube.Shared/DTOs/ClubeDTOs.cs`
- Create: `src/Clube.Shared/DTOs/AssociadoDTOs.cs`
- Create: `src/Clube.Shared/DTOs/EventoDTOs.cs`
- Create: `src/Clube.Shared/DTOs/ReservaDTOs.cs`
- Create: `src/Clube.Shared/DTOs/MensalidadeDTOs.cs`
- Create: `src/Clube.Shared/DTOs/SolicitacaoDTOs.cs`
- Create: `src/Clube.Shared/DTOs/SyncDTOs.cs`
- Create: `src/Clube.Shared/Validation/CpfValidator.cs`
- Test: `tests/Clube.Domain.Tests/CpfValidatorTests.cs`

**Interfaces:**
- Produces: Contratos de entrada e saída consumidos pela Web API, Web Admin e Mobile App.

- [ ] **Step 1: Escrever testes unitários para o validador de CPF**
- [ ] **Step 2: Implementar classes de DTOs e validador de CPF em `Clube.Shared`**
- [ ] **Step 3: Executar testes e validar aprovação**
- [ ] **Step 4: Commit atômico dos DTOs compartilhados**

---

### Task 4: Camada de Infraestrutura, EF Core Multi-Tenant e Seed (Clube.Infrastructure)
**Files:**
- Create: `src/Clube.Infrastructure/Data/ClubeDbContext.cs`
- Create: `src/Clube.Infrastructure/Data/ITenantContext.cs`
- Create: `src/Clube.Infrastructure/Data/TenantContext.cs`
- Create: `src/Clube.Infrastructure/Data/DbInitializer.cs` (Seed com SuperAdmin, Clubes de Foz e Guaíra, Usuários, Eventos, Espaços e Mensalidades)
- Create: `src/Clube.Infrastructure/Security/PasswordHasher.cs`
- Create: `src/Clube.Infrastructure/Security/TokenService.cs`
- Test: `tests/Clube.Application.Tests/MultiTenancyFilterTests.cs`

**Interfaces:**
- Produces: `ClubeDbContext` com isolamento multi-tenant, repositórios e serviços de segurança.

- [ ] **Step 1: Escrever teste de isolamento de consultas entre clubes**
- [ ] **Step 2: Implementar DbContext com mapeamento EF Core, query filters por `ClubeId` e serviço de seed**
- [ ] **Step 3: Implementar gerador e validador de tokens JWT**
- [ ] **Step 4: Executar testes de multi-tenancy e verificar passagem**
- [ ] **Step 5: Commit atômico da infraestrutura**

---

### Task 5: Serviços de Aplicação, Gerador PIX e Motor de Sincronização (Clube.Application)
**Files:**
- Create: `src/Clube.Application/Services/IAuthService.cs` & `AuthService.cs`
- Create: `src/Clube.Application/Services/IClubeService.cs` & `ClubeService.cs`
- Create: `src/Clube.Application/Services/IAssociadoService.cs` & `AssociadoService.cs`
- Create: `src/Clube.Application/Services/IEventoService.cs` & `EventoService.cs`
- Create: `src/Clube.Application/Services/IReservaService.cs` & `ReservaService.cs`
- Create: `src/Clube.Application/Services/IMensalidadeService.cs` & `MensalidadeService.cs`
- Create: `src/Clube.Application/Services/ISolicitacaoService.cs` & `SolicitacaoService.cs`
- Create: `src/Clube.Application/Services/ISyncService.cs` & `SyncService.cs`
- Create: `src/Clube.Application/Services/IPixPayloadGenerator.cs` & `PixPayloadGenerator.cs`
- Test: `tests/Clube.Application.Tests/ReservaServiceTests.cs`
- Test: `tests/Clube.Application.Tests/SyncServiceTests.cs`
- Test: `tests/Clube.Application.Tests/PixPayloadGeneratorTests.cs`

**Interfaces:**
- Produces: Lógica de aplicação completa com resolução de concorrência e geração de strings EMV PIX Copia-e-Cola.

- [ ] **Step 1: Escrever testes unitários para o `SyncService` e gerador de PIX**
- [ ] **Step 2: Implementar os serviços de aplicação com regras de negócio completas**
- [ ] **Step 3: Executar testes unitários e garantir 100% de sucesso**
- [ ] **Step 4: Commit atômico da camada de aplicação**

---

### Task 6: ASP.NET Core Web API, Middlewares e Swagger (Clube.Api)
**Files:**
- Create: `src/Clube.Api/Controllers/AuthController.cs`
- Create: `src/Clube.Api/Controllers/ClubesController.cs`
- Create: `src/Clube.Api/Controllers/AssociadosController.cs`
- Create: `src/Clube.Api/Controllers/EventosController.cs`
- Create: `src/Clube.Api/Controllers/EspacosController.cs`
- Create: `src/Clube.Api/Controllers/ReservasController.cs`
- Create: `src/Clube.Api/Controllers/MensalidadesController.cs`
- Create: `src/Clube.Api/Controllers/SolicitacoesController.cs`
- Create: `src/Clube.Api/Controllers/SyncController.cs`
- Create: `src/Clube.Api/Program.cs`
- Test: `tests/Clube.Api.IntegrationTests/AuthControllerTests.cs`
- Test: `tests/Clube.Api.IntegrationTests/ReservasMultiTenantTests.cs`

**Interfaces:**
- Produces: API RESTful documentada via OpenAPI/Swagger com autenticação JWT e endpoints consumíveis.

- [ ] **Step 1: Escrever testes de integração para login e isolamento de rotas**
- [ ] **Step 2: Implementar controllers e pipeline de middlewares no `Program.cs`**
- [ ] **Step 3: Configurar Swagger UI com anotações e suporte a JWT**
- [ ] **Step 4: Executar testes de integração e validar funcionamento**
- [ ] **Step 5: Commit atômico da Web API**

---

### Task 7: Painel Web Administrativo em Blazor (Clube.WebAdmin)
**Files:**
- Create: `src/Clube.WebAdmin/Program.cs`
- Create: `src/Clube.WebAdmin/App.razor` & layouts modernos responsivos
- Create: `src/Clube.WebAdmin/Pages/Login.razor`
- Create: `src/Clube.WebAdmin/Pages/SuperAdmin/ClubesList.razor` & `ClubeEdit.razor`
- Create: `src/Clube.WebAdmin/Pages/Presidente/AssociadosAprovacao.razor`
- Create: `src/Clube.WebAdmin/Pages/Presidente/ReservasAgenda.razor`
- Create: `src/Clube.WebAdmin/Pages/Presidente/EventosGestao.razor`
- Create: `src/Clube.WebAdmin/Pages/Presidente/SolicitacoesAtendimento.razor`
- Create: `src/Clube.WebAdmin/Pages/Tesoureiro/MensalidadesPainel.razor`
- Create: `src/Clube.WebAdmin/Services/ApiClientService.cs`

**Interfaces:**
- Produces: Portal web moderno em Blazor para gestão de federações e clubes.

- [ ] **Step 1: Configurar layout base com navegação responsiva e suporte a temas**
- [ ] **Step 2: Implementar módulo SuperAdmin (cadastro e gestão de clubes)**
- [ ] **Step 3: Implementar módulo Presidente (fila de aprovação com filtros de sócios, agenda de reservas com detecção de concorrência e eventos)**
- [ ] **Step 4: Implementar módulo Tesoureiro (painel de adimplência, geração de lote de mensalidades e conciliação)**
- [ ] **Step 5: Testar e commitar módulo WebAdmin**

---

### Task 8: Aplicativo Móvel Dedicado em .NET MAUI com Offline-First (Clube.Mobile)
**Files:**
- Create: `src/Clube.Mobile/MauiProgram.cs`
- Create: `src/Clube.Mobile/App.xaml` & `AppShell.xaml`
- Create: `src/Clube.Mobile/Services/LocalDatabaseService.cs` (SQLite local `sqlite-net-pcl`)
- Create: `src/Clube.Mobile/Services/MobileSyncService.cs` (Fila Outbox e reconciliação)
- Create: `src/Clube.Mobile/ViewModels/*.cs` (Auth, Eventos, Reservas, Mensalidades, SAC)
- Create: `src/Clube.Mobile/Views/LoginPage.xaml` & `RegisterPage.xaml`
- Create: `src/Clube.Mobile/Views/EventosPage.xaml` (Mural com RSVP e filtro de interesses)
- Create: `src/Clube.Mobile/Views/ReservasPage.xaml` (Agenda com bloqueio contínuo e solicitação)
- Create: `src/Clube.Mobile/Views/MensalidadesPage.xaml` (Status financeiro e PIX Copia-e-Cola)
- Create: `src/Clube.Mobile/Views/SolicitacoesPage.xaml` (SAC e ouvidoria)
- Create: `src/Clube.Mobile/Views/Controls/OfflineBanner.xaml`

**Interfaces:**
- Produces: App móvel nativo responsivo com persistência local em SQLite e sincronização sob demanda/rede.

- [ ] **Step 1: Configurar banco local SQLite com tabelas espelho e fila de Outbox**
- [ ] **Step 2: Implementar ViewModels com lógica de cache e sincronização offline-first**
- [ ] **Step 3: Desenvolver telas XAML modernas (login/cadastro, eventos com RSVP, reservas de espaços, mensalidades com PIX e SAC)**
- [ ] **Step 4: Compilar e validar a solução MAUI**
- [ ] **Step 5: Commit atômico do aplicativo móvel**

---

### Task 9: Integração Ponta a Ponta, Execução de Testes e Validação de Requisitos
- [ ] **Step 1: Executar suite completa de testes automatizados (`dotnet test`)**
- [ ] **Step 2: Verificar aderência completa a todos os requisitos do vídeo e do bloco de notas**
- [ ] **Step 3: Testar fluxo multi-tenant (Foz vs Guaíra) e sincronização offline simulada**

---

### Task 10: Documentação Facilitada, Repositório GitHub e Finalização
- [ ] **Step 1: Redigir README.md completo e didático com instruções de build, execução e credenciais padrão**
- [ ] **Step 2: Configurar repositório remoto `https://github.com/ThiagoCCarmona/Projeto-Clube.git`**
- [ ] **Step 3: Realizar push completo para o GitHub**
- [ ] **Step 4: Verificação final de entrega com a tag GOAL_COMPLETE**
