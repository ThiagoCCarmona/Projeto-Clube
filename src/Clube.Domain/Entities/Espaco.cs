using System;
using System.Collections.Generic;

namespace Clube.Domain.Entities;

public class Espaco
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubeId { get; set; }
    public string Nome { get; set; } = string.Empty; // ex: "Salão Social Principal", "Quiosque Churrasqueira"
    public int Capacidade { get; set; } = 100;
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorLocacao { get; set; } = 0.00m;
    public bool Ativo { get; set; } = true;

    public Clube? Clube { get; set; }
    public ICollection<ReservaEspaco> Reservas { get; set; } = new List<ReservaEspaco>();
}
