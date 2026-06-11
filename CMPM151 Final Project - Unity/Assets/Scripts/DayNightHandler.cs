using UnityEngine;

public class DayNightHandler : MonoBehaviour
{
    public float cycleTime = 30;
    public float timeSoFar = 0;
    public Camera mainCamera;
    private bool isDay = true;
    /*
    void Start()
    {
        OSCHandler.Instance.SendMessageToClient("pd", "/oscmusicon", 1);
    }
    */

    void Update()
    {
        // Should swap between day and night time every cycleTime seconds
        // and send a message to pd to trigger the change in music
        timeSoFar += Time.deltaTime;
        if (timeSoFar >= cycleTime)
        {
            timeSoFar = 0;
            onCycleChange();
        }
    }

    void onCycleChange()
    {
        isDay = !isDay;
        OSCHandler.Instance.SendMessageToClient("pd", "/unity/cyclechange", 1);
        mainCamera.backgroundColor = mainCamera.backgroundColor == Color.darkGray ? Color.lightBlue : Color.darkGray;
    }

    public bool IsDay()
    {
        return isDay;
    }
}
