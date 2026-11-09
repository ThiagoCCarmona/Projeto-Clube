using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clube.Application.Interfaces;
using Clube.Domain.Entities;
using Clube.Domain.Enums;
using Clube.Domain.Exceptions;
using Clube.Shared.DTOs;

namespace Clube.Application.Services;

public interface IClubeService
{
    Task<List<ClubeDto>> ListarTodosAsync();
    Task<ClubeDto?> ObterPorIdAsync(Guid id);
    Task<ClubeDto> CriarClubeAsync(CriarClubeRequest request);
    Task<bool> AtualizarClubeAsync(Guid id, ClubeDto request);
}

public class ClubeService : IClubeService
{
    private readonly IClubeDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ClubeService(IClubeDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<List<ClubeDto>> ListarTodosAsync()
    {
        var clubes = await _context.Clubes
            .IgnoreQueryFilters()
            .Include(c => c.Associados)
            .OrderBy(c => c.Nome)
            .ToListAsync();

        return clubes.Select(c => new ClubeDto
        {
            Id = c.Id,
            Nome = c.Nome,
            Sigla = c.Sigla,
            Endereco = c.Endereco,
            Bairro = c.Bairro,
            Cidade = c.Cidade,
            UF = c.UF,
            Telefone = c.Telefone,
            Email = c.Email,
            ChavePix = c.ChavePix,
            TipoChavePix = c.TipoChavePix,
            ValorMensalidadePadrao = c.ValorMensalidadePadrao,
            Ativo = c.Ativo,
            TotalAssociados = c.Associados.Count(a => a.StatusAprovacao == StatusAprovacao.Aprovado),
            AssociadosPendentes = c.Associados.Count(a => a.StatusAprovacao == StatusAprovacao.Pendente)
        }).ToList();
    }

    public async Task<ClubeDto?> ObterPorIdAsync(Guid id)
    {
        var c = await _context.Clubes
            .IgnoreQueryFilters()
            .Include(c => c.Associados)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (c == null) return null;

        return new ClubeDto
        {
            Id = c.Id,
            Nome = c.Nome,
            Sigla = c.Sigla,
            Endereco = c.Endereco,
            Bairro = c.Bairro,
            Cidade = c.Cidade,
            UF = c.UF,
            Telefone = c.Telefone,
            Email = c.Email,
            ChavePix = c.ChavePix,
            TipoChavePix = c.TipoChavePix,
            ValorMensalidadePadrao = c.ValorMensalidadePadrao,
            Ativo = c.Ativo,
            TotalAssociados = c.Associados.Count(a => a.StatusAprovacao == StatusAprovacao.Aprovado),
            AssociadosPendentes = c.Associados.Count(a => a.StatusAprovacao == StatusAprovacao.Pendente)
        };
    }

    public async Task<ClubeDto> CriarClubeAsync(CriarClubeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            throw new DomainValidationException("Nome do clube é obrigatório.");

        var clube = new Domain.Entities.Clube
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Sigla = request.Sigla,
            Endereco = request.Endereco,
            Bairro = request.Bairro,
            Cidade = request.Cidade,
            UF = request.UF,
            Telefone = request.Telefone,
            Email = request.Email,
            ChavePix = request.ChavePix,
            TipoChavePix = request.TipoChavePix,
            ValorMensalidadePadrao = request.ValorMensalidadePadrao,
            Ativo = true,
            DataCadastro = DateTime.UtcNow
        };

        _context.Clubes.Add(clube);

        // Cria o Salão Social padrão para o novo clube
        var salaoPadrao = new Espaco
        {
            Id = Guid.NewGuid(),
            ClubeId = clube.Id,
            Nome = "Salão Social Principal",
            Capacidade = 200,
            Descricao = "Salão de festas e eventos com cozinha e churrasqueira",
            ValorLocacao = 250.00m,
            Ativo = true
        };
        _context.Espacos.Add(salaoPadrao);

        // Se foram informados dados do presidente inicial
        if (!string.IsNullOrWhiteSpace(request.CpfPresidente) && !string.IsNullOrWhiteSpace(request.NomePresidente))
        {
            var presidente = new Usuario
            {
                Id = Guid.NewGuid(),
                ClubeId = clube.Id,
                Nome = request.NomePresidente,
                CPF = request.CpfPresidente,
                Email = request.EmailPresidente,
                Telefone = request.TelefonePresidente,
                SenhaHash = string.IsNullOrWhiteSpace(request.SenhaPresidente) 
                    ? "admin123" 
                    : request.SenhaPresidente, // O caller hash se necessário
                Perfil = PerfilUsuario.Presidente,
                Ativo = true
            };
            _context.Usuarios.Add(presidente);
        }

        await _context.SaveChangesAsync();
        return (await ObterPorIdAsync(clube.Id))!;
    }

    public async Task<bool> AtualizarClubeAsync(Guid id, ClubeDto request)
    {
        var clube = await _context.Clubes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (clube == null) return false;

        clube.Nome = request.Nome;
        clube.Sigla = request.Sigla;
        clube.Endereco = request.Endereco;
        clube.Bairro = request.Bairro;
        clube.Cidade = request.Cidade;
        clube.UF = request.UF;
        clube.Telefone = request.Telefone;
        clube.Email = request.Email;
        clube.ChavePix = request.ChavePix;
        clube.TipoChavePix = request.TipoChavePix;
        clube.ValorMensalidadePadrao = request.ValorMensalidadePadrao;
        clube.Ativo = request.Ativo;

        await _context.SaveChangesAsync();
        return true;
    }
}
