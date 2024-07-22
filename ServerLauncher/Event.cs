namespace ServerLauncher.Server;

public delegate void CustomEventHandler();

public class Event
{
    private event CustomEventHandler InnerEvent;

    public static Event operator +(Event @event, CustomEventHandler customEventHandler)
    {
        @event.Subscribe(customEventHandler);
        return @event;
    }

    public static Event operator -(Event @event, CustomEventHandler customEventHandler)
    {
        @event.Unsubscribe(customEventHandler);
        return @event;
    }

    public void Subscribe(CustomEventHandler customEventHandler)
    {
        InnerEvent += customEventHandler;
    }
    
    public void Unsubscribe(CustomEventHandler customEventHandler)
    {
        InnerEvent -= customEventHandler;
    }
    
    public void InvokeSafely()
    {
        InvokeNormal();
    }

    internal void InvokeNormal()
    {
        if (InnerEvent is null)
            return;

        foreach (var handler in InnerEvent.GetInvocationList().Cast<CustomEventHandler>())
        {
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                Program.Logger.Error( nameof(InvokeNormal), $"Method \"{handler.Method.Name}\" of the class \"{handler.Method.ReflectedType.FullName}\" caused an exception when handling the event \"{GetType().FullName}\"\n{ex}");
            }
        }
    }
}