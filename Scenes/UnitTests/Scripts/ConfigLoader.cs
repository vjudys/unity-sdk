using UnityEngine;
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
        EndUserSession();
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
        EndUserSession();

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
            // Very rudementry way of getting all builds to be low res, this is not very effective, but needed for Surface Distribution, until we have some time to implement a "clever" solution
            QualitySettings.SetQualityLevel(1);
            if ((QualitySettings.names[QualitySettings.GetQualityLevel()] == "LoRes") || (QualitySettings.names[QualitySettings.GetQualityLevel()] == "Tablet"))
            {
                Screen.SetResolution(1440, 900, true, 60);
            }
        }
    }


    /// <summary>
    /// Ends the user session. This sends the current SessionID to the backend if there is one.
    /// </summary>
    public void EndUserSession()
    {
        Log.Status("ConfigLoader", "Sending session close to backend");
        
        //performs only if the session is valid
        if (!string.IsNullOrEmpty(Config.Instance.SessionID))
        {
            // When Application Closes, sends sessionID to the backend.
            IBM.Watson.Solutions.XRay.Utilities.Session session = new IBM.Watson.Solutions.XRay.Utilities.Session();
            session.StopSession(Config.Instance.SessionID);
        }
    }
}
