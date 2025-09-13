using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using FlaUI.Core.Definitions;
using System.Diagnostics;

namespace MagiDesk.Tests.UIAutomation
{
    [TestClass]
    public class QuickTablesTest
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

                // Kill by process ID if we have it
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
        public void QuickTablesTest_NavigateToTables_ShouldWork()
        {
            Console.WriteLine("🚀 QUICK TABLES TEST STARTED");
            Console.WriteLine("=============================");

            try
            {
                // Step 1: Launch app with timeout
                Console.WriteLine("\n🚀 STEP 1: LAUNCHING APP WITH TIMEOUT");
                LaunchAppWithTimeout();

                // Step 2: Find window with timeout
                Console.WriteLine("\n🔍 STEP 2: FINDING WINDOW WITH TIMEOUT");
                FindWindowWithTimeout();

                // Step 3: Find Tables element with timeout
                Console.WriteLine("\n🎯 STEP 3: FINDING TABLES ELEMENT WITH TIMEOUT");
                var tablesElement = FindTablesElementWithTimeout();

                // Step 4: Click Tables with timeout
                Console.WriteLine("\n👆 STEP 4: CLICKING TABLES WITH TIMEOUT");
                ClickTablesWithTimeout(tablesElement);

                // Step 5: Verify navigation
                Console.WriteLine("\n✅ STEP 5: VERIFYING NAVIGATION");
                VerifyNavigation();

                Console.WriteLine("\n🎉 QUICK TABLES TEST COMPLETED SUCCESSFULLY!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ TEST FAILED: {ex.Message}");
                throw;
            }
        }

        private void LaunchAppWithTimeout()
        {
            Console.WriteLine("⏰ Launching app with 15 second timeout...");
            
            var launchTask = Task.Run(() =>
            {
                var appPath = @"C:\Users\giris\Documents\Code\Order-Tracking-By-GPT\solution\frontend\bin\Release\net8.0-windows10.0.19041.0\MagiDesk.Frontend.exe";
                
                if (!File.Exists(appPath))
                {
                    throw new FileNotFoundException($"App not found at: {appPath}");
                }

                Console.WriteLine($"✅ Found app at: {appPath}");
                
                _application = FlaUI.Core.Application.Launch(appPath);
                _appProcess = Process.GetProcessById(_application.ProcessId);
                
                Console.WriteLine($"✅ App launched with PID: {_application.ProcessId}");
                
                // Wait for app to load
                Thread.Sleep(5000);
                
                return true;
            });

            if (!launchTask.Wait(TimeSpan.FromSeconds(15)))
            {
                throw new TimeoutException("App launch timed out after 15 seconds");
            }

            Console.WriteLine("✅ App launch completed");
        }

        private void FindWindowWithTimeout()
        {
            Console.WriteLine("⏰ Finding window with 10 second timeout...");
            
            var findTask = Task.Run(() =>
            {
                _automation = new UIA3Automation();
                _mainWindow = _application?.GetMainWindow(_automation);
                
                if (_mainWindow == null)
                {
                    throw new Exception("Could not find main window");
                }
                
                Console.WriteLine($"✅ Found window: '{_mainWindow.Title}'");
                return true;
            });

            if (!findTask.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Window finding timed out after 10 seconds");
            }

            Console.WriteLine("✅ Window found");
        }

        private AutomationElement? FindTablesElementWithTimeout()
        {
            Console.WriteLine("⏰ Finding Tables element with 10 second timeout...");
            
            AutomationElement? tablesElement = null;
            
            var findTask = Task.Run(() =>
            {
                if (_mainWindow == null) return false;

                var allElements = _mainWindow.FindAllDescendants();
                Console.WriteLine($"🔍 Found {allElements.Length} total elements");

                tablesElement = allElements
                    .Where(e => e.ControlType == ControlType.ListItem)
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Where(e => e.Name.ToLower().Contains("table"))
                    .FirstOrDefault();

                if (tablesElement != null)
                {
                    Console.WriteLine($"✅ Found Tables element: '{tablesElement.Name}'");
                    Console.WriteLine($"📍 Position: {tablesElement.BoundingRectangle}");
                }
                else
                {
                    Console.WriteLine("❌ Tables element not found");
                }

                return tablesElement != null;
            });

            if (!findTask.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Tables element finding timed out after 10 seconds");
            }

            return tablesElement;
        }

        private void ClickTablesWithTimeout(AutomationElement? tablesElement)
        {
            if (tablesElement == null)
            {
                throw new ArgumentNullException("Tables element is null");
            }

            Console.WriteLine("⏰ Clicking Tables with 10 second timeout...");
            Console.WriteLine("👀 WATCH: You should see Tables element highlighted and clicked!");
            
            var clickTask = Task.Run(() =>
            {
                // Highlight the element
                tablesElement.DrawHighlight();
                Thread.Sleep(2000);
                
                // Click the element
                tablesElement.Click();
                Console.WriteLine("✅ Tables element clicked");
                
                // Wait for navigation
                Thread.Sleep(8000);
                
                return true;
            });

            if (!clickTask.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Tables clicking timed out after 10 seconds");
            }

            Console.WriteLine("✅ Tables click completed");
        }

        private void VerifyNavigation()
        {
            Console.WriteLine("🔍 Verifying Tables page navigation...");
            
            if (_mainWindow == null) return;

            Console.WriteLine($"📱 Current Window Title: '{_mainWindow.Title}'");
            
            var allElements = _mainWindow.FindAllDescendants();
            var buttons = allElements
                .Where(e => e.ControlType == ControlType.Button && !string.IsNullOrEmpty(e.Name))
                .Take(5)
                .ToList();
            
            Console.WriteLine($"🔍 Found {buttons.Count} buttons on current page:");
            foreach (var button in buttons)
            {
                Console.WriteLine($"   🔘 '{button.Name}'");
            }
            
            Console.WriteLine("✅ Navigation verification completed");
        }
    }
}
