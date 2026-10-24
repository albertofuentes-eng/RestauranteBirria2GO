using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteControl.Services;
using RestauranteControl.ViewModels;

namespace RestauranteControl.Controllers;

public class AccountController : BaseController
{
    private readonly AuthService _auth;

    public AccountController(AuthService auth)
    {
        _auth = auth;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View(new LoginVm());
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(LoginVm modelo)
    {
        var (ok, error) = await _auth.LoginAsync(modelo.Email, modelo.Password);
        if (ok)
        {
            return RedirectToAction("Index", "Home");
        }
        modelo.Password = "";
        modelo.Error = error;
        return View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _auth.CerrarSesionAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();

    [HttpGet]
    public IActionResult CambiarPassword() => View();

    [HttpPost]
    public async Task<IActionResult> CambiarPassword(string? actual, string? nueva, string? confirmar)
    {
        var error = await _auth.CambiarPasswordAsync(UsuarioId, actual, nueva, confirmar);
        if (error != null)
        {
            Falla(error);
            return View();
        }
        Exito("Contraseña actualizada correctamente.");
        return RedirectToAction("Index", "Home");
    }
}
