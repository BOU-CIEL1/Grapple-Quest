using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class CameraSuiviTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject player;
    private GameObject cameraObject;
    private Camera camera;
    private Component follow;

    private void Create(Vector3 start)
    {
        player = new GameObject("Camera test player");
        player.transform.position = start;
        cameraObject = new GameObject("Camera test");
        cameraObject.SetActive(false);
        cameraObject.transform.position = new Vector3(-6f, 1.08f, -10f);
        camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.83f;
        camera.aspect = 16f / 9f;
        follow = cameraObject.AddComponent(Type.GetType("CameraSuivi, Assembly-CSharp", true));
        follow.GetType().GetField("joueur", Flags).SetValue(follow, player.transform);
        cameraObject.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        if (cameraObject != null) Object.DestroyImmediate(cameraObject);
        if (player != null) Object.DestroyImmediate(player);
    }

    [TestCase(70f, 14f)]
    [TestCase(35f, 3f)]
    public void RelocatedStartIsFramedImmediatelyAndRemainsFramed(float x, float y)
    {
        Create(new Vector3(x, y, 0f));
        Vector3 viewport = camera.WorldToViewportPoint(player.transform.position);
        Assert.That(viewport.x, Is.EqualTo(0.33f).Within(0.001f));
        Assert.That(viewport.y, Is.InRange(0.45f, 0.55f));
        Assert.That(camera.transform.position.y, Is.EqualTo(y + 0.21f).Within(0.001f));
        Assert.That(camera.transform.position.z, Is.EqualTo(-10f));
        Vector3 initial = camera.transform.position;
        follow.GetType().GetMethod("LateUpdate", Flags).Invoke(follow, null);
        Assert.That(Vector3.Distance(camera.transform.position, initial), Is.LessThan(0.001f));
    }

    [Test]
    public void OriginalStartKeepsOriginalFramingAndVerticalDeadZone()
    {
        Create(new Vector3(-11.55f, 0.87f, 0f));
        Assert.That(camera.transform.position.x, Is.EqualTo(-6f).Within(0.001f));
        Assert.That(camera.transform.position.y, Is.EqualTo(1.08f).Within(0.001f));
        player.transform.position += Vector3.up;
        follow.GetType().GetMethod("LateUpdate", Flags).Invoke(follow, null);
        Assert.That(camera.transform.position.y, Is.EqualTo(1.08f).Within(0.001f));
    }

    [Test]
    public void LowStartRespectsCameraBottomLimit()
    {
        Create(new Vector3(35f, -4f, 0f));
        Assert.That(camera.transform.position.y, Is.EqualTo(0.83f).Within(0.001f));
    }
}
