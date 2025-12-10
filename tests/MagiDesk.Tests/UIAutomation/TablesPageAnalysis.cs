using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.Core;
using FlaUI.UIA3;
using FlaUI.Core.Definitions;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.Infrastructure;

namespace MagiDesk.Tests.UIAutomation;

[TestClass]
public class TablesPageAnalysis
{
    private FlaUI.Core.Application? _app;
    private UIA3Automation? _automation;
    private FlaUI.Core.AutomationElements.Window? _window;

    [TestInitialize]
    public async Task Initialize()
    {
        Console.WriteLine("🔍 STARTING TABLES PAGE ANALYSIS");
        Console.WriteLine("================================");
        
        // Kill any existing processes
        KillExistingProcesses();
        
        // Start the application
        var appPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "frontend", "bin", "Debug", "net8.0-windows10.0.19041.0", "MagiDesk.Frontend.exe");
        Console.WriteLine($"📱 Starting app from: {appPath}");
        
        _app = FlaUI.Core.Application.Launch(appPath);
        await Task.Delay(3000);
        
        _automation = new UIA3Automation();
        _window = _app.GetMainWindow(_automation);
        
        Console.WriteLine($"✅ App started. Window title: {_window.Title}");
        
        // Navigate to Tables page
        await NavigateToTablesPage();
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            _automation?.Dispose();
            _app?.Kill();
            _app?.Dispose();
            
            // Aggressive cleanup
            KillExistingProcesses();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Cleanup error: {ex.Message}");
        }
    }

    private void KillExistingProcesses()
    {
        try
        {
            var processes = Process.GetProcessesByName("MagiDesk.Frontend");
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(2000);
                    Console.WriteLine($"🔥 Killed existing process: {process.Id}");
                }
                catch { }
            }
        }
        catch { }
    }

    private async Task NavigateToTablesPage()
    {
        Console.WriteLine("\n🎯 NAVIGATING TO TABLES PAGE");
        Console.WriteLine("=============================");
        
        try
        {
            // Find navigation elements
            var navigationElements = _window!.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem)
                .Or(cf.ByControlType(ControlType.Button))
                .Or(cf.ByControlType(ControlType.Text)))
                .Where(e => !string.IsNullOrEmpty(e.Name) && 
                    (e.Name.ToLower().Contains("table") || 
                     e.Name.ToLower().Contains("navigation") ||
                     e.Name.ToLower().Contains("menu")))
                .OrderBy(e => e.Name)
                .Take(5)
                .ToList();

            Console.WriteLine($"🔍 Found {navigationElements.Count} potential navigation elements:");
            foreach (var element in navigationElements)
            {
                Console.WriteLine($"   - {element.ControlType}: {element.Name}");
            }

            // Try to find and click Tables navigation
            var tablesNav = navigationElements.FirstOrDefault(e => 
                e.Name.ToLower().Contains("table") && 
                !e.Name.ToLower().Contains("tablet"));
                
            if (tablesNav != null)
            {
                Console.WriteLine($"🎯 Found Tables navigation: {tablesNav.Name}");
                
                // Highlight and click
                tablesNav.DrawHighlight();
                await Task.Delay(1000);
                
                var clickTask = Task.Run(() => tablesNav.Click());
                if (clickTask.Wait(TimeSpan.FromSeconds(3)))
                {
                    Console.WriteLine("✅ Clicked Tables navigation");
                    await Task.Delay(3000); // Wait for page load
                }
                else
                {
                    Console.WriteLine("⏰ Timeout clicking Tables navigation");
                }
            }
            else
            {
                Console.WriteLine("❌ Could not find Tables navigation element");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Navigation error: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task AnalyzeTablesPageFunctions()
    {
        Console.WriteLine("\n📊 ANALYZING TABLES PAGE FUNCTIONS");
        Console.WriteLine("===================================");
        
        try
        {
            // Wait for page to load
            await Task.Delay(2000);
            
            // Analyze the current window title and content
            Console.WriteLine($"📋 Current Window Title: {_window!.Title}");
            
            // Find all interactive elements
            var allElements = _window.FindAllDescendants();
            Console.WriteLine($"🔍 Total UI elements found: {allElements.Length}");
            
            // Categorize elements by control type
            var elementTypes = allElements
                .GroupBy(e => e.ControlType)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToList();
                
            Console.WriteLine("\n📈 ELEMENT TYPES SUMMARY:");
            foreach (var group in elementTypes)
            {
                Console.WriteLine($"   {group.Key}: {group.Count()} elements");
            }
            
            // Find all buttons and interactive controls
            var buttons = allElements
                .Where(e => e.ControlType == ControlType.Button)
                .ToList();
                
            var listItems = allElements
                .Where(e => e.ControlType == ControlType.ListItem)
                .ToList();
                
            var menuItems = allElements
                .Where(e => e.ControlType == ControlType.MenuItem)
                .ToList();
                
            var customControls = allElements
                .Where(e => e.ControlType == ControlType.Custom)
                .ToList();

            Console.WriteLine($"\n🎛️ INTERACTIVE ELEMENTS:");
            Console.WriteLine($"   Buttons: {buttons.Count}");
            Console.WriteLine($"   List Items: {listItems.Count}");
            Console.WriteLine($"   Menu Items: {menuItems.Count}");
            Console.WriteLine($"   Custom Controls: {customControls.Count}");

            // Analyze buttons
            if (buttons.Count > 0)
            {
                Console.WriteLine("\n🔘 BUTTONS FOUND:");
                foreach (var button in buttons.Take(10))
                {
                    Console.WriteLine($"   - {button.Name} (AutomationId: {button.AutomationId})");
                }
            }

            // Analyze list items (likely table cards)
            if (listItems.Count > 0)
            {
                Console.WriteLine("\n📋 LIST ITEMS FOUND:");
                foreach (var item in listItems.Take(5))
                {
                    Console.WriteLine($"   - {item.Name} (AutomationId: {item.AutomationId})");
                    
                    // Try to find child elements (buttons, text, etc.)
                    var children = item.FindAllChildren();
                    var childButtons = children.Where(c => c.ControlType == ControlType.Button).ToList();
                    var childTexts = children.Where(c => c.ControlType == ControlType.Text).ToList();
                    
                    if (childButtons.Count > 0)
                    {
                        Console.WriteLine($"     📌 Child Buttons ({childButtons.Count}):");
                        foreach (var childBtn in childButtons)
                        {
                            Console.WriteLine($"       - {childBtn.Name}");
                        }
                    }
                    
                    if (childTexts.Count > 0)
                    {
                        Console.WriteLine($"     📝 Child Texts ({childTexts.Count}):");
                        foreach (var childText in childTexts.Take(3))
                        {
                            Console.WriteLine($"       - {childText.Name}");
                        }
                    }
                }
            }

            // Look for specific table-related functionality
            await AnalyzeTableSpecificFunctions();
            
            // Look for filter and control elements
            await AnalyzeFilterAndControls();
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Analysis error: {ex.Message}");
        }
    }

    private async Task AnalyzeTableSpecificFunctions()
    {
        Console.WriteLine("\n🎱 TABLE-SPECIFIC FUNCTIONS ANALYSIS");
        Console.WriteLine("=====================================");
        
        try
        {
            // Look for elements with table-related names
            var tableElements = _window!.FindAllDescendants()
                .Where(e => !string.IsNullOrEmpty(e.Name) && 
                    (e.Name.ToLower().Contains("table") ||
                     e.Name.ToLower().Contains("billiard") ||
                     e.Name.ToLower().Contains("bar") ||
                     e.Name.ToLower().Contains("start") ||
                     e.Name.ToLower().Contains("stop") ||
                     e.Name.ToLower().Contains("session") ||
                     e.Name.ToLower().Contains("bill") ||
                     e.Name.ToLower().Contains("add") ||
                     e.Name.ToLower().Contains("move") ||
                     e.Name.ToLower().Contains("threshold")))
                .ToList();

            Console.WriteLine($"🎯 Found {tableElements.Count} table-related elements:");
            foreach (var element in tableElements.Take(15))
            {
                Console.WriteLine($"   - {element.ControlType}: {element.Name}");
                
                // If it's a button or interactive element, highlight it briefly
                if (element.ControlType == ControlType.Button || 
                    element.ControlType == ControlType.MenuItem ||
                    element.ControlType == ControlType.ListItem)
                {
                    try
                    {
                        element.DrawHighlight();
                        await Task.Delay(300);
                    }
                    catch { }
                }
            }

            // Look for context menus or dropdown menus
            var menuElements = _window.FindAllDescendants()
                .Where(e => e.ControlType == ControlType.Menu || 
                           e.ControlType == ControlType.MenuItem ||
                           e.ControlType == ControlType.MenuBar)
                .ToList();

            if (menuElements.Count > 0)
            {
                Console.WriteLine($"\n📋 MENU ELEMENTS FOUND ({menuElements.Count}):");
                foreach (var menu in menuElements)
                {
                    Console.WriteLine($"   - {menu.ControlType}: {menu.Name}");
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Table functions analysis error: {ex.Message}");
        }
    }

    private async Task AnalyzeFilterAndControls()
    {
        Console.WriteLine("\n🔍 FILTER AND CONTROL ELEMENTS");
        Console.WriteLine("===============================");
        
        try
        {
            // Look for filter controls
            var filterElements = _window!.FindAllDescendants()
                .Where(e => !string.IsNullOrEmpty(e.Name) && 
                    (e.Name.ToLower().Contains("filter") ||
                     e.Name.ToLower().Contains("combo") ||
                     e.Name.ToLower().Contains("dropdown") ||
                     e.Name.ToLower().Contains("available") ||
                     e.Name.ToLower().Contains("occupied")))
                .ToList();

            Console.WriteLine($"🔍 Found {filterElements.Count} filter/control elements:");
            foreach (var element in filterElements)
            {
                Console.WriteLine($"   - {element.ControlType}: {element.Name}");
            }

            // Look for text elements that might show status
            var statusElements = _window.FindAllDescendants()
                .Where(e => e.ControlType == ControlType.Text && 
                           !string.IsNullOrEmpty(e.Name) &&
                           (e.Name.ToLower().Contains("count") ||
                            e.Name.ToLower().Contains("status") ||
                            e.Name.ToLower().Contains("total") ||
                            e.Name.ToLower().Contains("available") ||
                            e.Name.ToLower().Contains("occupied")))
                .ToList();

            if (statusElements.Count > 0)
            {
                Console.WriteLine($"\n📊 STATUS ELEMENTS FOUND ({statusElements.Count}):");
                foreach (var status in statusElements)
                {
                    Console.WriteLine($"   - {status.Name}");
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Filter analysis error: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task TestTableInteractionFlow()
    {
        Console.WriteLine("\n🎮 TESTING TABLE INTERACTION FLOW");
        Console.WriteLine("===================================");
        
        try
        {
            // Wait for page to load
            await Task.Delay(2000);
            
            // Find table items (likely ListItem controls)
            var tableItems = _window!.FindAllDescendants()
                .Where(e => e.ControlType == ControlType.ListItem)
                .Where(e => !string.IsNullOrEmpty(e.Name) && 
                    (e.Name.ToLower().Contains("billiard") || 
                     e.Name.ToLower().Contains("bar") ||
                     e.Name.ToLower().Contains("table")))
                .Take(3)
                .ToList();

            Console.WriteLine($"🎱 Found {tableItems.Count} table items to test:");
            
            foreach (var table in tableItems)
            {
                Console.WriteLine($"\n🎯 Testing table: {table.Name}");
                
                // Highlight the table
                table.DrawHighlight();
                await Task.Delay(1000);
                
                // Look for action buttons within this table
                var actionButtons = table.FindAllChildren()
                    .Where(c => c.ControlType == ControlType.Button)
                    .Where(c => !string.IsNullOrEmpty(c.Name))
                    .ToList();
                
                Console.WriteLine($"   🔘 Found {actionButtons.Count} action buttons:");
                foreach (var button in actionButtons)
                {
                    Console.WriteLine($"      - {button.Name}");
                    
                    // Try to click the button
                    try
                    {
                        button.DrawHighlight();
                        await Task.Delay(500);
                        
                        var clickTask = Task.Run(() => button.Click());
                        if (clickTask.Wait(TimeSpan.FromSeconds(2)))
                        {
                            Console.WriteLine($"      ✅ Clicked: {button.Name}");
                            await Task.Delay(1000);
                            
                            // Look for any dialogs or menus that appeared
                            var dialogs = _window.FindAllDescendants()
                                .Where(e => e.ControlType == ControlType.Window || 
                                           e.ControlType == ControlType.Pane ||
                                           e.ControlType == ControlType.Menu)
                                .Where(e => e.Name != _window.Title)
                                .ToList();
                                
                            if (dialogs.Count > 0)
                            {
                                Console.WriteLine($"      📋 Found {dialogs.Count} dialogs/menus after click:");
                                foreach (var dialog in dialogs)
                                {
                                    Console.WriteLine($"         - {dialog.ControlType}: {dialog.Name}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"      ⏰ Timeout clicking: {button.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"      ❌ Error clicking {button.Name}: {ex.Message}");
                    }
                }
                
                await Task.Delay(1000);
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Interaction flow error: {ex.Message}");
        }
    }
}
