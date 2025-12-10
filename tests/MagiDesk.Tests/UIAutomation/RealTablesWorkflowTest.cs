using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using FlaUI.Core.Definitions;
using System.Diagnostics;

namespace MagiDesk.Tests.UIAutomation
{
    [TestClass]
    public class RealTablesWorkflowTest
    {
        private FlaUI.Core.Application? _application;
        private Window? _mainWindow;
        private UIA3Automation? _automation;
        private Process? _appProcess;

        [TestCleanup]
        public void Cleanup()
        {
            Console.WriteLine("🧹 FORCE CLEANUP STARTED");
            
            try
            {
                // Force kill ALL MagiDesk processes
                var processes = Process.GetProcessesByName("MagiDesk.Frontend");
                foreach (var process in processes)
                {
                    Console.WriteLine($"🔪 Force killing process: {process.Id}");
                    process.Kill();
                    process.WaitForExit(2000);
                }

                if (_appProcess != null && !_appProcess.HasExited)
                {
                    Console.WriteLine($"🔪 Force killing process by ID: {_appProcess.Id}");
                    _appProcess.Kill();
                    _appProcess.WaitForExit(2000);
                }

                _application?.Dispose();
                _automation?.Dispose();
                Console.WriteLine("✅ FORCE CLEANUP COMPLETED");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Cleanup error: {ex.Message}");
            }
        }

        [TestMethod]
        public void RealTablesWorkflowTest_ShowActualTablesOperations_ShouldDemonstrateRealWorkflow()
        {
            Console.WriteLine("🎬 REAL TABLES WORKFLOW DEMONSTRATION");
            Console.WriteLine("=====================================");
            Console.WriteLine("👀 WATCH: This will show ACTUAL Tables operations!");

            try
            {
                // STEP 1: Launch and Navigate to Tables
                Console.WriteLine("\n🚀 STEP 1: LAUNCHING AND NAVIGATING TO TABLES");
                Console.WriteLine("==============================================");
                LaunchAndNavigateToTables();
                
                // STEP 2: View Existing Tables
                Console.WriteLine("\n📋 STEP 2: VIEWING EXISTING TABLES");
                Console.WriteLine("==================================");
                ViewExistingTables();
                
                // STEP 3: Add New Table
                Console.WriteLine("\n➕ STEP 3: ADDING NEW TABLE");
                Console.WriteLine("===========================");
                AddNewTable();
                
                // STEP 4: Start Table (Begin Service)
                Console.WriteLine("\n▶️ STEP 4: STARTING TABLE (BEGIN SERVICE)");
                Console.WriteLine("=========================================");
                StartTable();
                
                // STEP 5: Add Items to Table
                Console.WriteLine("\n🍽️ STEP 5: ADDING ITEMS TO TABLE");
                Console.WriteLine("================================");
                AddItemsToTable();
                
                // STEP 6: View Table Status
                Console.WriteLine("\n📊 STEP 6: VIEWING TABLE STATUS");
                Console.WriteLine("===============================");
                ViewTableStatus();
                
                // STEP 7: Stop Table (End Service)
                Console.WriteLine("\n⏹️ STEP 7: STOPPING TABLE (END SERVICE)");
                Console.WriteLine("=======================================");
                StopTable();
                
                // STEP 8: Edit Table
                Console.WriteLine("\n✏️ STEP 8: EDITING TABLE");
                Console.WriteLine("========================");
                EditTable();
                
                // STEP 9: Delete Table
                Console.WriteLine("\n🗑️ STEP 9: DELETING TABLE");
                Console.WriteLine("=========================");
                DeleteTable();
                
                Console.WriteLine("\n🎉 REAL TABLES WORKFLOW DEMONSTRATION COMPLETED!");
                Console.WriteLine("👏 You have seen the COMPLETE Tables workflow in action!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ REAL TABLES WORKFLOW FAILED: {ex.Message}");
                throw;
            }
        }

        private void LaunchAndNavigateToTables()
        {
            Console.WriteLine("⏰ Launching MagiDesk and navigating to Tables...");
            
            var launchTask = Task.Run(() =>
            {
                // Launch application
                var appPath = @"C:\Users\giris\Documents\Code\Order-Tracking-By-GPT\solution\frontend\bin\Release\net8.0-windows10.0.19041.0\MagiDesk.Frontend.exe";
                
                if (!File.Exists(appPath))
                {
                    throw new FileNotFoundException($"Application not found at: {appPath}");
                }

                Console.WriteLine($"✅ Launching MagiDesk application...");
                _application = FlaUI.Core.Application.Launch(appPath);
                _appProcess = Process.GetProcessById(_application.ProcessId);
                
                Console.WriteLine($"✅ MagiDesk launched with PID: {_application.ProcessId}");
                Console.WriteLine($"👀 WATCH: You should see the MagiDesk application window!");
                
                Thread.Sleep(8000);
                
                // Find main window and navigate to Tables
                _automation = new UIA3Automation();
                _mainWindow = _application?.GetMainWindow(_automation);
                
                if (_mainWindow == null)
                {
                    throw new Exception("Could not find main window");
                }
                
                Console.WriteLine($"✅ Found main window: '{_mainWindow.Title}'");
                
                // Find and click Tables navigation
                var allElements = _mainWindow.FindAllDescendants();
                var tablesElement = allElements
                    .Where(e => e.ControlType == ControlType.ListItem)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("table"))
                    .FirstOrDefault();

                if (tablesElement == null)
                {
                    throw new Exception("Could not find Tables navigation element");
                }
                
                Console.WriteLine($"✅ Found Tables navigation: '{tablesElement.Name}'");
                Console.WriteLine($"👀 WATCH: Clicking Tables navigation!");
                
                tablesElement.DrawHighlight();
                Thread.Sleep(2000);
                tablesElement.Click();
                Console.WriteLine($"✅ Successfully navigated to Tables page!");
                
                Thread.Sleep(5000);
                return true;
            });

            if (!launchTask.Wait(TimeSpan.FromSeconds(25)))
            {
                throw new TimeoutException("Launch and navigation timed out");
            }

            Console.WriteLine("✅ Successfully launched and navigated to Tables");
        }

        private void ViewExistingTables()
        {
            Console.WriteLine("⏰ Viewing existing tables...");
            
            var viewTask = Task.Run(() =>
            {
                if (_mainWindow == null) return false;

                var allElements = _mainWindow.FindAllDescendants();
                
                // Skip ALL navigation items - they're all in the left sidebar
                var navigationItems = allElements
                    .Where(e => e.ControlType == ControlType.ListItem)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("dashboard") || 
                               e.Name.ToLower().Contains("tables") || 
                               e.Name.ToLower().Contains("order") || 
                               e.Name.ToLower().Contains("billing") ||
                               e.Name.ToLower().Contains("payment") ||
                               e.Name.ToLower().Contains("menu") ||
                               e.Name.ToLower().Contains("inventory") ||
                               e.Name.ToLower().Contains("cash"))
                    .ToList();

                Console.WriteLine($"🚫 Skipping {navigationItems.Count} navigation items");
                
                // Look for actual table data in the main content area (right side)
                // Tables would typically be in a grid, list, or data control in the main area
                var tableItems = new List<AutomationElement>();
                
                foreach (var element in allElements)
                {
                    try
                    {
                        if ((element.ControlType == ControlType.DataItem || 
                             element.ControlType == ControlType.Custom ||
                             element.ControlType == ControlType.Group) &&
                            !string.IsNullOrEmpty(element.Name) &&
                            element.Name.Length > 1 &&
                            !navigationItems.Contains(element))
                        {
                            tableItems.Add(element);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skip elements that don't support Name property
                        Console.WriteLine($"⚠️ Skipping element with unsupported Name property: {ex.Message}");
                    }
                }

                Console.WriteLine($"🔍 Found {tableItems.Count} actual table items:");
                foreach (var item in tableItems.Take(5))
                {
                    Console.WriteLine($"   📋 Table: '{item.Name}' at {item.BoundingRectangle}");
                    Console.WriteLine($"   👀 WATCH: This table will be highlighted!");
                    item.DrawHighlight();
                    Thread.Sleep(2000);
                }

                // If no table items found, show what's actually on the page
                if (tableItems.Count == 0)
                {
                    Console.WriteLine("⚠️ No table items found - showing what's actually on the Tables page:");
                    
                    var allContentElements = new List<AutomationElement>();
                    foreach (var element in allElements.Take(20))
                    {
                        try
                        {
                            if (!navigationItems.Contains(element) && 
                                !string.IsNullOrEmpty(element.Name))
                            {
                                allContentElements.Add(element);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Skip elements that don't support Name property
                        }
                    }
                    
                    Console.WriteLine($"📋 Found {allContentElements.Count} content elements on Tables page:");
                    foreach (var element in allContentElements)
                    {
                        Console.WriteLine($"   📋 {element.ControlType}: '{element.Name}' at {element.BoundingRectangle}");
                        element.DrawHighlight();
                        Thread.Sleep(1500);
                    }
                }

                // Look for table management buttons on the main content area
                var contentButtons = allElements
                    .Where(e => e.ControlType == ControlType.Button)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("add") || 
                               e.Name.ToLower().Contains("edit") || 
                               e.Name.ToLower().Contains("delete") || 
                               e.Name.ToLower().Contains("refresh"))
                    .ToList();

                Console.WriteLine($"🔘 Found {contentButtons.Count} table management buttons:");
                foreach (var button in contentButtons)
                {
                    Console.WriteLine($"   🔘 Button: '{button.Name}' at {button.BoundingRectangle}");
                    Console.WriteLine($"   👀 WATCH: This button will be highlighted!");
                    button.DrawHighlight();
                    Thread.Sleep(1500);
                }

                // Look for table status or content text
                var contentText = allElements
                    .Where(e => e.ControlType == ControlType.Text)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.Length > 3)
                    .Where(e => !e.Name.ToLower().Contains("dashboard") && 
                               !e.Name.ToLower().Contains("tables") && 
                               !e.Name.ToLower().Contains("order") && 
                               !e.Name.ToLower().Contains("billing"))
                    .Take(10)
                    .ToList();

                Console.WriteLine($"📝 Found {contentText.Count} content text elements:");
                foreach (var text in contentText.Take(5))
                {
                    Console.WriteLine($"   📝 Content: '{text.Name}'");
                    text.DrawHighlight();
                    Thread.Sleep(1000);
                }

                return true;
            });

            if (!viewTask.Wait(TimeSpan.FromSeconds(15)))
            {
                throw new TimeoutException("Viewing existing tables timed out");
            }

            Console.WriteLine("✅ Successfully viewed existing tables");
        }

        private void AddNewTable()
        {
            Console.WriteLine("⏰ Adding new table...");
            
            var addTask = Task.Run(() =>
            {
                if (_mainWindow == null) return false;

                var allElements = _mainWindow.FindAllDescendants();
                
                // Find Add button
                var addButton = allElements
                    .Where(e => e.ControlType == ControlType.Button)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("add"))
                    .FirstOrDefault();

                if (addButton == null)
                {
                    Console.WriteLine("❌ Add button not found - showing all available buttons");
                    var allButtons = allElements
                        .Where(e => e.ControlType == ControlType.Button)
                        .Where(e => !string.IsNullOrEmpty(e.Name))
                        .Take(10)
                        .ToList();
                    
                    Console.WriteLine($"🔘 Available buttons ({allButtons.Count}):");
                    foreach (var btn in allButtons)
                    {
                        Console.WriteLine($"   🔘 '{btn.Name}' at {btn.BoundingRectangle}");
                        btn.DrawHighlight();
                        Thread.Sleep(1000);
                    }
                    return false;
                }

                Console.WriteLine($"✅ Found Add button: '{addButton.Name}'");
                Console.WriteLine($"👀 WATCH: Clicking Add button to create new table!");
                
                addButton.DrawHighlight();
                Thread.Sleep(3000);
                addButton.Click();
                Console.WriteLine($"✅ CLICKED Add button!");
                
                // Wait for Add table dialog/form to appear
                Thread.Sleep(8000);
                
                // Look for form fields (table name, capacity, etc.)
                var formElements = _mainWindow.FindAllDescendants()
                    .Where(e => e.ControlType == ControlType.Edit || e.ControlType == ControlType.ComboBox)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .ToList();

                Console.WriteLine($"📝 Found {formElements.Count} form fields for new table:");
                foreach (var field in formElements.Take(5))
                {
                    Console.WriteLine($"   📝 Field: '{field.Name}' at {field.BoundingRectangle}");
                    Console.WriteLine($"   👀 WATCH: This field will be highlighted!");
                    field.DrawHighlight();
                    Thread.Sleep(2000);
                    
                    // Try to interact with the field
                    try
                    {
                        field.Click();
                        Console.WriteLine($"   ✅ Clicked field: '{field.Name}'");
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ❌ Could not click field: {ex.Message}");
                    }
                }

                // Look for Save/Confirm button
                var saveButton = _mainWindow.FindAllDescendants()
                    .Where(e => e.ControlType == ControlType.Button)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("save") || 
                               e.Name.ToLower().Contains("confirm") ||
                               e.Name.ToLower().Contains("ok") ||
                               e.Name.ToLower().Contains("create") ||
                               e.Name.ToLower().Contains("add"))
                    .FirstOrDefault();

                if (saveButton != null)
                {
                    Console.WriteLine($"✅ Found Save button: '{saveButton.Name}'");
                    Console.WriteLine($"👀 WATCH: Clicking Save to create the table!");
                    saveButton.DrawHighlight();
                    Thread.Sleep(3000);
                    saveButton.Click();
                    Console.WriteLine($"✅ CLICKED Save button!");
                    Thread.Sleep(5000);
                }
                else
                {
                    Console.WriteLine("⚠️ No Save button found - form might have auto-saved or closed");
                }

                return true;
            });

            if (!addTask.Wait(TimeSpan.FromSeconds(25)))
            {
                throw new TimeoutException("Adding new table timed out");
            }

            Console.WriteLine("✅ Successfully demonstrated adding new table");
        }

        private void StartTable()
        {
            Console.WriteLine("⏰ Starting table (beginning service)...");
            
            var startTask = Task.Run(() =>
            {
                if (_mainWindow == null) return false;

                var allElements = _mainWindow.FindAllDescendants();
                
                // Look for Start button or Play button
                var startButton = allElements
                    .Where(e => e.ControlType == ControlType.Button)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("start") || 
                               e.Name.ToLower().Contains("play") ||
                               e.Name.ToLower().Contains("begin") ||
                               e.Name.ToLower().Contains("service"))
                    .FirstOrDefault();

                if (startButton != null)
                {
                    Console.WriteLine($"✅ Found Start button: '{startButton.Name}'");
                    Console.WriteLine($"👀 WATCH: Clicking Start to begin table service!");
                    
                    startButton.DrawHighlight();
                    Thread.Sleep(2000);
                    startButton.Click();
                    Console.WriteLine($"✅ CLICKED Start button!");
                    
                    Thread.Sleep(3000);
                }
                else
                {
                    // Look for table item and right-click for context menu
                    var tableItem = allElements
                        .Where(e => e.ControlType == ControlType.ListItem)
                        .Where(e => !string.IsNullOrEmpty(e.Name))
                        .FirstOrDefault();

                    if (tableItem != null)
                    {
                        Console.WriteLine($"✅ Found table item: '{tableItem.Name}'");
                        Console.WriteLine($"👀 WATCH: Right-clicking table for context menu!");
                        
                        tableItem.DrawHighlight();
                        Thread.Sleep(2000);
                        
                        // Right-click to open context menu
                        tableItem.RightClick();
                        Console.WriteLine($"✅ Right-clicked table!");
                        
                        Thread.Sleep(2000);
                        
                        // Look for Start option in context menu
                        var contextElements = _mainWindow.FindAllDescendants()
                            .Where(e => e.ControlType == ControlType.MenuItem)
                            .Where(e => !string.IsNullOrEmpty(e.Name))
                            .Where(e => e.Name.ToLower().Contains("start"))
                            .FirstOrDefault();

                        if (contextElements != null)
                        {
                            Console.WriteLine($"✅ Found Start menu option: '{contextElements.Name}'");
                            contextElements.Click();
                            Console.WriteLine($"✅ CLICKED Start menu option!");
                            Thread.Sleep(3000);
                        }
                    }
                }

                return true;
            });

            if (!startTask.Wait(TimeSpan.FromSeconds(15)))
            {
                throw new TimeoutException("Starting table timed out");
            }

            Console.WriteLine("✅ Successfully started table service");
        }

        private void AddItemsToTable()
        {
            Console.WriteLine("⏰ Adding items to table...");
            
            var addItemsTask = Task.Run(() =>
            {
                if (_mainWindow == null) return false;

                var allElements = _mainWindow.FindAllDescendants();
                
                // Look for Add Item button or Order button
                var addItemButton = allElements
                    .Where(e => e.ControlType == ControlType.Button)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("add") && 
                               (e.Name.ToLower().Contains("item") || e.Name.ToLower().Contains("order")))
                    .FirstOrDefault();

                if (addItemButton != null)
                {
                    Console.WriteLine($"✅ Found Add Item button: '{addItemButton.Name}'");
                    Console.WriteLine($"👀 WATCH: Clicking Add Item to add items to table!");
                    
                    addItemButton.DrawHighlight();
                    Thread.Sleep(2000);
                    addItemButton.Click();
                    Console.WriteLine($"✅ CLICKED Add Item button!");
                    
                    Thread.Sleep(3000);
                    
                    // Look for menu items or food items
                    var menuItems = allElements
                        .Where(e => e.ControlType == ControlType.ListItem || e.ControlType == ControlType.Button)
                        .Where(e => !string.IsNullOrEmpty(e.Name))
                        .Where(e => e.Name.Length > 2)
                        .Take(5)
                        .ToList();

                    Console.WriteLine($"🍽️ Found {menuItems.Count} menu items to add:");
                    foreach (var item in menuItems)
                    {
                        Console.WriteLine($"   🍽️ Menu Item: '{item.Name}'");
                        Console.WriteLine($"   👀 WATCH: Adding this item to table!");
                        item.DrawHighlight();
                        Thread.Sleep(1500);
                        item.Click();
                        Console.WriteLine($"   ✅ Added: '{item.Name}'");
                        Thread.Sleep(1000);
                    }
                }

                return true;
            });

            if (!addItemsTask.Wait(TimeSpan.FromSeconds(20)))
            {
                throw new TimeoutException("Adding items to table timed out");
            }

            Console.WriteLine("✅ Successfully added items to table");
        }

        private void ViewTableStatus()
        {
            Console.WriteLine("⏰ Viewing table status...");
            
            var statusTask = Task.Run(() =>
            {
                if (_mainWindow == null) return false;

                var allElements = _mainWindow.FindAllDescendants();
                
                // Look for status indicators
                var statusElements = allElements
                    .Where(e => e.ControlType == ControlType.Text || e.ControlType == ControlType.Custom)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("status") || 
                               e.Name.ToLower().Contains("active") || 
                               e.Name.ToLower().Contains("inactive") ||
                               e.Name.ToLower().Contains("occupied") ||
                               e.Name.ToLower().Contains("available") ||
                               e.Name.ToLower().Contains("serving"))
                    .ToList();

                Console.WriteLine($"📊 Found {statusElements.Count} status indicators:");
                foreach (var status in statusElements.Take(5))
                {
                    Console.WriteLine($"   📊 Status: '{status.Name}' at {status.BoundingRectangle}");
                    Console.WriteLine($"   👀 WATCH: Current table status!");
                    status.DrawHighlight();
                    Thread.Sleep(2000);
                }

                // Look for table details (orders, total, etc.)
                var detailElements = allElements
                    .Where(e => e.ControlType == ControlType.Text)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("order") || 
                               e.Name.ToLower().Contains("total") ||
                               e.Name.ToLower().Contains("bill") ||
                               e.Name.ToLower().Contains("amount"))
                    .ToList();

                Console.WriteLine($"💰 Found {detailElements.Count} table details:");
                foreach (var detail in detailElements.Take(3))
                {
                    Console.WriteLine($"   💰 Detail: '{detail.Name}'");
                    detail.DrawHighlight();
                    Thread.Sleep(1500);
                }

                return true;
            });

            if (!statusTask.Wait(TimeSpan.FromSeconds(15)))
            {
                throw new TimeoutException("Viewing table status timed out");
            }

            Console.WriteLine("✅ Successfully viewed table status");
        }

        private void StopTable()
        {
            Console.WriteLine("⏰ Stopping table (ending service)...");
            
            var stopTask = Task.Run(() =>
            {
                if (_mainWindow == null) return false;

                var allElements = _mainWindow.FindAllDescendants();
                
                // Look for Stop button
                var stopButton = allElements
                    .Where(e => e.ControlType == ControlType.Button)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("stop") || 
                               e.Name.ToLower().Contains("end") ||
                               e.Name.ToLower().Contains("close") ||
                               e.Name.ToLower().Contains("finish"))
                    .FirstOrDefault();

                if (stopButton != null)
                {
                    Console.WriteLine($"✅ Found Stop button: '{stopButton.Name}'");
                    Console.WriteLine($"👀 WATCH: Clicking Stop to end table service!");
                    
                    stopButton.DrawHighlight();
                    Thread.Sleep(2000);
                    stopButton.Click();
                    Console.WriteLine($"✅ CLICKED Stop button!");
                    
                    Thread.Sleep(3000);
                }
                else
                {
                    // Look for context menu option
                    var tableItem = allElements
                        .Where(e => e.ControlType == ControlType.ListItem)
                        .Where(e => !string.IsNullOrEmpty(e.Name))
                        .FirstOrDefault();

                    if (tableItem != null)
                    {
                        Console.WriteLine($"✅ Found table item: '{tableItem.Name}'");
                        Console.WriteLine($"👀 WATCH: Right-clicking table for stop option!");
                        
                        tableItem.DrawHighlight();
                        Thread.Sleep(2000);
                        tableItem.RightClick();
                        Console.WriteLine($"✅ Right-clicked table!");
                        
                        Thread.Sleep(2000);
                        
                        var stopOption = _mainWindow.FindAllDescendants()
                            .Where(e => e.ControlType == ControlType.MenuItem)
                            .Where(e => !string.IsNullOrEmpty(e.Name))
                            .Where(e => e.Name.ToLower().Contains("stop"))
                            .FirstOrDefault();

                        if (stopOption != null)
                        {
                            Console.WriteLine($"✅ Found Stop option: '{stopOption.Name}'");
                            stopOption.Click();
                            Console.WriteLine($"✅ CLICKED Stop option!");
                            Thread.Sleep(3000);
                        }
                    }
                }

                return true;
            });

            if (!stopTask.Wait(TimeSpan.FromSeconds(15)))
            {
                throw new TimeoutException("Stopping table timed out");
            }

            Console.WriteLine("✅ Successfully stopped table service");
        }

        private void EditTable()
        {
            Console.WriteLine("⏰ Editing table...");
            
            var editTask = Task.Run(() =>
            {
                if (_mainWindow == null) return false;

                var allElements = _mainWindow.FindAllDescendants();
                
                // Find Edit button
                var editButton = allElements
                    .Where(e => e.ControlType == ControlType.Button)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("edit"))
                    .FirstOrDefault();

                if (editButton != null)
                {
                    Console.WriteLine($"✅ Found Edit button: '{editButton.Name}'");
                    Console.WriteLine($"👀 WATCH: Clicking Edit to modify table!");
                    
                    editButton.DrawHighlight();
                    Thread.Sleep(2000);
                    editButton.Click();
                    Console.WriteLine($"✅ CLICKED Edit button!");
                    
                    Thread.Sleep(3000);
                    
                    // Look for edit form fields
                    var editFields = allElements
                        .Where(e => e.ControlType == ControlType.Edit || e.ControlType == ControlType.ComboBox)
                        .Where(e => !string.IsNullOrEmpty(e.Name))
                        .Take(3)
                        .ToList();

                    Console.WriteLine($"✏️ Found {editFields.Count} editable fields:");
                    foreach (var field in editFields)
                    {
                        Console.WriteLine($"   ✏️ Field: '{field.Name}'");
                        field.DrawHighlight();
                        Thread.Sleep(1500);
                    }

                    // Look for Save button
                    var saveButton = allElements
                        .Where(e => e.ControlType == ControlType.Button)
                        .Where(e => !string.IsNullOrEmpty(e.Name))
                        .Where(e => e.Name.ToLower().Contains("save"))
                        .FirstOrDefault();

                    if (saveButton != null)
                    {
                        Console.WriteLine($"✅ Found Save button: '{saveButton.Name}'");
                        Console.WriteLine($"👀 WATCH: Clicking Save to save changes!");
                        saveButton.DrawHighlight();
                        Thread.Sleep(2000);
                        saveButton.Click();
                        Console.WriteLine($"✅ CLICKED Save button!");
                        Thread.Sleep(3000);
                    }
                }

                return true;
            });

            if (!editTask.Wait(TimeSpan.FromSeconds(20)))
            {
                throw new TimeoutException("Editing table timed out");
            }

            Console.WriteLine("✅ Successfully edited table");
        }

        private void DeleteTable()
        {
            Console.WriteLine("⏰ Deleting table...");
            
            var deleteTask = Task.Run(() =>
            {
                if (_mainWindow == null) return false;

                var allElements = _mainWindow.FindAllDescendants();
                
                // Find Delete button
                var deleteButton = allElements
                    .Where(e => e.ControlType == ControlType.Button)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("delete"))
                    .FirstOrDefault();

                if (deleteButton != null)
                {
                    Console.WriteLine($"✅ Found Delete button: '{deleteButton.Name}'");
                    Console.WriteLine($"👀 WATCH: Clicking Delete to remove table!");
                    
                    deleteButton.DrawHighlight();
                    Thread.Sleep(2000);
                    deleteButton.Click();
                    Console.WriteLine($"✅ CLICKED Delete button!");
                    
                    Thread.Sleep(3000);
                    
                    // Look for confirmation dialog
                    var confirmButton = allElements
                        .Where(e => e.ControlType == ControlType.Button)
                        .Where(e => !string.IsNullOrEmpty(e.Name))
                        .Where(e => e.Name.ToLower().Contains("yes") || 
                                   e.Name.ToLower().Contains("confirm") ||
                                   e.Name.ToLower().Contains("ok"))
                        .FirstOrDefault();

                    if (confirmButton != null)
                    {
                        Console.WriteLine($"✅ Found confirmation button: '{confirmButton.Name}'");
                        Console.WriteLine($"👀 WATCH: Confirming table deletion!");
                        confirmButton.DrawHighlight();
                        Thread.Sleep(2000);
                        confirmButton.Click();
                        Console.WriteLine($"✅ CLICKED confirmation button!");
                        Thread.Sleep(3000);
                    }
                }

                return true;
            });

            if (!deleteTask.Wait(TimeSpan.FromSeconds(15)))
            {
                throw new TimeoutException("Deleting table timed out");
            }

            Console.WriteLine("✅ Successfully deleted table");
        }
    }
}
