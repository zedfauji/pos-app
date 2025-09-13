using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using FlaUI.Core.Definitions;
using System.Diagnostics;

namespace MagiDesk.Tests.UIAutomation
{
    [TestClass]
    public class SimpleNavigationTest
    {
        private FlaUI.Core.Application? _application;
        private Window? _mainWindow;
        private UIA3Automation? _automation;

        [TestInitialize]
        public async Task Setup()
        {
            Console.WriteLine("🚀 SIMPLE NAVIGATION TEST INITIALIZATION");
            Console.WriteLine("==========================================");

            try
            {
                await LaunchMagiDeskApplication();
                await FindMainWindow();
                Console.WriteLine("✅ Setup completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Setup failed: {ex.Message}");
                throw;
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            Console.WriteLine("🧹 CLEANUP STARTED");
            
            try
            {
                // Force kill any MagiDesk processes
                var processes = Process.GetProcessesByName("MagiDesk.Frontend");
                foreach (var process in processes)
                {
                    Console.WriteLine($"🔪 Killing process: {process.Id}");
                    process.Kill();
                    process.WaitForExit(5000);
                }

                if (_application != null)
                {
                    Console.WriteLine("🚪 Closing FlaUI application...");
                    _application.Close();
                    _application.Dispose();
                }
                
                _automation?.Dispose();
                Console.WriteLine("✅ CLEANUP COMPLETED");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Cleanup error: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task SimpleNavigationTest_NavigateToTables_ShouldWork()
        {
            Console.WriteLine("🎬 STARTING SIMPLE TABLES NAVIGATION TEST");
            Console.WriteLine("==========================================");

            try
            {
                Assert.IsNotNull(_mainWindow, "Main window should be available");
                
                // Step 1: Find the Tables navigation element
                Console.WriteLine("\n🔍 STEP 1: FINDING TABLES NAVIGATION ELEMENT");
                Console.WriteLine("=============================================");
                
                var tablesElement = FindTablesNavigationElement();
                Assert.IsNotNull(tablesElement, "Tables navigation element should be found");
                
                Console.WriteLine($"✅ Found Tables element: '{tablesElement.Name}'");
                Console.WriteLine($"📍 Position: {tablesElement.BoundingRectangle}");
                Console.WriteLine($"🔍 IsEnabled: {tablesElement.IsEnabled}");
                Console.WriteLine($"🔍 IsOffscreen: {tablesElement.IsOffscreen}");
                
                // Step 2: Highlight and click the Tables element
                Console.WriteLine("\n🎯 STEP 2: CLICKING TABLES NAVIGATION ELEMENT");
                Console.WriteLine("=============================================");
                
                Console.WriteLine("👀 WATCH: You should see the Tables element highlighted!");
                tablesElement.DrawHighlight();
                await Task.Delay(2000);
                
                Console.WriteLine("👀 WATCH: You should see a click happening on Tables!");
                
                // Use forced timeout for click operation
                var clickTask = Task.Run(() => {
                    try
                    {
                        tablesElement.Click();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ❌ Click failed: {ex.Message}");
                        return false;
                    }
                });
                
                if (clickTask.Wait(TimeSpan.FromSeconds(10)))
                {
                    if (clickTask.Result)
                    {
                        Console.WriteLine("✅ CLICKED Tables navigation element");
                    }
                    else
                    {
                        Console.WriteLine("❌ Failed to click Tables navigation element");
                    }
                }
                else
                {
                    Console.WriteLine("⏰ FORCED TIMEOUT: Clicking Tables took longer than 10 seconds");
                }
                
                // Step 3: Wait for navigation and verify
                Console.WriteLine("\n⏳ STEP 3: WAITING FOR NAVIGATION TO COMPLETE");
                Console.WriteLine("=============================================");
                
                Console.WriteLine("⏳ Waiting 10 seconds for Tables page to load...");
                await Task.Delay(10000);
                
                // Step 4: Verify we're on the Tables page
                Console.WriteLine("\n🔍 STEP 4: VERIFYING TABLES PAGE NAVIGATION");
                Console.WriteLine("===========================================");
                
                await VerifyTablesPage();
                
                Console.WriteLine("🎉 SIMPLE TABLES NAVIGATION TEST COMPLETED SUCCESSFULLY!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"📋 Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private AutomationElement? FindTablesNavigationElement()
        {
            if (_mainWindow == null || _automation == null) return null;

            Console.WriteLine("🔍 Searching for Tables navigation element...");
            
            var allElements = _mainWindow.FindAllDescendants();
            Console.WriteLine($"🔍 Found {allElements.Length} total elements");

            // Look for Tables navigation specifically
            var tablesElements = allElements
                .Where(e => e.ControlType == ControlType.ListItem)
                .Where(e => !string.IsNullOrEmpty(e.Name))
                .Where(e => e.Name.ToLower().Contains("table"))
                .ToList();

            Console.WriteLine($"🔍 Found {tablesElements.Count} potential Tables elements");
            
            foreach (var element in tablesElements)
            {
                Console.WriteLine($"   📍 '{element.Name}' at {element.BoundingRectangle}");
            }

            return tablesElements.FirstOrDefault();
        }

        private async Task VerifyTablesPage()
        {
            if (_mainWindow == null) return;

            Console.WriteLine("🔍 Verifying Tables page content...");
            
            // Get current window title
            Console.WriteLine($"📱 Current Window Title: '{_mainWindow.Title}'");
            
            // Look for Tables-specific content
            var allElements = _mainWindow.FindAllDescendants();
            
            // Look for buttons that might be on Tables page
            var buttons = allElements
                .Where(e => e.ControlType == ControlType.Button && !string.IsNullOrEmpty(e.Name))
                .Take(10)
                .ToList();
            
            Console.WriteLine($"🔍 Found {buttons.Count} buttons on current page:");
            foreach (var button in buttons)
            {
                Console.WriteLine($"   🔘 '{button.Name}' at {button.BoundingRectangle}");
                button.DrawHighlight();
                await Task.Delay(500);
            }
            
            // Look for text elements that might indicate Tables page
            var textElements = allElements
                .Where(e => e.ControlType == ControlType.Text && !string.IsNullOrEmpty(e.Name))
                .Where(e => e.Name.Length > 3)
                .Take(10)
                .ToList();
            
            Console.WriteLine($"🔍 Found {textElements.Count} text elements on current page:");
            foreach (var text in textElements)
            {
                Console.WriteLine($"   📝 '{text.Name}' at {text.BoundingRectangle}");
            }
            
            Console.WriteLine("✅ Tables page verification completed");
        }

        private async Task LaunchMagiDeskApplication()
        {
            Console.WriteLine("🚀 Launching MagiDesk Application...");
            
            var possiblePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "frontend", "bin", "Release", "net8.0-windows10.0.19041.0", "MagiDesk.Frontend.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "frontend", "bin", "Release", "net8.0-windows10.0.19041.0", "MagiDesk.Frontend.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "frontend", "bin", "Release", "net8.0-windows10.0.19041.0", "MagiDesk.Frontend.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "frontend", "bin", "Debug", "net8.0-windows10.0.19041.0", "MagiDesk.Frontend.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "frontend", "bin", "Debug", "net8.0-windows10.0.19041.0", "MagiDesk.Frontend.exe")
            };

            string? appPath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    appPath = path;
                    Console.WriteLine($"✅ Found application at: {path}");
                    break;
                }
            }

            if (appPath == null)
            {
                throw new FileNotFoundException("Could not find MagiDesk.Frontend.exe");
            }

            _application = FlaUI.Core.Application.Launch(appPath);
            Console.WriteLine($"✅ Application launched with PID: {_application.ProcessId}");
            
            Console.WriteLine("⏳ Waiting 10 seconds for application to fully load...");
            await Task.Delay(10000);
        }

        private async Task FindMainWindow()
        {
            Console.WriteLine("🔍 Finding main window...");
            
            _automation = new UIA3Automation();
            _mainWindow = _application?.GetMainWindow(_automation);
            
            if (_mainWindow == null)
            {
                throw new Exception("Could not find main window");
            }
            
            Console.WriteLine($"✅ Found main window: '{_mainWindow.Title}'");
            Console.WriteLine($"📍 Window bounds: {_mainWindow.BoundingRectangle}");
            
            await Task.Delay(1000);
        }
    }
}
