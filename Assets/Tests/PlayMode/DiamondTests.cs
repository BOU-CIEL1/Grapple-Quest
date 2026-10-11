using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class DiamondTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private Rigidbody2D playerBody;
    private Component inventory;
    private SimulationMode2D previousSimulation;

    private static Type Script(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static int Count(Component component, string property) =>
        (int)component.GetType().GetProperty(property).GetValue(component);
    private Text DiamondCounter => (Text)inventory.GetType()
        .GetField("diamondCounter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(inventory);

    private Rigidbody2D CreateMover(string name, Vector2 position)
    {
        GameObject mover = new GameObject(name);
        objects.Add(mover);
        mover.transform.position = position;
        Rigidbody2D body = mover.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        mover.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.8f);
        return body;
    }

    [SetUp]
    public void SetUp()
    {
        previousSimulation = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;
        playerBody = CreateMover("Diamond test player", new Vector2(-2f, 0f));
        inventory = playerBody.gameObject.AddComponent(Script("PlayerCoins"));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject obj in objects)
            if (obj != null) Object.DestroyImmediate(obj);
        objects.Clear();
        Physics2D.simulationMode = previousSimulation;
    }

    private GameObject CreateDiamond(Vector2 position)
    {
#if UNITY_EDITOR
        // Utilise le vrai prefab, pour verifier egalement son script et son collider.
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Diamond.prefab");
        Assert.That(prefab, Is.Not.Null);
        GameObject diamond = Object.Instantiate(prefab, position, Quaternion.identity);
#else
        GameObject diamond = new GameObject("Diamond");
        diamond.transform.position = position;
        diamond.AddComponent(Script("CollectDiamond"));
#endif
        objects.Add(diamond);
        Assert.That(diamond.GetComponent(Script("CollectDiamond")), Is.Not.Null);
        Assert.That(diamond.GetComponent<Collider2D>().isTrigger, Is.True);
        return diamond;
    }

    private static void Simulate(int steps)
    {
        Physics2D.SyncTransforms();
        for (int i = 0; i < steps; i++) Physics2D.Simulate(Time.fixedDeltaTime);
    }

    [Test]
    public void PlayerCollectsDiamondAndPassesThroughIt()
    {
        GameObject diamond = CreateDiamond(Vector2.zero);
        Assert.That(DiamondCounter.text, Is.EqualTo("Diamonds: 0 / 3"));
        playerBody.linearVelocity = new Vector2(10f, 0f);
        Simulate(30);
        Assert.That(Count(inventory, "Diamonds"), Is.EqualTo(1));
        Assert.That(DiamondCounter.text, Is.EqualTo("Diamonds: 1 / 3"));
        Assert.That(diamond.activeSelf, Is.False);
        Assert.That(playerBody.position.x, Is.GreaterThan(3f));
        Assert.That(playerBody.linearVelocity.x, Is.EqualTo(10f).Within(0.001f));
    }

    [Test]
    public void ThreeDistinctDiamondsCompleteTheCounter()
    {
        CreateDiamond(Vector2.zero);
        CreateDiamond(new Vector2(3f, 0f));
        CreateDiamond(new Vector2(6f, 0f));
        playerBody.linearVelocity = new Vector2(10f, 0f);
        Simulate(50);
        Assert.That(Count(inventory, "Diamonds"), Is.EqualTo(3));
        Assert.That(DiamondCounter.text, Is.EqualTo("Diamonds: 3 / 3"));
        Assert.That(Count(inventory, "Coins"), Is.EqualTo(0));
        Assert.That(DiamondCounter.raycastTarget, Is.False);
    }

    [Test]
    public void MultiplePlayerCollidersDoNotCollectTheSameDiamondTwice()
    {
        GameObject extraCollider = new GameObject("Player child collider");
        extraCollider.transform.SetParent(playerBody.transform, false);
        extraCollider.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 0.9f);
        CreateDiamond(Vector2.zero);
        playerBody.linearVelocity = new Vector2(10f, 0f);
        Simulate(30);
        Assert.That(Count(inventory, "Diamonds"), Is.EqualTo(1));
    }

    [Test]
    public void NonPlayerCannotCollectDiamond()
    {
        playerBody.position = new Vector2(-10f, 0f);
        GameObject diamond = CreateDiamond(Vector2.zero);
        Rigidbody2D enemy = CreateMover("Non player", new Vector2(-2f, 0f));
        enemy.linearVelocity = new Vector2(10f, 0f);
        Simulate(30);
        Assert.That(diamond.activeSelf, Is.True);
        Assert.That(Count(inventory, "Diamonds"), Is.EqualTo(0));
    }

    [Test]
    public void CoinsAndDiamondsHaveSeparateCounters()
    {
        GameObject coin = new GameObject("Test coin");
        objects.Add(coin);
        coin.transform.position = new Vector2(-0.5f, 0f);
        coin.AddComponent(Script("CollectCoin"));
        CreateDiamond(new Vector2(3f, 0f));
        playerBody.linearVelocity = new Vector2(10f, 0f);
        Simulate(40);
        Assert.That(Count(inventory, "Coins"), Is.EqualTo(1));
        Assert.That(Count(inventory, "Diamonds"), Is.EqualTo(1));
        Assert.That(DiamondCounter.text, Is.EqualTo("Diamonds: 1 / 3"));
    }
}
