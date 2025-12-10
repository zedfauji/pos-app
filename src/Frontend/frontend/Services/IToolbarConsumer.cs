namespace MagiDesk.Frontend.Services;

public interface IToolbarConsumer
{
    void OnAdd();
    void OnEdit();
    void OnDelete();
    void OnRefresh();
}
