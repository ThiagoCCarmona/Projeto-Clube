using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Clube.Application.Interfaces;
using Clube.Shared.DTOs;

namespace Clube.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EspacosController : ControllerBase
{
    private readonly IClubeDbContext _context;

    public EspacosController(IClubeDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<EspacoDto>>> Listar([FromQuery] Guid? clubeId)
    {
        var query = _context.Espacos.Where(e => e.Ativo).AsNoTracking();

        if (clubeId.HasValue)
            query = query.Where(e => e.ClubeId == clubeId.Value);

        var lista = await query.ToListAsync();

        return Ok(lista.Select(e => new EspacoDto
        {
            Id = e.Id,
            ClubeId = e.ClubeId,
            Nome = e.Nome,
            Capacidade = e.Capacidade,
            Descricao = e.Descricao,
            ValorLocacao = e.ValorLocacao,
            Ativo = e.Ativo
        }).ToList());
    }
}
