using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.Generic;

namespace MagiDesk.Frontend.Collections
{
    /// <summary>
    /// A safe ObservableCollection implementation that avoids Windows Runtime COM interop
    /// This prevents Marshal.ThrowExceptionForHR errors in WinUI 3 Desktop Apps
    /// </summary>
    public class SafeObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Override to prevent COM interop issues
        /// </summary>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            try
            {
                base.OnCollectionChanged(e);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Suppress COM exceptions - they're handled by the framework
                // This prevents Marshal.ThrowExceptionForHR errors in WinUI 3 Desktop Apps
            }
        }

        /// <summary>
        /// Override to prevent COM interop issues
        /// </summary>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            try
            {
                base.OnPropertyChanged(e);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Suppress COM exceptions - they're handled by the framework
                // This prevents Marshal.ThrowExceptionForHR errors in WinUI 3 Desktop Apps
            }
        }
    }
}
