using System;

namespace Clube.Domain.Entities;

public class Dependente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssociadoTitularId { get; set; }
    public Guid UsuarioId { get; set; }
    public string GrauParentesco { get; set; } = string.Empty; // Cônjuge, Filho, Outro
    public DateTime? DataNascimento { get; set; }

    public Associado? AssociadoTitular { get; set; }
    public Usuario? Usuario { get; set; }
}
