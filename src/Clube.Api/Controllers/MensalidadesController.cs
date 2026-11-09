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
public class MensalidadesController : ControllerBase
{
    private readonly IMensalidadeService _mensalidadeService;
    private readonly IAssociadoService _associadoService;

    public MensalidadesController(IMensalidadeService mensalidadeService, IAssociadoService associadoService)
    {
        _mensalidadeService = mensalidadeService;
        _associadoService = associadoService;
    }

    [HttpGet]
    [Authorize(Roles = "Tesoureiro,Presidente,SuperAdmin")]
    public async Task<ActionResult<List<MensalidadeDto>>> Listar([FromQuery] Guid? clubeId, [FromQuery] StatusMensalidade? status, [FromQuery] string? competencia)
    {
        var lista = await _mensalidadeService.ListarMensalidadesAsync(clubeId, status, competencia);
        return Ok(lista);
    }

    [HttpGet("minhas")]
    public async Task<ActionResult<List<MensalidadeDto>>> ListarMinhas()
    {
        var associadoId = await ObterAssociadoIdUsuarioLogadoAsync();
        if (!associadoId.HasValue) return BadRequest(new { mensagem = "Associado não identificado." });

        var lista = await _mensalidadeService.ObterMensalidadesDoAssociadoAsync(associadoId.Value);
        return Ok(lista);
    }

    [HttpGet("resumo")]
    [Authorize(Roles = "Tesoureiro,Presidente,SuperAdmin")]
    public async Task<ActionResult<ResumoFinanceiroDto>> ObterResumo([FromQuery] Guid? clubeId)
    {
        var resumo = await _mensalidadeService.ObterResumoFinanceiroAsync(clubeId);
        return Ok(resumo);
    }

    [HttpPost("gerar-lote")]
    [Authorize(Roles = "Tesoureiro,SuperAdmin")]
    public async Task<IActionResult> GerarLote([FromBody] GerarLoteMensalidadesRequest request)
    {
        try
        {
            var gerados = await _mensalidadeService.GerarLoteMensalidadesAsync(request);
            return Ok(new { mensagem = $"Lote gerado com sucesso! Total de {gerados} mensalidades geradas com chave PIX.", total = gerados });
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPost("{id}/baixar")]
    [Authorize(Roles = "Tesoureiro,SuperAdmin")]
    public async Task<IActionResult> Baixar(Guid id, [FromBody] BaixarMensalidadeRequest request)
    {
        var sucesso = await _mensalidadeService.BaixarMensalidadeAsync(id, request);
        if (!sucesso) return NotFound();
        return Ok(new { mensagem = "Pagamento registrado e mensalidade baixada com sucesso." });
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
