using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using FlaUI.Core.Definitions;
using System.Diagnostics;

namespace MagiDesk.Tests.UIAutomation
{
    [TestClass]
    public class CompleteTablesFlowTest
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
        public void CompleteTablesFlowTest_ShowFullTablesWorkflow_ShouldDemonstrateEverything()
        {
            Console.WriteLine("🎬 COMPLETE TABLES FLOW DEMONSTRATION");
            Console.WriteLine("=====================================");
            Console.WriteLine("👀 WATCH: This will show the ENTIRE Tables workflow!");

            try
            {
                // STEP 1: Launch Application
                Console.WriteLine("\n🚀 STEP 1: LAUNCHING MAGIDESK APPLICATION");
                Console.WriteLine("==========================================");
                LaunchApplication();
                
                // STEP 2: Navigate to Tables Page
                Console.WriteLine("\n🧭 STEP 2: NAVIGATING TO TABLES PAGE");
                Console.WriteLine("====================================");
                NavigateToTablesPage();
                
                // STEP 3: Explore Tables Page Elements
                Console.WriteLine("\n🔍 STEP 3: EXPLORING TABLES PAGE ELEMENTS");
                Console.WriteLine("=========================================");
                ExploreTablesPageElements();
                
                // STEP 4: Demonstrate Tables Actions
                Console.WriteLine("\n⚡ STEP 4: DEMONSTRATING TABLES ACTIONS");
                Console.WriteLine("=====================================");
                DemonstrateTablesActions();
                
                // STEP 5: Show Tables Data/Content
                Console.WriteLine("\n📋 STEP 5: SHOWING TABLES DATA AND CONTENT");
                Console.WriteLine("==========================================");
                ShowTablesDataAndContent();
                
                // STEP 6: Navigate to Other Pages
                Console.WriteLine("\n🌐 STEP 6: NAVIGATING TO OTHER PAGES");
                Console.WriteLine("====================================");
                NavigateToOtherPages();
                
                Console.WriteLine("\n🎉 COMPLETE TABLES FLOW DEMONSTRATION FINISHED!");
                Console.WriteLine("👏 You have seen the entire Tables workflow in action!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ FLOW DEMONSTRATION FAILED: {ex.Message}");
                throw;
            }
        }

        private void LaunchApplication()
        {
            Console.WriteLine("⏰ Launching MagiDesk application...");
            
            var launchTask = Task.Run(() =>
            {
                var appPath = @"C:\Users\giris\Documents\Code\Order-Tracking-By-GPT\solution\frontend\bin\Release\net8.0-windows10.0.19041.0\MagiDesk.Frontend.exe";
                
                if (!File.Exists(appPath))
                {
                    throw new FileNotFoundException($"Application not found at: {appPath}");
                }

                Console.WriteLine($"✅ Found MagiDesk application at: {appPath}");
                
                _application = FlaUI.Core.Application.Launch(appPath);
                _appProcess = Process.GetProcessById(_application.ProcessId);
                
                Console.WriteLine($"✅ MagiDesk launched successfully!");
                Console.WriteLine($"📱 Application PID: {_application.ProcessId}");
                Console.WriteLine($"👀 WATCH: You should see the MagiDesk application window!");
                
                // Wait for application to fully load
                Thread.Sleep(8000);
                
                return true;
            });

            if (!launchTask.Wait(TimeSpan.FromSeconds(20)))
            {
                throw new TimeoutException("Application launch timed out");
            }

            Console.WriteLine("✅ Application launch completed successfully");
        }

        private void NavigateToTablesPage()
        {
            Console.WriteLine("⏰ Finding main window and navigating to Tables...");
            
            var navigateTask = Task.Run(() =>
            {
                // Find main window
                _automation = new UIA3Automation();
                _mainWindow = _application?.GetMainWindow(_automation);
                
                if (_mainWindow == null)
                {
                    throw new Exception("Could not find main window");
                }
                
                Console.WriteLine($"✅ Found main window: '{_mainWindow.Title}'");
                Console.WriteLine($"👀 WATCH: You should see the main MagiDesk window!");
                
                // Find Tables navigation element
                var allElements = _mainWindow.FindAllDescendants();
                Console.WriteLine($"🔍 Found {allElements.Length} UI elements in the application");
                
                var tablesElement = allElements
                    .Where(e => e.ControlType == ControlType.ListItem)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("table"))
                    .FirstOrDefault();

                if (tablesElement == null)
                {
                    throw new Exception("Could not find Tables navigation element");
                }
                
                Console.WriteLine($"✅ Found Tables navigation element: '{tablesElement.Name}'");
                Console.WriteLine($"📍 Position: {tablesElement.BoundingRectangle}");
                Console.WriteLine($"👀 WATCH: Tables element will be highlighted and clicked!");
                
                // Highlight and click Tables element
                tablesElement.DrawHighlight();
                Thread.Sleep(3000);
                
                tablesElement.Click();
                Console.WriteLine($"✅ CLICKED Tables navigation element!");
                Console.WriteLine($"👀 WATCH: You should see navigation to Tables page!");
                
                // Wait for Tables page to load
                Thread.Sleep(10000);
                
                return true;
            });

            if (!navigateTask.Wait(TimeSpan.FromSeconds(25)))
            {
                throw new TimeoutException("Navigation to Tables page timed out");
            }

            Console.WriteLine("✅ Successfully navigated to Tables page");
        }

        private void ExploreTablesPageElements()
        {
            Console.WriteLine("⏰ Exploring Tables page elements...");
            
            var exploreTask = Task.Run(() =>
            {
                if (_mainWindow == null) return false;

                var allElements = _mainWindow.FindAllDescendants();
                Console.WriteLine($"🔍 Found {allElements.Length} elements on Tables page");

                // Find buttons on Tables page
                var buttons = allElements
                    .Where(e => e.ControlType == ControlType.Button && !string.IsNullOrEmpty(e.Name))
                    .ToList();

                Console.WriteLine($"🔘 Found {buttons.Count} buttons on Tables page:");
                foreach (var button in buttons.Take(10))
                {
                    Console.WriteLine($"   🔘 '{button.Name}' at {button.BoundingRectangle}");
                    button.DrawHighlight();
                    Thread.Sleep(1000);
                }

                // Find text elements
                var textElements = allElements
                    .Where(e => e.ControlType == ControlType.Text && !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.Length > 3)
                    .Take(10)
                    .ToList();

                Console.WriteLine($"📝 Found {textElements.Count} text elements on Tables page:");
                foreach (var text in textElements)
                {
                    Console.WriteLine($"   📝 '{text.Name}'");
                }

                // Find list items (potential table data)
                var listItems = allElements
                    .Where(e => e.ControlType == ControlType.ListItem && !string.IsNullOrEmpty(e.Name))
                    .Take(5)
                    .ToList();

                Console.WriteLine($"📋 Found {listItems.Count} list items on Tables page:");
                foreach (var item in listItems)
                {
                    Console.WriteLine($"   📋 '{item.Name}'");
                    item.DrawHighlight();
                    Thread.Sleep(1000);
                }

                return true;
            });

            if (!exploreTask.Wait(TimeSpan.FromSeconds(15)))
            {
                throw new TimeoutException("Exploring Tables page elements timed out");
            }

            Console.WriteLine("✅ Successfully explored Tables page elements");
        }

        private void DemonstrateTablesActions()
        {
            Console.WriteLine("⏰ Demonstrating Tables actions...");
            
            var actionTask = Task.Run(() =>
            {
                if (_mainWindow == null) return false;

                var allElements = _mainWindow.FindAllDescendants();
                
                // Look for Add button
                var addButton = allElements
                    .Where(e => e.ControlType == ControlType.Button && !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("add"))
                    .FirstOrDefault();

                if (addButton != null)
                {
                    Console.WriteLine($"✅ Found Add button: '{addButton.Name}'");
                    Console.WriteLine($"👀 WATCH: Add button will be highlighted!");
                    addButton.DrawHighlight();
                    Thread.Sleep(3000);
                    
                    Console.WriteLine($"👀 WATCH: Add button will be clicked!");
                    addButton.Click();
                    Console.WriteLine($"✅ CLICKED Add button!");
                    
                    Thread.Sleep(5000);
                }

                // Look for Edit button
                var editButton = allElements
                    .Where(e => e.ControlType == ControlType.Button && !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("edit"))
                    .FirstOrDefault();

                if (editButton != null)
                {
                    Console.WriteLine($"✅ Found Edit button: '{editButton.Name}'");
                    Console.WriteLine($"👀 WATCH: Edit button will be highlighted!");
                    editButton.DrawHighlight();
                    Thread.Sleep(3000);
                }

                // Look for Delete button
                var deleteButton = allElements
                    .Where(e => e.ControlType == ControlType.Button && !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("delete"))
                    .FirstOrDefault();

                if (deleteButton != null)
                {
                    Console.WriteLine($"✅ Found Delete button: '{deleteButton.Name}'");
                    Console.WriteLine($"👀 WATCH: Delete button will be highlighted!");
                    deleteButton.DrawHighlight();
                    Thread.Sleep(3000);
                }

                return true;
            });

            if (!actionTask.Wait(TimeSpan.FromSeconds(20)))
            {
                throw new TimeoutException("Demonstrating Tables actions timed out");
            }

            Console.WriteLine("✅ Successfully demonstrated Tables actions");
        }

        private void ShowTablesDataAndContent()
        {
            Console.WriteLine("⏰ Showing Tables data and content...");
            
            var dataTask = Task.Run(() =>
            {
                if (_mainWindow == null) return false;

                var allElements = _mainWindow.FindAllDescendants();
                
                Console.WriteLine($"📊 Current window title: '{_mainWindow.Title}'");
                Console.WriteLine($"📍 Window bounds: {_mainWindow.BoundingRectangle}");

                // Show all interactive elements
                var interactiveElements = allElements
                    .Where(e => e.ControlType == ControlType.Button || 
                               e.ControlType == ControlType.ListItem || 
                               e.ControlType == ControlType.MenuItem)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Take(15)
                    .ToList();

                Console.WriteLine($"🎯 Found {interactiveElements.Count} interactive elements:");
                foreach (var element in interactiveElements)
                {
                    Console.WriteLine($"   🎯 {element.ControlType}: '{element.Name}' at {element.BoundingRectangle}");
                    element.DrawHighlight();
                    Thread.Sleep(800);
                }

                return true;
            });

            if (!dataTask.Wait(TimeSpan.FromSeconds(15)))
            {
                throw new TimeoutException("Showing Tables data timed out");
            }

            Console.WriteLine("✅ Successfully showed Tables data and content");
        }

        private void NavigateToOtherPages()
        {
            Console.WriteLine("⏰ Navigating to other pages...");
            
            var navigateTask = Task.Run(() =>
            {
                if (_mainWindow == null) return false;

                var allElements = _mainWindow.FindAllDescendants();
                
                // Try to navigate to Dashboard
                var dashboardElement = allElements
                    .Where(e => e.ControlType == ControlType.ListItem)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("dashboard"))
                    .FirstOrDefault();

                if (dashboardElement != null)
                {
                    Console.WriteLine($"✅ Found Dashboard navigation: '{dashboardElement.Name}'");
                    Console.WriteLine($"👀 WATCH: Navigating back to Dashboard!");
                    dashboardElement.DrawHighlight();
                    Thread.Sleep(2000);
                    
                    dashboardElement.Click();
                    Console.WriteLine($"✅ CLICKED Dashboard navigation!");
                    
                    Thread.Sleep(5000);
                }

                // Try to navigate to Orders
                var ordersElement = allElements
                    .Where(e => e.ControlType == ControlType.ListItem)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("order"))
                    .FirstOrDefault();

                if (ordersElement != null)
                {
                    Console.WriteLine($"✅ Found Orders navigation: '{ordersElement.Name}'");
                    Console.WriteLine($"👀 WATCH: Navigating to Orders page!");
                    ordersElement.DrawHighlight();
                    Thread.Sleep(2000);
                    
                    ordersElement.Click();
                    Console.WriteLine($"✅ CLICKED Orders navigation!");
                    
                    Thread.Sleep(5000);
                }

                return true;
            });

            if (!navigateTask.Wait(TimeSpan.FromSeconds(20)))
            {
                throw new TimeoutException("Navigating to other pages timed out");
            }

            Console.WriteLine("✅ Successfully demonstrated navigation to other pages");
        }
    }
}
