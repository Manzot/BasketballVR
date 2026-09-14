using UnityEngine;
using UnityEngine.Events;

public class GameEventListener : MonoBehaviour
{
    public GameEvent gameEvent;
    public UnityEvent response;

    private void OnEnable()
    {
        if(gameEvent != null)
        {
            gameEvent.RegisterListener(this);
        }
    }

    private void OnDisable()
    {
        if(gameEvent != null)
        {
            gameEvent.UnregisterListener(this);
        }
    }

    public void OnEventRaised()
    {
        response.Invoke();
    }
}

public abstract class GameEventListener<T> : MonoBehaviour
{
    public GameEvent<T> gameEvent;

    private void OnEnable()
    {
        if(gameEvent != null)
        {
            gameEvent.RegisterListener(this);
        }
    }

    private void OnDisable()
    {
        if(gameEvent != null)
        {
            gameEvent.UnregisterListener(this);
        }
    }

    public abstract void OnEventRaised(T value);
}
