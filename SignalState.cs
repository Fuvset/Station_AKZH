using System.Collections.Generic;

public class SignalState
{
    public Dictionary<string, LampState> Lamps { get; set; } = new();
    public bool Manual { get; set; } = false;
}