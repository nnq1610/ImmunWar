using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmunWar.Core
{
    public static class SceneFlowService
    {
        public static event Action<string, float> ProgressChanged;
        public static event Action<string> LoadFailed;

        public static IEnumerator Load(string sceneName)
        {
            AsyncOperation operation;
            try { operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); }
            catch (Exception exception) { LoadFailed?.Invoke(exception.Message); yield break; }
            if (operation == null) { LoadFailed?.Invoke("Scene load returned no operation: " + sceneName); yield break; }
            while (!operation.isDone)
            {
                ProgressChanged?.Invoke(sceneName, operation.progress);
                yield return null;
            }
            ProgressChanged?.Invoke(sceneName, 1f);
        }
    }
}
