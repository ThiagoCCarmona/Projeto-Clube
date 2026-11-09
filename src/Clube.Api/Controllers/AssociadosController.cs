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
public class AssociadosController : ControllerBase
{
    private readonly IAssociadoService _associadoService;

    public AssociadosController(IAssociadoService associadoService)
    {
        _associadoService = associadoService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AssociadoDto>>> Listar([FromQuery] Guid? clubeId, [FromQuery] StatusAprovacao? status)
    {
        var lista = await _associadoService.ListarAssociadosAsync(clubeId, status);
        return Ok(lista);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AssociadoDto>> ObterPorId(Guid id)
    {
        var associado = await _associadoService.ObterPorIdAsync(id);
        if (associado == null) return NotFound();
        return Ok(associado);
    }

    [HttpPost("{id}/aprovar")]
    [Authorize(Roles = "Presidente,SuperAdmin")]
    public async Task<IActionResult> Aprovar(Guid id, [FromBody] AprovarAssociadoRequest request)
    {
        var sucesso = await _associadoService.AprovarAssociadoAsync(id, request.TipoAssociadoDefinido);
        if (!sucesso) return NotFound();
        return Ok(new { mensagem = "Associado aprovado com sucesso." });
    }

    [HttpPost("{id}/rejeitar")]
    [Authorize(Roles = "Presidente,SuperAdmin")]
    public async Task<IActionResult> Rejeitar(Guid id, [FromBody] RejeitarAssociadoRequest request)
    {
        var sucesso = await _associadoService.RejeitarAssociadoAsync(id, request.Motivo);
        if (!sucesso) return NotFound();
        return Ok(new { mensagem = "Associado rejeitado com sucesso." });
    }
}
