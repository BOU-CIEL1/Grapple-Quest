using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class LandingPhysicsTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private readonly List<GameObject> objects = new List<GameObject>();
    private Scene scene;
    private PhysicsScene2D physics;

    [SetUp]
    public void SetUp()
    {
        scene = SceneManager.CreateScene("Landing physics tests",
            new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        physics = scene.GetPhysicsScene2D();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (GameObject item in objects) Object.DestroyImmediate(item);
        objects.Clear();
        yield return SceneManager.UnloadSceneAsync(scene);
    }

    private GameObject Create(string name, Vector2 position)
    {
        GameObject item = new GameObject(name);
        item.SetActive(false);
        SceneManager.MoveGameObjectToScene(item, scene);
        item.transform.position = position;
        objects.Add(item);
        return item;
    }

    private static Component AddScript(GameObject item, string name) =>
        item.AddComponent(Type.GetType(name + ", Assembly-CSharp", true));

    private void Step(Component script)
    {
        script.GetType().GetMethod("FixedUpdate", Flags).Invoke(script, null);
        physics.Simulate(Time.fixedDeltaTime);
    }

    [Test]
    public void MovingPlatformStaysWithinEndpointsAndWaitsWithoutDrifting()
    {
        GameObject item = Create("Moving platform", new Vector2(0f, 4f));
        Rigidbody2D body = item.AddComponent<Rigidbody2D>();
        item.AddComponent<BoxCollider2D>();
        Component script = AddScript(item, "UpDownPlatform");
        script.GetType().GetField("distance", Flags).SetValue(script, 3.01f);
        script.GetType().GetField("attente", Flags).SetValue(script, 0.1f);
        item.SetActive(true);
        bool reachedBottom = false;
        bool returnedToTop = false;
        int stationarySteps = 0;
        for (int i = 0; i < 3000; i++)
        {
            float previousY = body.position.y;
            Step(script);
            float y = body.position.y;
            Assert.That(y, Is.InRange(0.9899f, 4.0001f));
            if (Mathf.Abs(y - 0.99f) < 0.0001f) reachedBottom = true;
            if (reachedBottom && Mathf.Abs(y - 4f) < 0.0001f) returnedToTop = true;
            if (Mathf.Abs(y - previousY) < 0.0001f && Mathf.Abs(body.linearVelocity.y) < 0.0001f)
                stationarySteps++;
        }
        Assert.That(reachedBottom && returnedToTop, Is.True);
        Assert.That(stationarySteps, Is.GreaterThan(100));
    }

    [Test]
    public void DisablingMovingPlatformStopsItsRigidbody()
    {
        GameObject item = Create("Moving platform", Vector2.zero);
        Rigidbody2D body = item.AddComponent<Rigidbody2D>();
        Component script = AddScript(item, "UpDownPlatform");
        item.SetActive(true);
        Step(script);
        Vector2 position = body.position;
        ((Behaviour)script).enabled = false;
        for (int i = 0; i < 10; i++) physics.Simulate(Time.fixedDeltaTime);
        Assert.That(Vector2.Distance(body.position, position), Is.LessThan(0.0001f));
    }

    [TestCase(0f)]
    [TestCase(1f)]
    public void PlayerLandsWithoutRisingAgainOnFlatGround(float direction)
    {
        GameObject floor = Create("Flat ground", new Vector2(0f, -0.5f));
        floor.AddComponent<BoxCollider2D>().size = new Vector2(200f, 1f);
        floor.SetActive(true);
        GameObject player = Create("Landing test player", new Vector2(-30f, 4f));
        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        player.AddComponent<CapsuleCollider2D>().size = new Vector2(0.8f, 1.6f);
        Component movement = AddScript(player, "Mouvements");
        player.SetActive(true);
        movement.GetType().GetField("direction", Flags).SetValue(movement, direction);
        bool landed = false;
        float landingY = 0f;
        for (int i = 0; i < 180; i++)
        {
            Step(movement);
            if (!landed && body.IsTouching(floor.GetComponent<Collider2D>()))
            {
                landed = true;
                landingY = body.position.y;
            }
            if (landed)
            {
                Assert.That(body.position.y - landingY, Is.LessThan(0.02f));
                Assert.That(body.linearVelocity.y, Is.LessThan(0.02f));
            }
        }
        Assert.That(landed, Is.True);
        Assert.That(body.position.y, Is.InRange(0.78f, 0.83f));
    }
}
