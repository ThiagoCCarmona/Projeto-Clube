using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Clube.Application.Services;
using Clube.Shared.DTOs;

namespace Clube.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly IAssociadoService _associadoService;

    public SyncController(ISyncService syncService, IAssociadoService associadoService)
    {
        _syncService = syncService;
        _associadoService = associadoService;
    }

    [HttpPost("push")]
    public async Task<ActionResult<SyncPushResponse>> Push([FromBody] SyncPushRequest request)
    {
        var associadoId = await ObterAssociadoIdUsuarioLogadoAsync();
        if (associadoId.HasValue)
        {
            request.AssociadoId = associadoId.Value;
        }

        var response = await _syncService.ProcessarPushAsync(request);
        return Ok(response);
    }

    [HttpGet("pull")]
    public async Task<ActionResult<SyncPullResponse>> Pull([FromQuery] DateTime? desde)
    {
        var associadoId = await ObterAssociadoIdUsuarioLogadoAsync();
        if (!associadoId.HasValue) return BadRequest(new { mensagem = "Associado não identificado." });

        var response = await _syncService.ProcessarPullAsync(associadoId.Value, desde);
        return Ok(response);
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
