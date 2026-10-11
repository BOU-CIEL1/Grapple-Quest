using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class BigRockTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<GameObject> objects = new List<GameObject>();
    private SimulationMode2D previousSimulation;
    private float previousTimeScale;
    private GameObject rock;
    private Component rockScript;

    private static Type Script(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static void Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Flags).Invoke(target, args);
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, Flags).SetValue(target, value);
    private static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);

    [SetUp]
    public void SetUp()
    {
        previousSimulation = Physics2D.simulationMode;
        previousTimeScale = Time.timeScale;
        Physics2D.simulationMode = SimulationMode2D.Script;
        Time.timeScale = 1f;
#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BigRock.prefab");
        Assert.That(prefab, Is.Not.Null);
        rock = Object.Instantiate(prefab, new Vector3(5f, -1.92f, 0f), prefab.transform.rotation);
#else
        rock = new GameObject("BigRock");
        rock.SetActive(false);
        rock.transform.position = new Vector3(5f, 0f, 0f);
        GameObject visual = new GameObject("Visuel", typeof(SpriteRenderer));
        visual.transform.SetParent(rock.transform, false);
        Component component = rock.AddComponent(Script("BigRock"));
        Set(component, "visuel", visual.transform);
        rock.SetActive(true);
#endif
        objects.Add(rock);
        rockScript = rock.GetComponent(Script("BigRock"));
        Assert.That(rockScript, Is.Not.Null);
        Physics2D.SyncTransforms();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject obj in objects)
            if (obj != null) Object.DestroyImmediate(obj);
        objects.Clear();
        Physics2D.simulationMode = previousSimulation;
        Time.timeScale = previousTimeScale;
    }

    private Component CreateGrapple()
    {
        GameObject player = new GameObject("Rock test player");
        objects.Add(player);
        player.SetActive(false);
        player.AddComponent<SpriteRenderer>();
        player.AddComponent<Rigidbody2D>();
        player.AddComponent<CapsuleCollider2D>().size = new Vector2(0.4f, 0.8f);
        player.AddComponent(Script("Mouvements"));
        Component grapple = player.AddComponent(Script("Grapple"));
        Set(grapple, "positionMain", new Vector2(0.25f, 0f));
        player.AddComponent(Script("Player"));
        player.SetActive(true);
        Physics2D.SyncTransforms();
        return grapple;
    }

    private void Fire(Component grapple)
    {
        Call(grapple, "LancerVers", (Vector2)rock.GetComponent<Collider2D>().bounds.center);
        for (int i = 0; i < 80 && Property(grapple, "Etat").ToString() == "Lance"; i++)
            Call(grapple, "FixedUpdate");
    }

    [Test]
    public void RockRemainsFixedUnderGravityAndPlayerCollision()
    {
        Vector3 position = rock.transform.position;
        Quaternion rotation = rock.transform.rotation;
        Assert.That(rock.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Static));
        Component grapple = CreateGrapple();
        Rigidbody2D player = grapple.GetComponent<Rigidbody2D>();
        player.gravityScale = 0f;
        player.linearVelocity = Vector2.right * 10f;
        for (int i = 0; i < 100; i++) Physics2D.Simulate(Time.fixedDeltaTime);
        Assert.That(rock.transform.position, Is.EqualTo(position));
        Assert.That(Quaternion.Angle(rock.transform.rotation, rotation), Is.LessThan(0.001f));
        Assert.That(player.position.x, Is.LessThan(rock.GetComponent<Collider2D>().bounds.min.x));
    }

    [Test]
    public void ThreeSeparateGrappleHitsDestroyRockWithoutAttaching()
    {
        Component grapple = CreateGrapple();
        for (int hit = 1; hit <= 3; hit++)
        {
            Fire(grapple);
            Assert.That(Property(rockScript, "CoupsRecus"), Is.EqualTo(hit));
            Assert.That(Property(grapple, "Etat").ToString(), Is.EqualTo("Retour"));
            Assert.That(grapple.GetComponent<DistanceJoint2D>().enabled, Is.False);
            Assert.That(rock.activeSelf, Is.EqualTo(hit < 3));
            if (hit < 3)
            {
                for (int i = 0; i < 80 && Property(grapple, "Etat").ToString() != "EnMain"; i++)
                    Call(grapple, "FixedUpdate");
                Assert.That(Property(rockScript, "CoupsRecus"), Is.EqualTo(hit));
            }
        }
        Assert.That(rock.GetComponent<Collider2D>().enabled, Is.False);
    }

    [Test]
    public void WallProtectsRockFromGrappleHits()
    {
        GameObject wall = new GameObject("Rock test wall");
        objects.Add(wall);
        wall.transform.position = new Vector3(1.5f, 0f, 0f);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 5f);
        Physics2D.SyncTransforms();
        Component grapple = CreateGrapple();
        Fire(grapple);
        Assert.That(Property(rockScript, "CoupsRecus"), Is.EqualTo(0));
        Assert.That(Property(grapple, "Etat").ToString(), Is.EqualTo("Retour"));
    }

    [UnityTest]
    public IEnumerator ShakeMovesOnlyVisualAndReturnsItToRest()
    {
        Transform visual = rock.GetComponentInChildren<SpriteRenderer>().transform;
        Vector3 rest = visual.localPosition;
        Vector3 rootPosition = rock.transform.position;
        Vector3 colliderCenter = rock.GetComponent<Collider2D>().bounds.center;
        Call(rockScript, "RecevoirCoupGrappin");
        Assert.That(visual.localPosition, Is.Not.EqualTo(rest));
        Assert.That(Vector3.Distance(visual.position, rock.transform.TransformPoint(rest)), Is.LessThan(0.04f));
        Assert.That(rock.transform.position, Is.EqualTo(rootPosition));
        Physics2D.SyncTransforms();
        Assert.That(rock.GetComponent<Collider2D>().bounds.center, Is.EqualTo(colliderCenter));
        yield return new WaitForSeconds(0.25f);
        Assert.That(visual.localPosition, Is.EqualTo(rest));
        Assert.That(rock.transform.position, Is.EqualTo(rootPosition));
    }
}
