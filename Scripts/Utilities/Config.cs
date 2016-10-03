/**
* Copyright 2015 IBM Corp. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
*/

using System.Collections;
using System.Collections.Generic;
using IBM.Watson.DeveloperCloud.Logging;
using UnityEngine;
using FullSerializer;
using System.IO;
using MiniJSON;
using System;

namespace IBM.Watson.DeveloperCloud.Utilities
{
    /// <summary>
    /// This class is used to hold configuration data for SDK.
    /// </summary>
	public class Config
    {
        #region Data Model
        /// <summary>
        /// Serialized class for holding generic key/value pairs.
        /// </summary>
        [fsObject]
        public class Variable
        {
            /// <summary>
            /// The key name.
            /// </summary>
            public string Key { get; set; }
            /// <summary>
            /// The value referenced by the key.
            /// </summary>
            public string Value { get; set; }
        };

        /// <summary>
        /// Serialized class for holding the user credentials for a service.
        /// </summary>
        [fsObject]
        public class CredentialInfo
        {
            /// <summary>
            /// The ID of the service this is the credentials.
            /// </summary>
            public string m_ServiceID;
            /// <summary>
            /// The URL for these credentials.
            /// </summary>
            public string m_URL;
            /// <summary>
            /// The user name for these credentials.
            /// </summary>
            public string m_User;
            /// <summary>
            /// The password for these credentials.
            /// </summary>
            public string m_Password;

            /// <summary>
            /// Generate JSON credentials.
            /// </summary>
            /// <returns>Returns a string of the JSON.</returns>
            public string MakeJSON()
            {
                return "{\n\t\"credentials\": {\n\t\t\"url\": \"" + m_URL + "\",\n\t\t\"username\": \"" + m_User + "\",\n\t\t\"password\": \"" + m_Password + "\"\n\t}\n}";
            }

            /// <summary>
            /// Parses a BlueMix json credentials into this object. This is usualy executed from the Unity Editor tool
            /// </summary>
            /// <param name="json">The JSON data to parse.</param>
            /// <returns>Returns true on success.</returns>
            public bool ParseJSON(string json)
            {
                try
                {
                    IDictionary iParse = Json.Deserialize(json) as IDictionary;
                    IDictionary iCredentials = iParse["credentials"] as IDictionary;
                    m_URL = (string)iCredentials["url"];
                    m_User = (string)iCredentials["username"];
                    m_Password = (string)iCredentials["password"];

                    return true;
                }
                catch (Exception e)
                {
                    Log.Error("Config", "Caught Exception: {0}", e.ToString());
                }

                return false;
            }
        }
        #endregion

        #region Private Data
        [fsProperty]
        private string m_ClassifierDirectory = "Watson/Scripts/Editor/Classifiers/";
        [fsProperty]
        private float m_TimeOut = 30.0f;
        [fsProperty]
        private int m_MaxRestConnections = 5;
        [fsProperty]
        private List<CredentialInfo> m_Credentials = new List<CredentialInfo>();
        [fsProperty]
        private List<Variable> m_Variables = new List<Variable>();

        private static fsSerializer sm_Serializer = new fsSerializer();
        #endregion

        #region Public Properties
        /// <summary>
        /// Returns true if the configuration is loaded or not.
        /// </summary>
        [fsIgnore]
        public bool ConfigLoaded { get; private set; }
        /// <summary>
        /// Returns the singleton instance.
        /// </summary>
        public static Config Instance { get { return Singleton<Config>.Instance; } }
        /// <summary>
        /// Returns the location of the classifiers
        /// </summary>
        public string ClassifierDirectory { get { return m_ClassifierDirectory; } set { m_ClassifierDirectory = value; } }
        /// <summary>
        /// Returns the Timeout for requests made to the server.
        /// </summary>
        public float TimeOut { get { return m_TimeOut; } set { m_TimeOut = value; } }
        /// <summary>
        /// Maximum number of connections Watson will make to the server back-end at any one time.
        /// </summary>
        public int MaxRestConnections { get { return m_MaxRestConnections; } set { m_MaxRestConnections = value; } }
        /// <summary>
        /// Returns the list of credentials used to login to the various services.
        /// </summary>
        public List<CredentialInfo> Credentials { get { return m_Credentials; } set { m_Credentials = value; } }
        /// <summary>
        /// Returns a list of variables which can hold key/value data.
        /// </summary>
        public List<Variable> Variables { get { return m_Variables; } set { m_Variables = value; } }
		/// <summary>
		/// Authentication token obtained from Azure AD
		/// </summary>
		/// <value>The auth token.</value>
		[fsIgnore]
		public string AuthToken { get; set; }
		/// <summary>
		/// Refresh token obtained from Azure AD. Used to generate an Azure Token.
		/// </summary>
		/// <value>The refresh token.</value>
		[fsIgnore]
		public string RefreshToken { get; set;}
        /// <summary>
        /// Stores weather the system is behind a proxy.
        /// </summary>
        /// <value><c>true</c> if active proxy; otherwise, <c>false</c>.</value>
        [fsIgnore]
        public bool ActiveProxy { get; set;}

        /// <summary>
        /// Gets or sets a value indicating whether the session is logged in.
        /// </summary>
        [fsIgnore]
        public bool IsLoggedIn { get; set; }

        #endregion

        #region Session Time
        /// <summary>
        /// Stores the time the user logged into there current session.
        /// </summary>
        private DateTime SessionStartTime { get; set;}

        public void SetLoginTime()
        {
            SessionStartTime = System.DateTime.Now;
        }

        public int GetLoginDuration()
        {
            TimeSpan ts = System.DateTime.Now - SessionStartTime;
            return (int)ts.TotalSeconds;
        }
        #endregion

        /// <summary>
        /// Default constructor will call LoadConfig() automatically.
        /// </summary>
        public Config()
        {
            LoadConfig();
        }

        /// <summary>
        /// Find BlueMix credentials by the service ID.
        /// </summary>
        /// <param name="serviceID">The ID of the service to find.</param>
        /// <returns>Returns null if the credentials cannot be found.</returns>
        public CredentialInfo FindCredentials(string serviceID)
        {
            foreach (var info in m_Credentials)
                if (info.m_ServiceID == serviceID)
                    return info;
            return null;
        }

        /// <summary>
        /// Invoking this function will start the co-routine to load the configuration. The user should check the 
        /// ConfigLoaded property to check when the configuration is actually loaded.
        /// </summary>
        public void LoadConfig()
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            try
            {
                if (!Directory.Exists(Application.streamingAssetsPath))
                    Directory.CreateDirectory(Application.streamingAssetsPath);
                LoadConfig(System.IO.File.ReadAllText(Application.streamingAssetsPath + Constants.Path.CONFIG_FILE));

				// Now read a local override that would allows us to replace distrubuted values with the user specific ones
				string localConfigFileName = Application.streamingAssetsPath + Constants.Path.LOCAL_CONFIG_FILE;
				if (File.Exists(localConfigFileName))
					MergeConfigs(System.IO.File.ReadAllText(localConfigFileName));
			}
            catch (System.IO.FileNotFoundException)
            {
                // mark as loaded anyway, so we don't keep retrying..
                Log.Error("Config", "Failed to load config file.");
                ConfigLoaded = true;
            }
#else
            Runnable.Run(LoadConfigCR());
#endif
        }

        /// <summary>
        /// Load the configuration from the given JSON data.
        /// </summary>
        /// <param name="json">The string containing the configuration JSON data.</param>
        /// <returns></returns>
        public bool LoadConfig(string json)
        {
            try
            {
                fsData data = null;
                fsResult r = fsJsonParser.Parse(json, out data);
                if (!r.Succeeded)
                {
                    Log.Error("Config", "Failed to parse Config.json: {0}", r.ToString());
                    return false;
                }

                object obj = this;
                r = sm_Serializer.TryDeserialize(data, GetType(), ref obj);
                if (!r.Succeeded)
                {
                    Log.Error("Config", "Failed to parse Config.json: {0}", r.ToString());
                    return false;
                }

                ConfigLoaded = true;
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Config", "Failed to load config: {0}", e.ToString());
            }

            ConfigLoaded = true;
            return false;
        }

        /// <summary>
        /// Save this COnfig into JSON.
        /// </summary>
        /// <param name="pretty">If true, then the json data will be formatted for readability.</param>
        /// <returns></returns>
        public string SaveConfig(bool pretty = true)
        {
            fsData data = null;
            sm_Serializer.TrySerialize(GetType(), this, out data);

            if (!System.IO.Directory.Exists(Application.streamingAssetsPath))
                System.IO.Directory.CreateDirectory(Application.streamingAssetsPath);

            if (pretty)
                return fsJsonPrinter.PrettyJson(data);

            return fsJsonPrinter.CompressedJson(data);
        }

        /// <summary>
        /// Finds a variable name and returns the Variable object
        /// </summary>
        /// <param name="key">The name of the variable to find.</param>
        /// <returns>Returns the Variable object or null if not found.</returns>
        public Variable GetVariable(string key)
        {
            foreach (var var in m_Variables)
                if (var.Key == key)
                    return var;

            return null;
        }

        /// <summary>
        /// Gets the variable value.
        /// </summary>
        /// <returns>The variable value.</returns>
        /// <param name="key">Key.</param>
        public string GetVariableValue(string key)
        {
            Variable v = GetVariable(key);
            if (v != null)
                return v.Value;

            return null;
        }

        /// <summary>
        /// Sets the variable value.
        /// </summary>
        /// <returns><c>true</c>, if variable value was set, <c>false</c> otherwise.</returns>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public bool SetVariableValue(string key, string value, bool bAdd = false)
        {
            Variable v = GetVariable(key);
            if (v == null)
            {
                if (!bAdd)
                    return false;

                v = new Variable();
                v.Key = key;
                m_Variables.Add(v);
            }

            v.Value = value;
            return true;
        }

        /// <summary>
        /// Resolves any variables found in the input string and returns the variable values in the returned string.
        /// </summary>
        /// <param name="input">A string containing variables.</param>
        /// <returns>Returns the string with all variables resolved to their actual values. Any missing variables are removed from the string.</returns>
        public string ResolveVariables(string input, bool recursive = true)
        {
            string output = input;
            foreach (var var in m_Variables)
            {
                string value = var.Value;
                if (recursive && value.Contains("${"))
                    value = ResolveVariables(value, false);

                output = output.Replace("${" + var.Key + "}", value);
            }

            // remove any variables still in the string..
            int variableIndex = output.IndexOf("${");
            while (variableIndex >= 0)
            {
                int endVariable = output.IndexOf("}", variableIndex);
                if (endVariable < 0)
                    break;      // end not found..

                output = output.Remove(variableIndex, (endVariable - variableIndex) + 1);

                // next..
                variableIndex = output.IndexOf("${");
            }

            return output;
        }

        private IEnumerator LoadConfigCR()
        {
#if UNITY_IOS
            // load the config using WWW, since this works on all platforms..
            WWW request = new WWW(Application.streamingAssetsPath + Constants.Path.CONFIG_FILE);
            while (!request.isDone)
            yield return null;
            LoadConfig(request.text);
#else
            // load the config using WWWProxy as most MS devices will need proxys
            BestHTTP.HTTPRequest request = new BestHTTP.HTTPRequest(new Uri(Application.streamingAssetsPath + Constants.Path.CONFIG_FILE));
            
            // if a proxy has been detected
            if (Config.Instance.ActiveProxy)
            {
                if (!(Application.streamingAssetsPath + Constants.Path.CONFIG_FILE).Contains("cs.woodside.com.au"))
                {
                    // use a URI builder to generate the proxy uri
                    UriBuilder proxyUriBuilder = new UriBuilder();
                    proxyUriBuilder.Host = "proxy.wde.woodside.com.au";
                    proxyUriBuilder.Port = 8080;
                    proxyUriBuilder.Scheme = "http";
                    request.Proxy = new BestHTTP.HTTPProxy(proxyUriBuilder.Uri);

                    BestHTTP.HTTPManager.Logger.Level = BestHTTP.Logger.Loglevels.All;
                }
            }

            request.Send();

            // runs the request in a co-routine
            Runnable.Run(request);

            while (request.State == BestHTTP.HTTPRequestStates.Processing)
            {
                yield return null;
            }

            // passes the responce
            LoadConfig(request.Response.DataAsText);
#endif
            yield break;
        }

		/// <summary>
		/// Merges the content of current config with a new content passed into the method. New content overrides old one.
		/// </summary>
		/// <param name="newConfig">New config content.</param>
		public void MergeConfigs(string newConfig)
		{
			// Store current credentials and variables
			List<CredentialInfo> savedCredentials = Credentials;
			Credentials = null;
			List<Variable> savedVariables = Variables;
			Variables = null;

			LoadConfig(newConfig);

			// Merge previous credentials into the new config..
			List<CredentialInfo> creds = Credentials;
			if (creds != null && creds.Count != 0)
			{
				foreach (var cred in savedCredentials)
				{
					bool bFound = false;
					for (int i = 0; i < creds.Count && !bFound; ++i)
						if (creds [i].m_ServiceID == cred.m_ServiceID)
							bFound = true;

					if (!bFound)
						creds.Add(cred);
				}
			}
			else
				Credentials = savedCredentials;

			// Merge previous variables into the new config..
			List<Variable> vars = Variables;
			if (vars != null && vars.Count != 0)
			{
				foreach (var var in savedVariables)
				{
					bool bFound = false;
					for (int i = 0; i < vars.Count && !bFound; ++i)
						if (vars [i].Key == var.Key)
							bFound = true;

					if (!bFound)
						vars.Add(var);
				}
			}
			else
				Variables = savedVariables;
		}
    }
}
