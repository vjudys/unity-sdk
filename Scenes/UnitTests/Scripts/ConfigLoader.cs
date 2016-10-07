using UnityEngine;
using System.Collections;
using IBM.Watson.DeveloperCloud.Utilities;

//! This helper class makes sure the Watson configuration is fully loaded before we try to access any of the services.
public class ConfigLoader : MonoBehaviour
{
    [SerializeField]
    private GameObject m_Prefab = null;
    private GameObject m_CreatedObject = null;

    [SerializeField]
    private bool m_SurfaceBuild = false;

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

    IEnumerator Start()
    {
        // wait for the configuration to be loaded first..
        while (!Config.Instance.ConfigLoaded)
            yield return null;

        // then initiate a prefab after we are done loading the config.
        m_CreatedObject = GameObject.Instantiate(m_Prefab);
    }

    /// <summary>
    /// Handler for user logout
    /// </summary>
    /// <param name="args"></param>
    public void OnUserLogOut(System.Object[] args)
    {
        Config.Instance.IsLoggedIn = false;
        StopWatch.instance.StopAllTimers();

        if (m_CreatedObject != null)
        {
            if (!m_CreatedObject.activeSelf)
                m_CreatedObject.SetActive(true);

            m_CreatedObject.SendMessage("DestroyCreatedObject", SendMessageOptions.DontRequireReceiver);
        }
        StartCoroutine(Start());
    }
        
    public void Update()
    {
        OrientScreen();
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

    public void Awake()
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
}
