namespace DNPDemo
{
    /// <summary>
    /// Player roles from the scenario. Assistant A hosts the session; Assistant B joins over LAN.
    /// </summary>
    public enum DemoRole : byte
    {
        None = 0,
        AssistantA = 1,
        AssistantB = 2,
    }
}
