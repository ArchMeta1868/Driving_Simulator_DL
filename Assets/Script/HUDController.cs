using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System;

public class HUDController : MonoBehaviour
{
    [Header("HUD Elements")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI gearText;
    public TextMeshProUGUI timerText;

    [Header("References")]
    public GameObject playerCar;           // PlayerCar object (assign or found by tag)

    private CarControl carControl;
    private float elapsedTime;             // seconds since timer started (unscaled)

    /* -------------------------------------------------------- */
    /*  INITIALISATION                                          */
    /* -------------------------------------------------------- */
    private void Start()
    {
        elapsedTime = 0f;                  // Start timer at zero

        if (playerCar == null)
            playerCar = GameObject.FindWithTag("Player");

        if (playerCar != null)
            carControl = playerCar.GetComponent<CarControl>();

        UpdateTimerLabel(0f);              // Show “00:00.000” immediately
    }

    /* -------------------------------------------------------- */
    /*  MAIN LOOP                                               */
    /* -------------------------------------------------------- */
    private void Update()
    {
        /* ---------- TIMER ----------------------------------- */
        elapsedTime += Time.deltaTime;
        UpdateTimerLabel(elapsedTime);

        /* ---------- SPEED & GEAR ---------------------------- */
        if (carControl != null)
        {
            float speed = carControl.GetSpeedKMH();
            speedText.text = $"Speed: {Mathf.Round(speed)} km/h";
            gearText.text = $"Gear:  {carControl.GetCurrentGear()}";
        }
    }

    /* -------------------------------------------------------- */
    /*  HELPER: format timer                                    */
    /* -------------------------------------------------------- */
    private void UpdateTimerLabel(float timeSeconds)
    {
        // TimeSpan gives robust, localisation-proof formatting
        TimeSpan ts = TimeSpan.FromSeconds(timeSeconds);
        // Format: mm:ss.fff  →  02:15.348
        timerText.text = $"Time: {ts:mm\\:ss\\.fff}";
    }

    /* -------------------------------------------------------- */
    /*  PUBLIC API                                              */
    /* -------------------------------------------------------- */

    /// Call from a Pause Menu or Restart button if you want to manually reset the timer.</summary>
    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateTimerLabel(0f);
    }

    /// Assigned to a UI button’s OnClick() to return to the main menu.</summary>
    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
