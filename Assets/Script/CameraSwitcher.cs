using UnityEngine;
public class CameraSwitcher : MonoBehaviour
{
    public Camera firstPersonCam;
    public Camera thirdPersonCam;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            // Toggle camera enabled states
            bool firstActive = firstPersonCam.enabled;
            firstPersonCam.enabled = !firstActive;
            thirdPersonCam.enabled = firstActive;
            // If AudioListener is on one of these cameras, also toggle if necessary
            if (firstPersonCam.GetComponent<AudioListener>())
                firstPersonCam.GetComponent<AudioListener>().enabled = !firstActive;
            if (thirdPersonCam.GetComponent<AudioListener>())
                thirdPersonCam.GetComponent<AudioListener>().enabled = firstActive;
        }
    }
}