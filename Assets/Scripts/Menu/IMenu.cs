// Autor: Murillo Gomes Yonamine
// Data: 02/10/2026

/// <summary>Shared view lifecycle: show/hide authored UI and release event subscriptions on disposal.</summary>
public interface IMenu
{
    void Show();
    void Hide();
    void Dispose();
}
