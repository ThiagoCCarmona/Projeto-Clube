using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Clube.Application.Services;
using Clube.Domain.Enums;
using Clube.Shared.DTOs;

namespace Clube.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SolicitacoesController : ControllerBase
{
    private readonly ISolicitacaoService _solicitacaoService;
    private readonly IAssociadoService _associadoService;

    public SolicitacoesController(ISolicitacaoService solicitacaoService, IAssociadoService associadoService)
    {
        _solicitacaoService = solicitacaoService;
        _associadoService = associadoService;
    }

    [HttpGet]
    [Authorize(Roles = "Presidente,SuperAdmin")]
    public async Task<ActionResult<List<SolicitacaoDto>>> Listar([FromQuery] Guid? clubeId, [FromQuery] StatusSolicitacao? status)
    {
        var lista = await _solicitacaoService.ListarSolicitacoesAsync(clubeId, status);
        return Ok(lista);
    }

    [HttpGet("minhas")]
    public async Task<ActionResult<List<SolicitacaoDto>>> ListarMinhas()
    {
        var associadoId = await ObterAssociadoIdUsuarioLogadoAsync();
        if (!associadoId.HasValue) return BadRequest(new { mensagem = "Associado não identificado." });

        var lista = await _solicitacaoService.ObterSolicitacoesDoAssociadoAsync(associadoId.Value);
        return Ok(lista);
    }

    [HttpPost]
    public async Task<ActionResult<SolicitacaoDto>> Criar([FromBody] CriarSolicitacaoRequest request)
    {
        var associadoId = await ObterAssociadoIdUsuarioLogadoAsync();
        if (!associadoId.HasValue) return BadRequest(new { mensagem = "Associado não identificado." });

        var solicitacao = await _solicitacaoService.CriarSolicitacaoAsync(associadoId.Value, request);
        return Ok(solicitacao);
    }

    [HttpPost("{id}/responder")]
    [Authorize(Roles = "Presidente,SuperAdmin")]
    public async Task<IActionResult> Responder(Guid id, [FromBody] ResponderSolicitacaoRequest request)
    {
        var sucesso = await _solicitacaoService.ResponderSolicitacaoAsync(id, request.Resposta);
        if (!sucesso) return NotFound();
        return Ok(new { mensagem = "Resposta enviada ao associado com sucesso." });
    }

    private async Task<Guid?> ObterAssociadoIdUsuarioLogadoAsync()
    {
        var sub = User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(sub) && Guid.TryParse(sub, out var userId))
        {
            var associado = await _associadoService.ObterPorUsuarioIdAsync(userId);
            return associado?.Id;
        }
        return null;
    }
}
