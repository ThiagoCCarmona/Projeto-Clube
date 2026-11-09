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
public class ClubesController : ControllerBase
{
    private readonly IClubeService _clubeService;

    public ClubesController(IClubeService clubeService)
    {
        _clubeService = clubeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClubeDto>>> ListarTodos()
    {
        var clubes = await _clubeService.ListarTodosAsync();
        return Ok(clubes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClubeDto>> ObterPorId(Guid id)
    {
        var clube = await _clubeService.ObterPorIdAsync(id);
        if (clube == null) return NotFound();
        return Ok(clube);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ClubeDto>> CriarClube([FromBody] CriarClubeRequest request)
    {
        try
        {
            var clube = await _clubeService.CriarClubeAsync(request);
            return CreatedAtAction(nameof(ObterPorId), new { id = clube.Id }, clube);
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Presidente")]
    public async Task<IActionResult> AtualizarClube(Guid id, [FromBody] ClubeDto request)
    {
        var sucesso = await _clubeService.AtualizarClubeAsync(id, request);
        if (!sucesso) return NotFound();
        return NoContent();
    }
}
