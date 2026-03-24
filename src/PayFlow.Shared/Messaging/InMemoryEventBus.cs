namespace PayFlow.Shared.Messaging;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Func<object, Task>>> _handlers = new();
    private readonly object _lock = new();

    public void Subscribe<T>(Func<T, Task> handler) where T : class
    {
        var type = typeof(T);
        lock (_lock)
        {
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Func<object, Task>>();
                _handlers[type] = list;
            }
            list.Add(e => handler((T)e));
        }
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        var type = typeof(T);
        List<Func<object, Task>> snapshot;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(type, out var list))
                return;
            snapshot = new List<Func<object, Task>>(list);
        }

        foreach (var handler in snapshot)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await handler(@event);
        }
    }
}
