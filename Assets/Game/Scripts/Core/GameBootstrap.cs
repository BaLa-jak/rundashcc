using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace RunDashC
{
    /// <summary>
    /// Antes de cargar la primera escena crea "[RunDashC]" (persistente) con AudioManager,
    /// EventSystem y ScreenFader. Por eso las escenas NO deben tener su propio EventSystem.
    /// </summary>
    static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            var root = new GameObject("[RunDashC]");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<AudioManager>();

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(root.transform, false);

            ScreenFader.Create(root.transform);
        }
    }
}
