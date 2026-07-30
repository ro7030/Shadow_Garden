using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Cloaked Moa silhouette — larger navy cape readable on cream tiles.
    /// </summary>
    public sealed class PlayerPresenter : MonoBehaviour
    {
        [SerializeField] private Transform playerVisual;

        public void EnsureVisual()
        {
            if (playerVisual != null)
            {
                return;
            }

            var root = new GameObject("Moa");
            root.transform.SetParent(transform, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.46f, 0.34f, 0.46f);
            body.transform.localPosition = new Vector3(0f, 0.08f, -0.28f);
            Object.Destroy(body.GetComponent<Collider>());
            ApplyColor(body, MockupPalette.PlayerCloak);

            var cloak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cloak.name = "Cloak";
            cloak.transform.SetParent(root.transform, false);
            cloak.transform.localScale = new Vector3(0.52f, 0.22f, 0.18f);
            cloak.transform.localPosition = new Vector3(0f, -0.06f, -0.22f);
            cloak.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            Object.Destroy(cloak.GetComponent<Collider>());
            ApplyColor(cloak, MockupPalette.PlayerCloak);

            var hood = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hood.name = "Hood";
            hood.transform.SetParent(root.transform, false);
            hood.transform.localScale = new Vector3(0.34f, 0.28f, 0.34f);
            hood.transform.localPosition = new Vector3(0f, 0.34f, -0.32f);
            Object.Destroy(hood.GetComponent<Collider>());
            ApplyColor(hood, MockupPalette.PlayerHood);

            playerVisual = root.transform;
        }

        public void Render(ShadowGarden.Core.GridPosition position)
        {
            EnsureVisual();
            playerVisual.position = GridWorld.ToWorld(position, -0.38f);
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            renderer.sharedMaterial = new Material(shader) { color = color };
        }
    }
}
