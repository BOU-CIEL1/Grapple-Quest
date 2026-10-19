using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class CheckpointTests
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
    private const string PrefabPath = "Assets/Imports/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - Road Sign 01.prefab";
    private readonly List<GameObject> objects = new List<GameObject>();
    private SimulationMode2D previousSimulation;
    private float previousTimeScale;

    private static Type Script(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Component target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Instance).Invoke(target, args);

    [SetUp]
    public void SetUp()
    {
        previousSimulation = Physics2D.simulationMode;
        previousTimeScale = Time.timeScale;
        Physics2D.simulationMode = SimulationMode2D.Script;
        Time.timeScale = 1f;
        Script("Player").GetMethod("ReinitialiserCheckpoint", Static).Invoke(null, null);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject obj in objects)
            if (obj != null) Object.DestroyImmediate(obj);
        objects.Clear();
        Script("Player").GetMethod("ReinitialiserCheckpoint", Static).Invoke(null, null);
        Physics2D.simulationMode = previousSimulation;
        Time.timeScale = previousTimeScale;
    }

    private GameObject Create(string name, Vector3 position)
    {
        GameObject obj = new GameObject(name);
        obj.SetActive(false);
        obj.transform.position = position;
        objects.Add(obj);
        return obj;
    }

    private Component CreatePlayer(Vector3 position)
    {
        GameObject obj = Create("Checkpoint test player", position);
        obj.AddComponent<Rigidbody2D>().gravityScale = 0f;
        obj.AddComponent<CapsuleCollider2D>().size = new Vector2(0.5f, 1f);
        Component player = obj.AddComponent(Script("Player"));
        obj.SetActive(true);
        return player;
    }

    private GameObject CreateSign()
    {
#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        GameObject sign = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
#else
        GameObject sign = new GameObject("Checkpoint test sign");
        sign.AddComponent<BoxCollider2D>().size = new Vector2(2f, 3f);
        sign.AddComponent(Script("Checkpoint"));
#endif
        objects.Add(sign);
        Assert.That(sign.GetComponent(Script("Checkpoint")), Is.Not.Null);
        Assert.That(sign.GetComponent<BoxCollider2D>().isTrigger, Is.True);
        return sign;
    }

    private static void Simulate(int steps)
    {
        Physics2D.SyncTransforms();
        for (int i = 0; i < steps; i++) Physics2D.Simulate(Time.fixedDeltaTime);
    }

    [Test]
    public void PassingTheSignActivatesCheckpointWithoutBlockingThePlayer()
    {
        Component player = CreatePlayer(new Vector3(-3f, 1f, 0f));
        GameObject sign = CreateSign();
        player.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(10f, 0f);
        Simulate(30);
        Assert.That(Call(player, "EstCheckpointActif", Vector3.up), Is.True);
        Assert.That(player.transform.position.x, Is.GreaterThan(2f));
        Assert.That(sign.transform.Find("Checkpoint !"), Is.Not.Null);
    }

    [Test]
    public void MultipleCollidersAndRepeatedPassesCreateOnlyOneLabel()
    {
        Component player = CreatePlayer(Vector3.up);
        GameObject child = new GameObject("Extra player collider", typeof(BoxCollider2D));
        child.transform.SetParent(player.transform, false);
        GameObject sign = CreateSign();
        int initialChildren = sign.transform.childCount;
        Simulate(3);
        player.GetComponent<Rigidbody2D>().position = new Vector2(-4f, 1f);
        Simulate(1);
        player.GetComponent<Rigidbody2D>().position = Vector2.up;
        Simulate(3);
        Assert.That(sign.transform.childCount, Is.EqualTo(initialChildren + 1));
    }

    [Test]
    public void NonPlayerAndDeadPlayerCannotActivateCheckpoint()
    {
        GameObject sign = CreateSign();
        GameObject enemy = Create("Non player", Vector3.up);
        enemy.AddComponent<Rigidbody2D>().gravityScale = 0f;
        enemy.AddComponent<BoxCollider2D>();
        enemy.SetActive(true);
        Simulate(3);
        Assert.That(sign.transform.Find("Checkpoint !"), Is.Null);

        Component player = CreatePlayer(new Vector3(-4f, 1f, 0f));
        Call(player, "Tuer");
        Assert.That(Call(player, "ActiverCheckpoint", Vector3.up), Is.False);
        Assert.That(Call(player, "EstCheckpointActif", Vector3.up), Is.False);
    }

    private static void PrepareReload(Vector3 position, string scenePath)
    {
        Script("Player").GetField("checkpointAuRechargement", Static).SetValue(null, (Vector3?)position);
        Script("Player").GetField("sceneAuRechargement", Static).SetValue(null, scenePath);
    }

    [Test]
    public void ReloadRestoresFullHealthPositionCameraAndCheckpointLabel()
    {
        Vector3 respawn = new Vector3(35f, 14f, 0f);
        GameObject placeholder = Create("Scene reference", Vector3.zero);
        PrepareReload(respawn, placeholder.scene.path);
        Component player = CreatePlayer(Vector3.zero);
        Assert.That(player.transform.position, Is.EqualTo(respawn));
        Assert.That(player.GetComponent<Rigidbody2D>().position, Is.EqualTo((Vector2)respawn));
        Assert.That(player.GetType().GetProperty("PvActuels").GetValue(player), Is.EqualTo(3));
        Assert.That(Call(player, "EstCheckpointActif", respawn), Is.True);

        GameObject cameraObject = Create("Checkpoint test camera", new Vector3(0f, 0f, -10f));
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.83f;
        camera.aspect = 16f / 9f;
        Component follow = cameraObject.AddComponent(Script("CameraSuivi"));
        follow.GetType().GetField("joueur", Instance).SetValue(follow, player.transform);
        cameraObject.SetActive(true);
        Assert.That(camera.WorldToViewportPoint(respawn).x, Is.EqualTo(0.33f).Within(0.001f));

        GameObject sign = CreateSign();
        sign.transform.position = respawn - Vector3.up;
        Call(sign.GetComponent(Script("Checkpoint")), "Start");
        Assert.That(sign.transform.Find("Checkpoint !"), Is.Not.Null);

        // Le transfert est consomme : une nouvelle partie repart de son point initial.
        Component freshPlayer = CreatePlayer(new Vector3(-11f, 1f, 0f));
        Assert.That(freshPlayer.transform.position, Is.EqualTo(new Vector3(-11f, 1f, 0f)));
        Assert.That(Call(freshPlayer, "EstCheckpointActif", respawn), Is.False);
    }

    [Test]
    public void CheckpointFromAnotherSceneDoesNotMoveThePlayer()
    {
        PrepareReload(new Vector3(35f, 14f, 0f), "Assets/Scenes/AnotherLevel.unity");
        Vector3 start = new Vector3(-11f, 1f, 0f);
        Component player = CreatePlayer(start);
        Assert.That(player.transform.position, Is.EqualTo(start));
        Assert.That(Call(player, "EstCheckpointActif", new Vector3(35f, 14f, 0f)), Is.False);
    }
}
