using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

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
                // Use .NET file operations instead of Windows Runtime COM interop
                // This avoids "No installed components were detected" errors in WinUI 3 Desktop Apps
                Current = LoadLanguageFromFile();
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
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
                // Use .NET file operations instead of Windows Runtime COM interop
                // This avoids "No installed components were detected" errors in WinUI 3 Desktop Apps
                SaveLanguageToFile();
                }
                catch (Exception ex)
                {
                    // Log the exception for debugging
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

        private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "i18n.json");

        private void SaveLanguageToFile()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                var settings = new Dictionary<string, object> { [LangKey] = _current.ToString() };
                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
            }
        }

        private AppLanguage LoadLanguageFromFile()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return AppLanguage.Eng;
                
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                
                if (settings?.TryGetValue(LangKey, out var langValue) == true)
                {
                    if (Enum.TryParse(langValue.ToString(), out AppLanguage lang))
                        return lang;
                }
            }
            catch (Exception ex)
            {
            }
            
            return AppLanguage.Eng;
        }
    }
}
