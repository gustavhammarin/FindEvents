namespace App.Services;

public class PopupManager
{
    public event Action<Guid>? OnOpen;

    public void Open(Guid id) => OnOpen?.Invoke(id);
}
