
public class PlayerInputHandler
{
    public PlayerButtonMap input;

    public PlayerInputHandler()
    {
        input = new PlayerButtonMap();
        input.Enable();
    }

    public void DisableInputEvents()
    {
        input.Disable();
    }   
}
