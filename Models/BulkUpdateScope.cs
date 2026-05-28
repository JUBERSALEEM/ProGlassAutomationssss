using System;

namespace ProGlassAutomation.Models
{
    /// <summary>
    /// Shared bulk update scope that can be used across all models
    /// </summary>
    public class BulkUpdateScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                OnDispose();
            }
        }

        protected virtual void OnDispose()
        {
            // Override in derived classes
        }
    }
}