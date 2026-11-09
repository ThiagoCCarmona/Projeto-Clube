using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Clube.Application.Interfaces;
using Clube.Application.Services;
using Clube.Domain.Entities;
using Clube.Domain.Enums;
using Clube.Infrastructure.Security;
using Clube.Shared.DTOs;
using Clube.Shared.Validation;

namespace Clube.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IClubeDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IAssociadoService _associadoService;

    public AuthController(IClubeDbContext context, ITokenService tokenService, IAssociadoService associadoService)
    {
        _context = context;
        _tokenService = tokenService;
        _associadoService = associadoService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var cpfLimpo = CpfValidator.ApenasDigitos(request.CPF);

        var usuario = await _context.Usuarios
            .IgnoreQueryFilters()
            .Include(u => u.Associado)
            .FirstOrDefaultAsync(u => u.CPF == cpfLimpo || u.Email == request.CPF);

        if (usuario == null || !PasswordHasher.Verificar(request.Senha, usuario.SenhaHash))
        {
            return Unauthorized(new LoginResponse
            {
                Sucesso = false,
                Mensagem = "CPF/E-mail ou senha incorretos."
            });
        }

        if (!usuario.Ativo)
        {
            return Unauthorized(new LoginResponse
            {
                Sucesso = false,
                Mensagem = "Conta inativa. Entre em contato com a administração do clube."
            });
        }

        var token = _tokenService.GerarToken(usuario);

        var associado = usuario.Associado;

        return Ok(new LoginResponse
        {
            Sucesso = true,
            Mensagem = "Autenticado com sucesso.",
            Token = token,
            UsuarioId = usuario.Id,
            ClubeId = usuario.ClubeId,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Perfil = usuario.Perfil,
            TipoAssociado = associado?.TipoAssociado,
            StatusAprovacao = associado?.StatusAprovacao,
            TemDireitoAVoto = associado?.TemDireitoAVoto() ?? false
        });
    }

    [HttpPost("register-public")]
    public async Task<ActionResult<AssociadoDto>> RegisterPublic([FromBody] RegistroPublicoRequest request)
    {
        try
        {
            var hash = PasswordHasher.Hash(request.Senha);
            var resultado = await _associadoService.RegistrarPublicoAsync(request, hash);
            return Ok(resultado);
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<LoginResponse>> ObterUsuarioLogado()
    {
        var sub = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var usuario = await _context.Usuarios
            .IgnoreQueryFilters()
            .Include(u => u.Associado)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (usuario == null) return NotFound();

        var associado = usuario.Associado;

        return Ok(new LoginResponse
        {
            Sucesso = true,
            UsuarioId = usuario.Id,
            ClubeId = usuario.ClubeId,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Perfil = usuario.Perfil,
            TipoAssociado = associado?.TipoAssociado,
            StatusAprovacao = associado?.StatusAprovacao,
            TemDireitoAVoto = associado?.TemDireitoAVoto() ?? false
        });
    }
}
