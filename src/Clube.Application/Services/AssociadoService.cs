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
using Clube.Shared.Validation;

namespace Clube.Application.Services;

public interface IAssociadoService
{
    Task<List<AssociadoDto>> ListarAssociadosAsync(Guid? clubeId = null, StatusAprovacao? status = null);
    Task<AssociadoDto?> ObterPorIdAsync(Guid id);
    Task<AssociadoDto?> ObterPorUsuarioIdAsync(Guid usuarioId);
    Task<AssociadoDto> RegistrarPublicoAsync(RegistroPublicoRequest request, string senhaHash);
    Task<bool> AprovarAssociadoAsync(Guid associadoId, TipoAssociado tipoDefinido);
    Task<bool> RejeitarAssociadoAsync(Guid associadoId, string motivo);
}

public class AssociadoService : IAssociadoService
{
    private readonly IClubeDbContext _context;
    private readonly ITenantContext _tenantContext;

    public AssociadoService(IClubeDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<List<AssociadoDto>> ListarAssociadosAsync(Guid? clubeId = null, StatusAprovacao? status = null)
    {
        var query = _context.Associados
            .Include(a => a.Usuario)
            .Include(a => a.Dependentes)
                .ThenInclude(d => d.Usuario)
            .AsNoTracking();

        if (clubeId.HasValue && _tenantContext.IsSuperAdmin)
        {
            query = query.Where(a => a.ClubeId == clubeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.StatusAprovacao == status.Value);
        }

        var lista = await query
            .OrderByDescending(a => a.DataSolicitacao)
            .ToListAsync();

        return lista.Select(MapearParaDto).ToList();
    }

    public async Task<AssociadoDto?> ObterPorIdAsync(Guid id)
    {
        var a = await _context.Associados
            .Include(x => x.Usuario)
            .Include(x => x.Dependentes)
                .ThenInclude(d => d.Usuario)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return a != null ? MapearParaDto(a) : null;
    }

    public async Task<AssociadoDto?> ObterPorUsuarioIdAsync(Guid usuarioId)
    {
        var a = await _context.Associados
            .Include(x => x.Usuario)
            .Include(x => x.Dependentes)
                .ThenInclude(d => d.Usuario)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);

        return a != null ? MapearParaDto(a) : null;
    }

    public async Task<AssociadoDto> RegistrarPublicoAsync(RegistroPublicoRequest request, string senhaHash)
    {
        var cpfLimpo = CpfValidator.ApenasDigitos(request.CPF);

        if (!CpfValidator.Validar(cpfLimpo))
            throw new DomainValidationException("CPF informado é inválido.");

        var jaExiste = await _context.Usuarios
            .IgnoreQueryFilters()
            .AnyAsync(u => u.CPF == cpfLimpo);

        if (jaExiste)
            throw new DomainValidationException("Já existe um cadastro com este CPF.");

        var clube = await _context.Clubes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == request.ClubeId);

        if (clube == null)
            throw new DomainValidationException("Clube selecionado não existe.");

        var perfilUsuario = request.TipoPretendido == TipoAssociado.Simpatizante 
            ? PerfilUsuario.Simpatizante 
            : PerfilUsuario.Associado;

        // 1. Cria usuário
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            ClubeId = request.ClubeId,
            Nome = request.Nome,
            CPF = cpfLimpo,
            Email = request.Email,
            Telefone = request.Telefone,
            SenhaHash = senhaHash,
            Perfil = perfilUsuario,
            Ativo = true,
            DataCadastro = DateTime.UtcNow
        };
        _context.Usuarios.Add(usuario);

        // 2. Cria associado com status Pendente de análise pelo Presidente
        var associado = new Associado
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            ClubeId = request.ClubeId,
            TipoAssociado = request.TipoPretendido,
            StatusAprovacao = StatusAprovacao.Pendente,
            DataNascimento = request.DataNascimento,
            Sexo = request.Sexo,
            EstadoCivil = request.EstadoCivil,
            Endereco = request.Endereco,
            Bairro = request.Bairro,
            Cidade = request.Cidade,
            UF = request.UF,
            Interesses = request.Interesses,
            DataSolicitacao = DateTime.UtcNow
        };
        _context.Associados.Add(associado);

        // 3. Dependentes (se houver)
        if (request.Dependentes != null && request.Dependentes.Count > 0)
        {
            foreach (var depDto in request.Dependentes)
            {
                var depCpf = CpfValidator.ApenasDigitos(depDto.CPF);
                var depUser = new Usuario
                {
                    Id = Guid.NewGuid(),
                    ClubeId = request.ClubeId,
                    Nome = depDto.Nome,
                    CPF = depCpf,
                    Email = depDto.Email,
                    Telefone = request.Telefone,
                    SenhaHash = senhaHash, // Mesma senha inicial ou redefinição
                    Perfil = PerfilUsuario.Dependente,
                    Ativo = true,
                    DataCadastro = DateTime.UtcNow
                };
                _context.Usuarios.Add(depUser);

                var dependente = new Dependente
                {
                    Id = Guid.NewGuid(),
                    AssociadoTitularId = associado.Id,
                    UsuarioId = depUser.Id,
                    GrauParentesco = depDto.GrauParentesco,
                    DataNascimento = depDto.DataNascimento
                };
                _context.Dependentes.Add(dependente);
            }
        }

        await _context.SaveChangesAsync();
        return (await ObterPorIdAsync(associado.Id))!;
    }

    public async Task<bool> AprovarAssociadoAsync(Guid associadoId, TipoAssociado tipoDefinido)
    {
        var associado = await _context.Associados
            .Include(a => a.Usuario)
            .FirstOrDefaultAsync(a => a.Id == associadoId);

        if (associado == null) return false;

        associado.TipoAssociado = tipoDefinido;
        associado.Aprovar();

        // Se foi aprovado como Simpatizante, perfil no usuário se mantém; se foi associado/convidado, perfil é Associado
        if (associado.Usuario != null)
        {
            associado.Usuario.Perfil = tipoDefinido == TipoAssociado.Simpatizante 
                ? PerfilUsuario.Simpatizante 
                : PerfilUsuario.Associado;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejeitarAssociadoAsync(Guid associadoId, string motivo)
    {
        var associado = await _context.Associados.FirstOrDefaultAsync(a => a.Id == associadoId);
        if (associado == null) return false;

        associado.Rejeitar(motivo);
        await _context.SaveChangesAsync();
        return true;
    }

    private static AssociadoDto MapearParaDto(Associado a)
    {
        return new AssociadoDto
        {
            Id = a.Id,
            UsuarioId = a.UsuarioId,
            ClubeId = a.ClubeId,
            Nome = a.Usuario?.Nome ?? string.Empty,
            CPF = a.Usuario?.CPF ?? string.Empty,
            Email = a.Usuario?.Email ?? string.Empty,
            Telefone = a.Usuario?.Telefone ?? string.Empty,
            TipoAssociado = a.TipoAssociado,
            StatusAprovacao = a.StatusAprovacao,
            TemDireitoAVoto = a.TemDireitoAVoto(),
            DataNascimento = a.DataNascimento,
            Sexo = a.Sexo,
            EstadoCivil = a.EstadoCivil,
            Endereco = a.Endereco,
            Bairro = a.Bairro,
            Cidade = a.Cidade,
            UF = a.UF,
            Interesses = a.Interesses,
            MotivoRejeicao = a.MotivoRejeicao,
            DataSolicitacao = a.DataSolicitacao,
            DataAprovacao = a.DataAprovacao,
            Dependentes = a.Dependentes.Select(d => new DependenteDto
            {
                Id = d.Id,
                AssociadoTitularId = d.AssociadoTitularId,
                UsuarioId = d.UsuarioId,
                Nome = d.Usuario?.Nome ?? string.Empty,
                CPF = d.Usuario?.CPF ?? string.Empty,
                Email = d.Usuario?.Email ?? string.Empty,
                GrauParentesco = d.GrauParentesco,
                DataNascimento = d.DataNascimento
            }).ToList()
        };
    }
}
