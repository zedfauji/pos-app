using System;
using System.Collections.Generic;
using Windows.Storage;

namespace MagiDesk.Frontend.Services
{
    public enum AppLanguage { Eng, Esp }

    public class I18nService
    {
        private const string LangKey = "App.Language";
        private readonly Dictionary<AppLanguage, Dictionary<string, string>> _dict;

        public I18nService()
        {
            _dict = new()
            {
                [AppLanguage.Eng] = new Dictionary<string, string>
                {
                    ["sign_in"] = "Sign in",
                    ["username"] = "Username",
                    ["passcode"] = "Passcode",
                    ["login"] = "Login",
                    ["language"] = "Language:",
                    ["enter_user_passcode"] = "Enter username and passcode",
                    ["passcode_digits_only"] = "Passcode must contain digits only",
                    ["invalid_credentials"] = "Invalid credentials or inactive user",

                    // Toolbar
                    ["add"] = "Add",
                    ["edit"] = "Edit",
                    ["delete"] = "Delete",
                    ["refresh"] = "Refresh",
                    ["dark_mode"] = "Dark Mode",
                    ["logout"] = "Logout",

                    // Navigation
                    ["dashboard"] = "Dashboard",
                    ["vendors"] = "Vendors",
                    ["items"] = "Items",
                    ["cash_flow"] = "Cash Flow",
                    ["inventory"] = "Inventory",
                    ["orders"] = "Orders",
                    ["settings"] = "Settings",
                    ["users"] = "Users",

                    // Messages
                    ["backend_unavailable"] = "Backend is unavailable. Features may be limited until connectivity is restored.",

                    // Orders page
                    ["orders_title"] = "Orders",
                    ["start_new_order"] = "Start New Order",
                    ["save_draft"] = "Save Draft",
                    ["open_draft"] = "Open Draft",
                    ["job_history"] = "Job History",
                    ["notifications"] = "Notifications",

                    // Inventory page
                    ["inventory_title"] = "Inventory",
                    ["sync_product_names"] = "Sync Product Names",
                    ["search_placeholder"] = "Search clave or product name",

                    // Cash flow page
                    ["cash_flow_title"] = "Cash Flow",
                    ["employee_name"] = "Employee Name",
                    ["date"] = "Date",
                    ["cash_amount"] = "Cash Amount",
                    ["submit_cash_entry"] = "Submit Cash Entry",
                    ["notes"] = "Notes",

                    // Vendors/Items page
                    ["vendors_title"] = "Vendors",
                    ["items_title"] = "Items",
                },
                [AppLanguage.Esp] = new Dictionary<string, string>
                {
                    ["sign_in"] = "Iniciar sesión",
                    ["username"] = "Usuario",
                    ["passcode"] = "Código",
                    ["login"] = "Entrar",
                    ["language"] = "Idioma:",
                    ["enter_user_passcode"] = "Ingrese usuario y código",
                    ["passcode_digits_only"] = "El código debe contener solo números",
                    ["invalid_credentials"] = "Credenciales inválidas o usuario inactivo",

                    // Toolbar
                    ["add"] = "Agregar",
                    ["edit"] = "Editar",
                    ["delete"] = "Eliminar",
                    ["refresh"] = "Actualizar",
                    ["dark_mode"] = "Modo oscuro",
                    ["logout"] = "Salir",

                    // Navigation
                    ["dashboard"] = "Tablero",
                    ["vendors"] = "Proveedores",
                    ["items"] = "Artículos",
                    ["cash_flow"] = "Flujo de caja",
                    ["inventory"] = "Inventario",
                    ["orders"] = "Pedidos",
                    ["settings"] = "Ajustes",
                    ["users"] = "Usuarios",

                    // Messages
                    ["backend_unavailable"] = "El backend no está disponible. Las funciones pueden estar limitadas hasta que se restablezca la conectividad.",

                    // Orders page
                    ["orders_title"] = "Pedidos",
                    ["start_new_order"] = "Iniciar nuevo pedido",
                    ["save_draft"] = "Guardar borrador",
                    ["open_draft"] = "Abrir borrador",
                    ["job_history"] = "Historial de trabajos",
                    ["notifications"] = "Notificaciones",

                    // Inventory page
                    ["inventory_title"] = "Inventario",
                    ["sync_product_names"] = "Sincronizar nombres de productos",
                    ["search_placeholder"] = "Buscar clave o nombre del producto",

                    // Cash flow page
                    ["cash_flow_title"] = "Flujo de caja",
                    ["employee_name"] = "Empleado",
                    ["date"] = "Fecha",
                    ["cash_amount"] = "Monto",
                    ["submit_cash_entry"] = "Enviar movimiento",
                    ["notes"] = "Notas",

                    // Vendors/Items page
                    ["vendors_title"] = "Proveedores",
                    ["items_title"] = "Artículos",
                }
            };

            // Load saved language
            try
            {
                // Check if ApplicationData is available (WinUI 3 initialization check)
                if (ApplicationData.Current != null)
                {
                    var settings = ApplicationData.Current.LocalSettings;
                    if (settings?.Values != null)
                    {
                        var saved = settings.Values[LangKey]?.ToString();
                        if (Enum.TryParse(saved, out AppLanguage lang))
                            Current = lang;
                        else
                            Current = AppLanguage.Eng;
                    }
                    else
                    {
                        Current = AppLanguage.Eng;
                    }
                }
                else
                {
                    Current = AppLanguage.Eng;
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"I18nService initialization error: {ex.Message}");
                Current = AppLanguage.Eng;
            }
        }

        public event EventHandler? LanguageChanged;

        private AppLanguage _current;
        public AppLanguage Current
        {
            get => _current;
            set
            {
                if (_current == value) return;
                _current = value;
                try
                {
                    // Check if ApplicationData is available before saving
                    if (ApplicationData.Current?.LocalSettings?.Values != null)
                    {
                        ApplicationData.Current.LocalSettings.Values[LangKey] = _current.ToString();
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception for debugging
                    System.Diagnostics.Debug.WriteLine($"I18nService save error: {ex.Message}");
                }
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string T(string key)
        {
            if (_dict.TryGetValue(Current, out var map) && map.TryGetValue(key, out var val))
                return val;
            if (_dict[AppLanguage.Eng].TryGetValue(key, out var en)) return en;
            return key;
        }
    }
}
