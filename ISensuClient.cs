using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Remoting;

namespace sensu_client
{
    public interface ISensuClient
    {
        void LoadConfiguration();
        void RequestAdditionalTime(int milliseconds);
        void Stop();
        void ServiceMainCallback(int argCount, IntPtr argPointer);
        bool AutoLog { get; set; }
        int ExitCode { get; set; }
        bool CanHandlePowerEvent { get; set; }
        bool CanHandleSessionChangeEvent { get; set; }
        bool CanPauseAndContinue { get; set; }
        bool CanShutdown { get; set; }
        bool CanStop { get; set; }
        EventLog EventLog { get; }
        string ServiceName { get; set; }
        ISite Site { get; set; }
        IContainer Container { get; }
        void Dispose();
        string ToString();
        event EventHandler Disposed;
        object GetLifetimeService();
        object InitializeLifetimeService();
        ObjRef CreateObjRef(Type requestedType);
    }
}