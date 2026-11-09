using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Clube.Application.Services;
using Clube.Shared.DTOs;

namespace Clube.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventosController : ControllerBase
{
    private readonly IEventoService _eventoService;
    private readonly IAssociadoService _associadoService;

    public EventosController(IEventoService eventoService, IAssociadoService associadoService)
    {
        _eventoService = eventoService;
        _associadoService = associadoService;
    }

    [HttpGet]
    public async Task<ActionResult<List<EventoDto>>> Listar([FromQuery] Guid? clubeId)
    {
        Guid? associadoId = await ObterAssociadoIdUsuarioLogadoAsync();
        var eventos = await _eventoService.ListarEventosAsync(clubeId, associadoId);
        return Ok(eventos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EventoDto>> ObterPorId(Guid id)
    {
        Guid? associadoId = await ObterAssociadoIdUsuarioLogadoAsync();
        var evento = await _eventoService.ObterPorIdAsync(id, associadoId);
        if (evento == null) return NotFound();
        return Ok(evento);
    }

    [HttpPost]
    [Authorize(Roles = "Presidente,SuperAdmin")]
    public async Task<ActionResult<EventoDto>> Criar([FromBody] CriarEventoRequest request)
    {
        try
        {
            var evento = await _eventoService.CriarEventoAsync(request);
            return CreatedAtAction(nameof(ObterPorId), new { id = evento.Id }, evento);
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPost("{id}/inscrever")]
    [Authorize]
    public async Task<IActionResult> Inscrever(Guid id, [FromBody] InscricaoEventoRequest request)
    {
        var associadoId = await ObterAssociadoIdUsuarioLogadoAsync();
        if (!associadoId.HasValue)
            return BadRequest(new { mensagem = "Apenas associados ou simpatizantes cadastrados podem confirmar presença." });

        var sucesso = await _eventoService.InscreverAsync(id, associadoId.Value, request.QuantidadeAcompanhantes);
        if (!sucesso) return NotFound();

        return Ok(new { mensagem = "Presença confirmada com sucesso!" });
    }

    [HttpDelete("{id}/inscrever")]
    [Authorize]
    public async Task<IActionResult> CancelarInscricao(Guid id)
    {
        var associadoId = await ObterAssociadoIdUsuarioLogadoAsync();
        if (!associadoId.HasValue) return BadRequest();

        var sucesso = await _eventoService.CancelarInscricaoAsync(id, associadoId.Value);
        if (!sucesso) return NotFound();

        return Ok(new { mensagem = "Inscrição cancelada com sucesso." });
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
