public class LampState
{
    public bool On { get; set; }
    public bool Blink { get; set; }

    public LampState(bool on = false, bool blink = false)
    {
        On = on;
        Blink = blink;
    }
}