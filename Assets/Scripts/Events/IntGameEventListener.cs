using UnityEngine.Events;

[System.Serializable]
public class IntUnityEvent : UnityEvent<int>
{
}

public class IntGameEventListener : GameEventListener<int>
{
    public IntUnityEvent response;

    public override void OnEventRaised(int value)
    {
        response.Invoke(value);
    }
}
