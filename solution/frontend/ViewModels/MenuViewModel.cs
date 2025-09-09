using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels;

public sealed class MenuItemVm
{
    public long MenuItemId { get; init; }
    public string Sku { get; init; } = "";
    public string Name { get; init; } = "";
    public string? PictureUrl { get; init; }
    public decimal Price { get; init; }
    public bool IsDiscountable { get; init; }
    public bool IsPartOfCombo { get; init; }
    public bool IsAvailable { get; set; }
}

public sealed class MenuViewModel : INotifyPropertyChanged
{
    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<string> Groups { get; } = new();
    public ObservableCollection<MenuItemVm> Items { get; } = new();

    private string? _searchText;
    public string? SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); _ = RefreshAsync(); } }

    private string? _selectedCategory;
    public string? SelectedCategory { get => _selectedCategory; set { _selectedCategory = value; OnPropertyChanged(); _ = RefreshAsync(); } }

    private string? _selectedGroup;
    public string? SelectedGroup { get => _selectedGroup; set { _selectedGroup = value; OnPropertyChanged(); _ = RefreshAsync(); } }

    public ICommand RefreshCommand { get; }
    public ICommand SelectCategoryCommand { get; }
    public ICommand SelectGroupCommand { get; }
    public ICommand AddToOrderCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    public MenuViewModel()
    {
        RefreshCommand = new RelayCommand(async _ => await RefreshAsync());
        SelectCategoryCommand = new RelayCommand(async c => { SelectedCategory = c as string; await RefreshAsync(); });
        SelectGroupCommand = new RelayCommand(async g => { SelectedGroup = g as string; await RefreshAsync(); });
        AddToOrderCommand = new RelayCommand(async o => await AddToOrderAsync(o as MenuItemVm));
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // Optional: preload categories/groups if your API supports
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var svc = App.Menu; // MenuApiService
            if (svc is null)
            {
                Items.Clear();
                return;
            }

            var q = BuildQuery();
            var list = await svc.ListItemsAsync(q);

            Items.Clear();
            foreach (var it in list)
            {
                Items.Add(new MenuItemVm
                {
                    MenuItemId = it.Id,
                    Sku = it.Sku,
                    Name = it.Name,
                    PictureUrl = it.PictureUrl,
                    Price = it.SellingPrice,
                    IsDiscountable = it.IsDiscountable,
                    IsPartOfCombo = it.IsPartOfCombo,
                    IsAvailable = it.IsAvailable
                });
            }
        }
        catch
        {
            // TODO: Surface error to UI via a status bar/InfoBar when integrating with a page
        }
    }

    private Services.MenuApiService.ItemsQuery BuildQuery()
        => new Services.MenuApiService.ItemsQuery(SearchText, SelectedCategory, SelectedGroup, true);

    private async Task AddToOrderAsync(MenuItemVm? item)
    {
        if (item is null) return;
        // Integrate with Orders flow (e.g., App.OrdersApi.AddItemsAsync on current order)
        await Task.CompletedTask;
    }
}
