using System.Collections.Concurrent;
using R3;

namespace ReadBinanceData.AsyncContainer;

public class RxConcurrentDictionary<K,V> : ConcurrentDictionary<K,V>, IDisposable where K : notnull
{
    public ReactiveCommand<(K key,V value)> OnAdd { get; }
    public ReactiveCommand<(K key,V value)> OnRemove { get; }
    public ReactiveCommand<(K key, V value)> OnUpdate { get; }

    public RxConcurrentDictionary()
    {
        OnAdd = new ReactiveCommand<(K,V)>();
        OnRemove = new ReactiveCommand<(K,V)>();
        OnUpdate = new ReactiveCommand<(K,V)>();
    }

    public void Dispose()
    {
        OnAdd.Dispose();
        OnRemove.Dispose();
        OnUpdate.Dispose();
    }
    
    public virtual bool TryRxAddOrUpdate(K key, V addorder)
    {
        if (ContainsKey(key))
        {
            this[key] = addorder;
            OnUpdate.Execute((key, addorder));
            return true;
        }
        
        if (TryAdd(key, addorder))
        {
            OnAdd.Execute((key, addorder));
            return true;
        }
        return false;
    }
    
    public virtual bool TryRxRemove(K key)
    {
        if (TryRemove(key, out var order))
        {
            OnRemove.Execute((key, order));
            return true;
        }
        return false;
    }
}