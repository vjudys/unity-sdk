﻿using UnityEngine;
using System;   // needed for Array
using System.Collections;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.Logging;

//! This helper class makes sure the Watson configuration is fully loaded before we try to access any of the services.
public class ConfigLoader : MonoBehaviour
{
    [SerializeField]
    private GameObject m_Prefab = null;
    private GameObject m_CreatedObject = null;

    [SerializeField]
    private bool m_SurfaceBuild = false; // true to set if this is a surface build and force the resolution

    string[] m_CommandLineArgs = System.Environment.GetCommandLineArgs();

    #region OnEnable / OnDisable - Registering events
    void OnEnable()
    {
        EventManager.Instance.RegisterEventReceiver("OnUserToLogout", OnUserLogOut);
    }

    void OnDisable()
    {
        EventManager.Instance.UnregisterEventReceiver("OnUserToLogout", OnUserLogOut);
    }
    #endregion

    #region Unity Monobehavior Functions
    IEnumerator Start()
    {
        // wait for the configuration to be loaded first..
        while (!Config.Instance.ConfigLoaded)
            yield return null;

        // then initiate a prefab after we are done loading the config.
        m_CreatedObject = GameObject.Instantiate(m_Prefab);
    }

    public void Update()
    {
        OrientScreen();
    }

    public void Awake()
    {
        SetScreenResolution();
    }

    public void OnApplicationQuit()
    {
        //sends the close session to the user on the backend
        EndUserSession(true);
    }

    //<summary> The applications focus has changed </sumary>
    void OnApplicationFocus( bool hasFocus )
    {
        if ( hasFocus && Config.Instance.IsLoggedIn ) // application now in focus and user is logged in
        {
            if (Config.Instance.GetLoginDuration() > Azure.Instance.LOGOUT_DURATION)
            {
                EventManager.Instance.SendEvent("OnUserToLogout");
            }
            else if (Config.Instance.GetLoginDuration() > Azure.Instance.REFRESH_TOKEN_DURATION)
            {
                Azure.Instance.RefreshToken();
                Config.Instance.SetLoginTime();
            }
        }
    }
    #endregion

    /// <summary>
    /// Handler for user logout
    /// </summary>
    /// <param name="args"></param>
    public void OnUserLogOut(System.Object[] args)
    {
        Config.Instance.IsLoggedIn = false;
        StopWatch.instance.StopAllTimers();

        // Stops the session when user logs out
        EndUserSession(false);

        if (m_CreatedObject != null)
        {
            if (!m_CreatedObject.activeSelf)
                m_CreatedObject.SetActive(true);

            m_CreatedObject.SendMessage("DestroyCreatedObject", SendMessageOptions.DontRequireReceiver);
        }

        StartCoroutine(Start());
    }


    // Sets the screen orientation
    public void OrientScreen()
    {
        DeviceOrientation orientation = Input.deviceOrientation;
        if (orientation == DeviceOrientation.LandscapeLeft)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
        else if (orientation == DeviceOrientation.LandscapeRight)
        {
            Screen.orientation = ScreenOrientation.LandscapeRight;
        }
        else
        {
            Screen.orientation = ScreenOrientation.LandscapeRight;
        }
    }


    /// <summary>
    /// Sets the screen resolution.
    /// </summary>
    private void SetScreenResolution()
    {
        // TODO: SETUP A SURFACE GLOBAL NAME that will allow builds to be surface specific, and disable and lower certain qualities
        if (m_SurfaceBuild)
        {
            // Allows for the system to take in a resolution size
            if (CheckInputResolution())
            {
                SetResolutionWithArguments();
            }
            else
            {
                // Very rudementry way of getting all builds to be low res, this is not very effective, but needed for Surface Distribution, until we have some time to implement a "clever" solution
                QualitySettings.SetQualityLevel(1);
                if ((QualitySettings.names[QualitySettings.GetQualityLevel()] == "LoRes") || (QualitySettings.names[QualitySettings.GetQualityLevel()] == "Tablet"))
                {
                    Screen.SetResolution(1440, 900, true, 60);
                }
            }
        }
    }

    /// <summary>
    /// Checks the input arguments are the correct amount
    /// </summary>
    private bool CheckInputResolution()
    {
        if ((m_CommandLineArgs.Length) >= 3)
            return true;
        return false;
    }

    private void SetResolutionWithArguments()
    {
        // get index of the resolution input name
        int resIndex = Array.FindIndex(m_CommandLineArgs, GetInputResolution);
        // checks to see if there are two elements after the '-res' keyword
        if ((m_CommandLineArgs.Length - (resIndex + 1)) >= 2)
        {
            int resWidth = Int32.Parse(m_CommandLineArgs[resIndex+1]);
            int resHeight = Int32.Parse(m_CommandLineArgs[resIndex+2]);
            Screen.SetResolution(resWidth, resHeight, true, 60);
        }
    }

    /// <summary>
    /// Predicate : Gets the input resolution.
    /// </summary>
    private static bool GetInputResolution(string s)
    {
        if (s.ToLower() == "-res")
            return true;
        return false;
    }

    #region Close Application
    /// <summary>
    /// Ends the user session. This sends the current SessionID to the backend if there is one.
    /// </summary>
    public void EndUserSession(bool quit)
    {
        //performs only if the session is valid
        if (!string.IsNullOrEmpty(Config.Instance.SessionID))
        {
            Log.Status("ConfigLoader", "Sending session close to backend");

            // When Application Closes, sends sessionID to the backend.
            IBM.Watson.Solutions.XRay.Utilities.Session session = new IBM.Watson.Solutions.XRay.Utilities.Session();
            session.StopSession(Config.Instance.SessionID, close:quit);
            StartCoroutine(KeepAlive()); // Hacky way of keeping the system alive for 4 seconds while we send the correlation ID to the backend
        }
    }

    IEnumerator KeepAlive()
    {
        Log.Debug("ConfigLoader", "KeepAlive");
        yield return new WaitForSeconds(4);
        Log.Debug("ConfigLoader", "KeepAlive Complete");
    }

    #endregion
}
