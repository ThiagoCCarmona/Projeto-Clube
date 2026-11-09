using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Clube.Application.Services;
using Clube.Shared.DTOs;

namespace Clube.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservasController : ControllerBase
{
    private readonly IReservaService _reservaService;
    private readonly IAssociadoService _associadoService;

    public ReservasController(IReservaService reservaService, IAssociadoService associadoService)
    {
        _reservaService = reservaService;
        _associadoService = associadoService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReservaDto>>> ListarTodas([FromQuery] Guid? clubeId)
    {
        var reservas = await _reservaService.ObterReservasPorClubeAsync(clubeId);
        return Ok(reservas);
    }

    [HttpGet("minhas")]
    public async Task<ActionResult<List<ReservaDto>>> ListarMinhas()
    {
        var associadoId = await ObterAssociadoIdUsuarioLogadoAsync();
        if (!associadoId.HasValue) return BadRequest(new { mensagem = "Associado não identificado." });

        var reservas = await _reservaService.ObterReservasDoAssociadoAsync(associadoId.Value);
        return Ok(reservas);
    }

    [HttpPost]
    public async Task<ActionResult<ReservaDto>> CriarReserva([FromBody] CriarReservaRequest request)
    {
        var associadoId = await ObterAssociadoIdUsuarioLogadoAsync();
        if (!associadoId.HasValue) return BadRequest(new { mensagem = "Associado não identificado." });

        try
        {
            var reserva = await _reservaService.CriarReservaAsync(associadoId.Value, request);
            return Ok(reserva);
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPost("{id}/aprovar")]
    [Authorize(Roles = "Presidente,SuperAdmin")]
    public async Task<IActionResult> Aprovar(Guid id)
    {
        var sucesso = await _reservaService.AprovarReservaAsync(id);
        if (!sucesso) return NotFound();
        return Ok(new { mensagem = "Reserva homologada com sucesso." });
    }

    [HttpPost("{id}/rejeitar")]
    [Authorize(Roles = "Presidente,SuperAdmin")]
    public async Task<IActionResult> Rejeitar(Guid id, [FromQuery] string motivo = "")
    {
        var sucesso = await _reservaService.RejeitarReservaAsync(id, motivo);
        if (!sucesso) return NotFound();
        return Ok(new { mensagem = "Reserva rejeitada." });
    }

    [HttpPost("{id}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        var associadoId = await ObterAssociadoIdUsuarioLogadoAsync();
        if (!associadoId.HasValue) return BadRequest();

        var sucesso = await _reservaService.CancelarReservaAsync(id, associadoId.Value);
        if (!sucesso) return NotFound();
        return Ok(new { mensagem = "Reserva cancelada." });
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
