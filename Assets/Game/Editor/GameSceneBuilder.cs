using UnityEngine;

namespace RunDashC.EditorTools
{
    /// <summary>
    /// Escena Game: Main Camera con CameraRig, Level (LevelBuilder arma el mapa en runtime), Camarada,
    /// RunManager y Parallax, con las referencias públicas cableadas. Después agrega el HUD.
    /// </summary>
    public static class GameSceneBuilder
    {
        public static void Build()
        {
            var scene = SceneBuilder.NewScene(withCamera: true);

            var cam = Object.FindFirstObjectByType<Camera>();
            cam.transform.position = new Vector3(0f, 6f, -10f);
            var rig = cam.gameObject.AddComponent<CameraRig>();

            var level = new GameObject("Level").AddComponent<LevelBuilder>();

            // RequireComponent agrega Rigidbody2D y BoxCollider2D; CamaradaController.Awake los configura.
            var player = new GameObject("Camarada").AddComponent<CamaradaController>();
            var visual = new GameObject("Visual", typeof(SpriteRenderer));
            visual.transform.SetParent(player.transform, false);
            var sprite = visual.GetComponent<SpriteRenderer>();
            sprite.sprite = SceneBuilder.LoadResource<Sprite>("Art/camarada");
            sprite.sortingOrder = 20;
            player.fx = visual.AddComponent<CamaradaFx>();
            rig.target = player.transform;

            var parallax = new GameObject("Parallax").AddComponent<ParallaxLoop>();
            parallax.cameraTransform = cam.transform;

            var run = new GameObject("RunManager").AddComponent<RunManager>();
            run.level = level;
            run.player = player;
            run.cameraRig = rig;

            HudBuilder.Build();
            SceneBuilder.Save(scene, SceneNames.Game);
        }
    }
}
