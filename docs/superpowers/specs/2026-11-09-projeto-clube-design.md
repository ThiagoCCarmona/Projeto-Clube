# Documento de Design: Sistema Projeto Clubes (Multi-Clube)

- **Data:** 2026-11-09
- **Status:** Aprovado
- **Autor / Time:** Thiago Corobinski Carmona & Antigravity Pair Programming
- **Repositório:** `https://github.com/ThiagoCCarmona/Projeto-Clube.git`

---

## 1. Intenção e Contexto ("O Porquê")

O **Projeto Clubes** nasce da necessidade de modernizar e digitalizar a gestão de clubes e associações regionais e temáticas (ex.: Rotary Clubs, Iate Clubes, Clubes de Campo, Clubes de Caça e Pesca, etc.). Muitas dessas entidades possuem subsedes em múltiplas cidades (Foz do Iguaçu, Guaíra, Maringá, etc.) e operavam historicamente em processos manuais ou isolados.

O sistema atende a dois grandes ecossistemas conectados:
1. **Governança & Gestão Administrativa (Web):**
   - **Super Admin:** visão global da federação de clubes, criação e parametrização de novos clubes.
   - **Presidente (Admin do Clube):** coordenação das atividades, emissão de comunicados, moderação da fila de aprovação de novos sócios e simpatizantes, e aprovação de reservas de espaços.
   - **Tesoureiro:** lançamento e conciliação de mensalidades, visualização de adimplência/inadimplência.
2. **Engajamento dos Sócios e Famílias (Aplicativo Móvel Dedicado .NET MAUI):**
   - Associados Fundadores (com direito a voto), Convidados (sem voto em período de avaliação), Simpatizantes (público de causas e eventos beneficentes) e Dependentes.
   - Experiência **Offline-First**: capacidade de visualizar mensalidades, eventos, dados do clube e cadastrar solicitações/reservas mesmo em locais com sinal fraco ou sem internet (ex.: sedes campestres, beira de rios ou salões afastados), com sincronização automática bidirecional assim que a conexão for restabelecida.
   - Confirmação de Presença (RSVP) em eventos com indicação de dependentes/acompanhantes para estimativa precisa de público para a diretoria.

---

## 2. Metas, Não-Metas e Anti-Metas

### Metas (Goals)
- **Arquitetura 100% C# (.NET):** Backend ASP.NET Core Web API, Painel Web em Blazor, e App Mobile dedicado em .NET MAUI Nativo (XAML/MVVM).
- **Multi-Tenant Seguro:** Isolamento rigoroso dos dados de cada clube, com suporte a Super Admin global.
- **Ciclo de Vida do Associado:** Autocadastro via App Móvel com status "Pendente de Análise", aprovação/rejeição pelo Presidente, classificação de categorias e suporte a dependentes vinculados.
- **Gestão de Espaços e Salão Social:** Agenda de reservas com bloqueio por data e horário de início/fim contínuos (ex.: feijoadas beneficentes que ocupam da véspera até o dia seguinte).
- **Controle Financeiro de Mensalidades:** Cadastro de valores pelo tesoureiro, geração de cobranças periódicas, geração de código PIX Copia-e-Cola/QR Code com a chave do clube e conciliação transparente.
- **Canal de Interação:** Módulo de solicitações e reclamações do sócio para a diretoria.
- **Sincronização Offline-First:** Banco SQLite local no dispositivo móvel, cache de leitura e padrão Outbox para operações pendentes com detecção de conflitos no servidor.

### Não-Metas (Non-Goals)
- Integração de gateway de adquirente bancário real (cartão de crédito/débito online) em v1 — o foco é emissão de cobranças com chave PIX do clube, geração de recibos e baixa financeira pelo tesoureiro.
- Rede social aberta complexa ou streaming de vídeo dentro do app.

### Anti-Metas (Anti-Goals)
- **Vazamento de Dados Multi-Clube:** Administradores de um clube jamais podem visualizar membros, finanças ou reservas de outro clube.
- **Sobrescrita Silenciosa Offline:** Uma reserva feita offline não pode sobrescrever automaticamente uma reserva já homologada no servidor; o sistema deve detectar sobreposição de datas e alertar o usuário.

---

## 3. Abordagens Consideradas e Escolha de Arquitetura

1. **Abordagem 1 (.NET MAUI XAML Nativo + Blazor Web Admin + ASP.NET Core Web API) [ESCOLHIDA]:**
   - Oferece UI móvel 100% nativa com controles do sistema operacional via XAML/MVVM, banco SQLite embutido (`sqlite-net-pcl`), e painel administrativo em Blazor Web.
   - Trade-off aceito: separação entre as camadas visuais (XAML no mobile vs Razor na web), com máximo desacoplamento e aderência aos padrões de plataforma.
2. **Abordagem 2 (.NET MAUI Blazor Hybrid + Blazor Web):**
   - Compartilharia componentes Razor entre mobile e web. Descartada pelo usuário em favor do MAUI nativo puro em XAML.
3. **Abordagem 3 (Monolito MVC tradicional):**
   - Descartada por não atender ao padrão moderno de APIs desacopladas para app nativo offline-first.

---

## 4. Arquitetura da Solução e Projetos C#

A solução será organizada seguindo os princípios de **Clean Architecture**:

```
ProjetoClube.sln
├── src/
│   ├── Clube.Domain/               # Entidades, Enums, Interfaces de Repositório e Regras de Negócio
│   ├── Clube.Shared/               # DTOs, Requests/Responses, Constantes e Validações
│   ├── Clube.Application/          # Serviços de Aplicação, Casos de Uso, Lógica de Sincronização
│   ├── Clube.Infrastructure/       # EF Core, DbContext, Mapeamentos, Repositórios, Migrations, Auth JWT
│   ├── Clube.Api/                  # ASP.NET Core Web API (Endpoints RESTful, Swagger/OpenAPI, Middlewares)
│   ├── Clube.WebAdmin/             # Painel Web Blazor (Super Admin, Presidente, Tesoureiro)
│   └── Clube.Mobile/               # Aplicativo .NET MAUI (XAML, MVVM, SQLite Local, SyncService)
└── tests/
    ├── Clube.Domain.Tests/         # Testes unitários de entidades e regras de negócio
    ├── Clube.Application.Tests/    # Testes unitários de serviços e validação de conflitos
    └── Clube.Api.IntegrationTests/ # Testes de integração de endpoints e fluxo multi-tenant
```

---

## 5. Modelo de Dados

### 5.1. Entidades Principais
- **`Clube`:** `Id` (GUID), `Nome`, `Sigla`, `Endereco`, `Bairro`, `Cidade`, `UF`, `Telefone`, `Email`, `ChavePix`, `TipoChavePix`, `ValorMensalidadePadrao`, `Ativo`, `DataCadastro`.
- **`Usuario`:** `Id`, `ClubeId` (nullable para SuperAdmin), `Nome`, `CPF`, `Email`, `Telefone`, `SenhaHash`, `Perfil` (SuperAdmin, Presidente, Tesoureiro, Associado, Dependente, Simpatizante), `Ativo`.
- **`Associado`:** `Id`, `UsuarioId`, `ClubeId`, `TipoAssociado` (`SocioFundadorComVoto`, `SocioConvidadoSemVoto`, `Simpatizante`), `StatusAprovacao` (`Pendente`, `Aprovado`, `Rejeitado`), `DataNascimento`, `Sexo`, `EstadoCivil`, `Endereco`, `Bairro`, `Cidade`, `UF`, `Interesses` (tags: Pesca, Caça, Culinária, Bebidas, Eventos), `SocioTitularId` (se for dependente), `MotivoRejeicao`.
- **`Dependente`:** `Id`, `AssociadoTitularId`, `UsuarioId`, `GrauParentesco` (`Conjuge`, `Filho`, `Outro`), `DataNascimento`.
- **`Evento`:** `Id`, `ClubeId`, `Titulo`, `Descricao`, `TipoEvento` (`AlmocoDiaDasMaes`, `JantarDiaDosPais`, `CafeDaTarde`, `FeijoadaBeneficente`, `Geral`), `PublicoAlvo` (`SomenteAssociados`, `AbertoAoPublico`, `Simpatizantes`), `DataInicio`, `DataFim`, `Local`, `ValorIngresso`, `RequerInscricao`, `Ativo`.
- **`InscricaoEvento`:** `Id`, `EventoId`, `AssociadoId`, `DataInscricao`, `QuantidadeAcompanhantes`, `Status` (`Confirmada`, `Cancelada`).
- **`Espaco`:** `Id`, `ClubeId`, `Nome` (ex: "Salão Social Principal", "Quiosque Churrasqueira"), `Capacidade`, `Descricao`, `Ativo`.
- **`ReservaEspaco`:** `Id`, `EspacoId`, `ClubeId`, `AssociadoId`, `FinalidadeUso` (`Festa`, `Churrasco`, `Feijoada`, `Outro`), `DataHoraInicio`, `DataHoraFim`, `Status` (`PendenteAprovacao`, `Confirmada`, `Cancelada`, `Rejeitada`, `Conflito`), `Observacoes`, `CriadoOffline`, `SincronizadoEm`.
- **`Mensalidade`:** `Id`, `ClubeId`, `AssociadoId`, `Competencia` (MM/AAAA), `Valor`, `DataVencimento`, `DataPagamento`, `Status` (`Pendente`, `Paga`, `Atrasada`, `Isenta`), `CodigoPixCopiaECola`, `ComprovantePagamento`, `Observacoes`.
- **`SolicitacaoReclamacao`:** `Id`, `ClubeId`, `AssociadoId`, `Tipo` (`Solicitacao`, `Reclamacao`), `Assunto`, `Descricao`, `RespostaDiretoria`, `Status` (`Aberta`, `EmAnalise`, `Respondida`), `DataCriacao`, `DataResposta`.

---

## 6. Decisões do Construtor (Builder's Calls)

1. **Escopo Financeiro & PIX:**
   - Cada clube cadastra sua chave PIX no painel administrativo.
   - Ao gerar as mensalidades (para Sócios Fundadores e Convidados), o sistema gera automaticamente a string de pagamento PIX Copia-e-Cola e QR Code estático formatado com o valor e a identificação do clube.
   - O associado copia a chave/código no app móvel e o tesoureiro realiza a conferência e baixa financeira no painel web, sem dependência de APIs bancárias complexas de terceiros na v1.
2. **Plataformas-Alvo .NET MAUI:**
   - O aplicativo MAUI será compilado e homologado para **Android** e **Windows Desktop** (WinUI 3 nativo), permitindo testes imediatos e execução fluida no ambiente de desenvolvimento local Windows sem bloqueio de hardware macOS/iOS.
3. **Elegibilidade de Mensalidades:**
   - A cobrança de mensalidade recorrente aplica-se aos **Sócios Titulares** (Fundadores e Convidados).
   - Dependentes e Simpatizantes não possuem cobrança de mensalidade fixa recorrente (simpatizantes contribuem apenas em eventos abertos/beneficentes).
4. **Política de Conflito em Reservas Offline:**
   - Se uma reserva sincronizada conflitar com uma reserva **já Confirmada**, o servidor rejeita imediatamente com status `Conflito` e alerta o associado.
   - Se conflitar com outra reserva ainda **Pendente de Aprovação**, o servidor aceita ambas e notifica o Presidente no painel Blazor com badge visual de alerta ("Concorrência de Horário"), permitindo que a liderança do clube decida qual homologar. Ao confirmar uma, a concorrente é automaticamente atualizada para `RejeitadaPorConflito`.

---

## 7. Mecânica Offline-First e Sincronização

1. **Banco Local no Mobile (.NET MAUI):**
   - Utiliza `sqlite-net-pcl` para armazenamento local com tabelas espelho de leitura rápida (`Clubes`, `Eventos`, `Espacos`, `MinhasReservas`, `MinhasMensalidades`, `MinhasSolicitacoes`).
2. **Padrão Outbox Local:**
   - Toda alteração gerada no app (nova reserva, envio de solicitação/reclamação, atualização cadastral) é salva imediatamente no SQLite local com status `PendenteSincronizacao`.
3. **Ciclo de Sync (`SyncEngine`):**
   - Monitoramento do status de conectividade via `Connectivity.Current.NetworkAccess`.
   - Ao detectar internet (ou solicitação manual via "Puxe para atualizar"):
     - Envia lote de itens do Outbox para `POST /api/sync/push`.
     - O servidor valida integridade e concorrência.
     - O app baixa alterações incrementais via `GET /api/sync/pull?since={timestamp}` e atualiza o banco local.

---

## 8. Segurança, Autenticação e Perfis

- **JWT Bearer Authentication:** tokens assinados contendo claims de identidade: `Sub` (UserId), `ClubeId`, `Role` (SuperAdmin, Presidente, Tesoureiro, Associado, Dependente, Simpatizante).
- **Políticas Multi-Tenant:**
  - O `ClubeId` é injetado no contexto de banco do EF Core via Query Filters globais (`x => x.ClubeId == _currentTenant.ClubeId`), impedindo acesso cruzado acidental entre clubes.
  - O SuperAdmin tem bypass controlado para visão consolidada de todas as unidades.

---

## 9. Interface do Usuário (UI/UX)

- **Web Admin (Blazor):**
  - Layout limpo, moderno, com navegação lateral expansível, modo claro/escuro harmonioso, cards informativos (KPIs de associados ativos, arrecadação mensal, solicitações abertas).
  - Tabela interativa com busca, filtros por status e badges visuais claras.
  - Calendário interativo de ocupação do Salão Social.
- **Mobile App (.NET MAUI XAML):**
  - Padrão moderno de design com TabBar inferior (Início/Eventos, Espaços/Reservas, Mensalidades, Meu Clube/SAC).
  - Indicador visual explícito de conexão: badge discreta "Modo Offline - dados locais" / "Sincronizado há X min".
  - Formulários rápidos com validações visuais nativas e suporte a modo escuro.

---

## 10. Plano de Testes e Garantia de Qualidade

1. **Testes Unitários:**
   - Validações de CPF, regras de transição de status de associados (convidado vs fundador com voto).
   - Validador de sobreposição de datas em reservas de espaços (intervalo fechado contínuo).
   - Cálculo de atrasos e status de mensalidades.
2. **Testes de Integração:**
   - Criação de clube por SuperAdmin.
   - Cadastro público de sócio e aprovação pelo Presidente.
   - Simulação de concorrência e detecção de conflitos de reserva offline.
3. **Testes de Interface / API:**
   - Testes de endpoints com tokens de diferentes perfis para validar isolamento multi-tenant.

---

## 11. Publicação e Repositório GitHub

- Estrutura completa de `.gitignore` para .NET e MAUI.
- `README.md` detalhado e facilitado com instruções passo a passo para execução do backend, web admin e mobile app.
- Commits atômicos e organizados no repositório `https://github.com/ThiagoCCarmona/Projeto-Clube.git`.
