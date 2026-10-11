using System.Reflection;
using NUnit.Framework;
using TinyTrouble.EditorTools;
using UnityEditor;
using UnityEngine;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет изображение после замены формы в уже сохранённом ассете.</summary>
    public class CosmeticMeshUploadTests
    {
        /// <summary>Новая сетка должна целиком отображаться сразу, даже если у старой было другое число точек.</summary>
        [Test] public void ReplacingSavedTopologyUpdatesRenderedSurface()
        {
            const string id = "__mesh-upload-test";
            var path = CosmeticAssetBuilder.Root + "/Meshes/" + id + ".asset";
            var root = new GameObject("MeshReplacement", typeof(MeshFilter));
            var save = typeof(CosmeticAssetBuilder).GetMethod("SaveMeshes", BindingFlags.Static | BindingFlags.NonPublic);
            Mesh fresh = null;
            try
            {
                var filter = root.GetComponent<MeshFilter>();
                filter.sharedMesh = CosmeticMeshBuilder.RoundedBox(new Vector3(.8f, .8f, .2f), .04f); filter.sharedMesh.name = "Grip";
                save.Invoke(null, new object[] { id, root });
                CountPixels(filter.sharedMesh);
                fresh = CosmeticMeshBuilder.Extrude(new[] { new Vector2(-.3f, -.4f), new Vector2(.3f, -.4f), new Vector2(.2f, .4f), new Vector2(-.2f, .4f) }, .2f);
                var expected = CountPixels(fresh);
                filter.sharedMesh = Object.Instantiate(fresh); filter.sharedMesh.name = "Grip";
                save.Invoke(null, new object[] { id, root });
                Assert.That(CountPixels(filter.sharedMesh), Is.EqualTo(expected).Within(2), "Изображение сохранённой новой формы должно совпадать со свежей сеткой");
            }
            finally { Object.DestroyImmediate(root); if (fresh) Object.DestroyImmediate(fresh); AssetDatabase.DeleteAsset(path); }
        }
        /// <summary>Снимает белую поверхность без света; считает видимые точки настоящего изображения.</summary>
        /// <param name="mesh">Проверяемая сетка.</param>
        /// <returns>Число белых точек.</returns>
        static int CountPixels(Mesh mesh)
        {
            var surface = new GameObject("UploadSurface", typeof(MeshFilter), typeof(MeshRenderer));
            var cameraObject = new GameObject("UploadCamera", typeof(Camera));
            var material = new Material(Shader.Find("Unlit/Color")) { color = Color.white };
            var rt = RenderTexture.GetTemporary(64, 64, 24); var before = RenderTexture.active;
            var pixels = new Texture2D(64, 64, TextureFormat.RGB24, false);
            try
            {
                surface.layer = 30; surface.transform.position = new Vector3(2000, 2000, 2000);
                surface.GetComponent<MeshFilter>().sharedMesh = mesh; surface.GetComponent<Renderer>().sharedMaterial = material;
                var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false; camera.cullingMask = 1 << 30;
                camera.orthographic = true; camera.orthographicSize = .5f; camera.aspect = 1;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.targetTexture = rt;
                camera.transform.position = surface.transform.position + Vector3.back * 2; camera.transform.LookAt(surface.transform);
                camera.Render(); RenderTexture.active = rt; pixels.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); pixels.Apply();
                var count = 0; foreach (var pixel in pixels.GetPixels32()) if (pixel.r > 128) count++;
                return count;
            }
            finally
            {
                RenderTexture.active = before; cameraObject.GetComponent<Camera>().targetTexture = null; RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(surface); Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(material); Object.DestroyImmediate(pixels);
            }
        }
    }
}
