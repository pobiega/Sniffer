using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sniffer.Async
{
    public sealed class AsyncEvent<TEventArgs>
    {
        private readonly List<Func<object, TEventArgs, Task>> _invocationList;
        private readonly object _locker;

        private AsyncEvent()
        {
            _invocationList = new List<Func<object, TEventArgs, Task>>();
            _locker = new object();
        }

        public static AsyncEvent<TEventArgs> operator +(
            AsyncEvent<TEventArgs> e, Func<object, TEventArgs, Task> callback)
        {
            _ = callback ?? throw new ArgumentNullException(nameof(callback));

            //Note: Thread safety issue- if two threads register to the same event (on the first time, i.e when it is null)
            //they could get a different instance, so whoever was first will be overridden.
            //A solution for that would be to switch to a public constructor and use it, but then we'll 'lose' the similar syntax to c# events             
            if (e == null) e = new AsyncEvent<TEventArgs>();

            lock (e._locker)
            {
                e._invocationList.Add(callback);
            }
            return e;
        }

        public static AsyncEvent<TEventArgs> operator -(
            AsyncEvent<TEventArgs> e, Func<object, TEventArgs, Task> callback)
        {
            _ = callback ?? throw new ArgumentNullException(nameof(callback));
            _ = e ?? throw new ArgumentNullException(nameof(e));

            lock (e._locker)
            {
                e._invocationList.Remove(callback);
            }
            return e;
        }

        public async Task InvokeAsyncSerial(object sender, TEventArgs eventArgs)
        {
            List<Func<object, TEventArgs, Task>> tmpInvocationList;
            lock (_locker)
            {
                tmpInvocationList = new List<Func<object, TEventArgs, Task>>(_invocationList);
            }

            foreach (var callback in tmpInvocationList)
            {
                // Assuming we want a serial invocation, for a parallel invocation we can use Task.WhenAll instead
                await callback(sender, eventArgs);
            }
        }

        public async Task InvokeAsyncParallel(object sender, TEventArgs eventArgs)
        {
            List<Func<object, TEventArgs, Task>> tmpInvocationList;
            lock (_locker)
            {
                tmpInvocationList = new List<Func<object, TEventArgs, Task>>(_invocationList);
            }

            await Task.WhenAll(tmpInvocationList.Select(i => i(sender, eventArgs)));
        }
    }
}
