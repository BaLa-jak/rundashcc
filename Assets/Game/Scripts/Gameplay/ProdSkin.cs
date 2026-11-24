using UnityEngine;

namespace RunDashC
{
    /// <summary>
    /// Dos versiones de un visual: "code" (ASCII, lo normal) y "prod" (dimensión producción, colores sin código).
    /// Activa una u otra según <see cref="WorldMode.Production"/>.
    /// </summary>
    public class ProdSkin : MonoBehaviour
    {
        GameObject code, prod;

        /// <summary>
        /// Crea los hijos "code" y "prod" bajo <paramref name="parent"/> (en su origen). Si el nivel no tiene
        /// portales 'D', <paramref name="prodRoot"/> sale null y no hay que construir la piel prod.
        /// </summary>
        public static Transform Split(Transform parent, out Transform prodRoot)
        {
            var codeRoot = Child(parent, "code");
            prodRoot = null;
            if (!WorldMode.HasProduction) return codeRoot;
            prodRoot = Child(parent, "prod");
            var holder = new GameObject("skin").AddComponent<ProdSkin>(); // un holder por par: varios Split en el mismo padre
            holder.transform.SetParent(parent, false);
            holder.code = codeRoot.gameObject;
            holder.prod = prodRoot.gameObject;
            holder.Apply();
            return codeRoot;
        }

        static Transform Child(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        void OnEnable()
        {
            WorldMode.Changed += Apply;
            Apply();
        }

        void OnDisable() => WorldMode.Changed -= Apply;

        void Apply()
        {
            if (code == null || prod == null) return;
            code.SetActive(!WorldMode.Production);
            prod.SetActive(WorldMode.Production);
        }
    }
}
