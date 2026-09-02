using System;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Xsl;

namespace ArmyLib.Source
{
    public class GarbageAppHandler
    {
        public static readonly Lazy<GarbageAppHandler> _instance = new Lazy<GarbageAppHandler>(() => new GarbageAppHandler());

        public static GarbageAppHandler Instance => _instance.Value;

        public ObservableCollection<GarbageApp> GarbageApps { get; }

        private readonly object _lock = new object();
        private GarbageAppHandler()
        {
            GarbageApps = new ObservableCollection<GarbageApp>();
            BindingOperations.EnableCollectionSynchronization(GarbageApps, _lock);
        }

        public void AddGarbageApp(GarbageApp app)
        {
            lock (_lock)
            {
                GarbageApps.Add(app);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                GarbageApps.Clear();
            }
        }

        public GarbageApp GetGarbageAppByAppID(string appID)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(appID))
                {
                    Debug.WriteLine("GetGarbageAppByAppID : cant ind appID");
                    return null;
                }

                else if(GarbageApps == null || GarbageApps.Count == 0)
                {
                    Debug.WriteLine("GetGarbageAppByAppID : cant find any ggarbage apps");
                    return null;
                }

                var result = GarbageApps.FirstOrDefault(x => x != null && !string.IsNullOrEmpty(x.AppIdOrPath) && string.Equals(x.AppIdOrPath, appID, StringComparison.OrdinalIgnoreCase));

                if(result != null)
                {
                    Debug.WriteLine($"GetGarbageAppByAppID : found garbage app with App ID : {appID}");
                }

                else
                {
                    Debug.WriteLine($"GetGarbagAppbyAppID : cant found garbage app with App ID {appID}");
                }

                return result;
            }
        }
    }
}
