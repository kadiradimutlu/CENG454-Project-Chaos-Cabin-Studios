using UnityEngine;

public class UnityMouseInputReader : MonoBehaviour, ICameraInputReader
{
    public Vector2 ReadLookInput()
    {
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }
}
