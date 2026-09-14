using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Game Event", menuName = "BlobbRoll/Events/Game Event")]
public class GameEvent : ScriptableObject
{
    private readonly List<GameEventListener> m_listeners = new List<GameEventListener>();
    public event System.Action OnRaised;

    public void Raise()
    {
        for(int i = m_listeners.Count - 1; i >= 0; i--)
        {
            m_listeners[i].OnEventRaised();
        }

        OnRaised?.Invoke();
    }

    public void RegisterListener(GameEventListener listener)
    {
        if(!m_listeners.Contains(listener))
        {
            m_listeners.Add(listener);
        }
    }

    public void UnregisterListener(GameEventListener listener)
    {
        if(m_listeners.Contains(listener))
        {
            m_listeners.Remove(listener);
        }
    }
}

public abstract class GameEvent<T> : ScriptableObject
{
    private readonly List<GameEventListener<T>> m_listeners = new List<GameEventListener<T>>();

    public event System.Action<T> OnRaised;

    public void Raise(T value)
    {
        for(int i = m_listeners.Count - 1; i >= 0; i--)
        {
            m_listeners[i].OnEventRaised(value);
        }
        OnRaised?.Invoke(value);
    }

    public void RegisterListener(GameEventListener<T> listener)
    {
        if(!m_listeners.Contains(listener))
        {
            m_listeners.Add(listener);
        }
    }

    public void UnregisterListener(GameEventListener<T> listener)
    {
        if(m_listeners.Contains(listener))
        {
            m_listeners.Remove(listener);
        }
    }
}
