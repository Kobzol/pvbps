using System;
using System.Collections.Generic;

namespace Antivirus.Util
{
    public class SubscriptionManager: IDisposable
    {
        private List<IDisposable> disposables = new List<IDisposable>();

        public static SubscriptionManager operator+(SubscriptionManager manager, IDisposable disposable)
        {
            manager.Add(disposable);
            return manager;
        }

        public void Add(IDisposable disposable)
        {
            this.disposables.Add(disposable);
        }

        public void Dispose()
        {
            this.disposables.ForEach(it => it.Dispose());
            this.disposables.Clear();
        }
    }
}
