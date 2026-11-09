using System;
using Clube.Domain.Entities;
using Clube.Domain.Enums;

namespace Clube.WebAdmin.Services;

public class WebAdminSession
{
    public Usuario? UsuarioLogado { get; private set; }
    public Guid? CurrentClubeId => UsuarioLogado?.ClubeId;
    public PerfilUsuario Perfil => UsuarioLogado?.Perfil ?? PerfilUsuario.SuperAdmin;
    public string Nome => UsuarioLogado?.Nome ?? "Super Administrador";
    public bool IsAutenticado => UsuarioLogado != null;

    public event Action? OnChange;

    public void Login(Usuario usuario)
    {
        UsuarioLogado = usuario;
        NotifyStateChanged();
    }

    public void Logout()
    {
        UsuarioLogado = null;
        NotifyStateChanged();
    }

    public void AlternarPerfil(Usuario usuario)
    {
        UsuarioLogado = usuario;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
