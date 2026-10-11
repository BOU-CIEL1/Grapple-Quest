#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class BossTests
{
    private readonly List<Object> objects = new List<Object>();
    private SimulationMode2D previousSimulation;
    private float previousTimeScale;
    private Component boss;
    private Animator animator;
    private GameObject bossObject;
    private static Type Script(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static void Call(Component component, string method, params object[] arguments) =>
        component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Invoke(component, arguments);

    private GameObject Create(string name)
    {
        GameObject result = new GameObject(name);
        result.SetActive(false);
        objects.Add(result);
        return result;
    }

    [SetUp]
    public void SetUp()
    {
        previousSimulation = Physics2D.simulationMode;
        previousTimeScale = Time.timeScale;
        Physics2D.simulationMode = SimulationMode2D.Script;
        Time.timeScale = 1f;
        bossObject = Create("Boss test");
        bossObject.AddComponent<Rigidbody2D>();
        bossObject.AddComponent<BoxCollider2D>().size = Vector2.one * 2f;
        bossObject.AddComponent(Script("BossCollision"));
        animator = bossObject.AddComponent<Animator>();
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        AnimatorController controller = new AnimatorController();
        controller.AddLayer("Base Layer");
        objects.Add(controller);
        objects.Add(controller.layers[0].stateMachine);
        foreach (string name in new[] { "Idle", "CastSpell", "Floating", "Teleport", "FinishSpell",
            "HandAttack", "Boomerang", "Explosion", "Summon", "Death" })
        {
            AnimationClip clip = new AnimationClip { name = name };
            clip.SetCurve("", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 1f, 0.08f, 1f));
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            objects.Add(clip);
            objects.Add(controller.AddMotion(clip));
        }
        animator.runtimeAnimatorController = controller;
        boss = bossObject.AddComponent(Script("Boss"));
        bossObject.SetActive(true);
        animator.Update(0f);
    }

    [TearDown]
    public void TearDown()
    {
        // Destroy scene objects before their animation assets.
        foreach (Object item in objects)
            if (item is GameObject && item != null) Object.DestroyImmediate(item);
        foreach (Object item in objects)
            if (item != null) Object.DestroyImmediate(item);
        objects.Clear();
        Physics2D.simulationMode = previousSimulation;
        Time.timeScale = previousTimeScale;
    }

    [UnityTest]
    public IEnumerator FatalHitInterruptsAttackAndPlaysDeathOnce()
    {
        animator.Play("Base Layer.HandAttack", 0, 0f);
        animator.Update(0f);
        Call(boss, "PrendreDegats", 100);
        yield return null;
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"), Is.True);
        Assert.That(bossObject.GetComponent<Rigidbody2D>().simulated, Is.False);
        foreach (Collider2D collider in bossObject.GetComponents<Collider2D>())
            Assert.That(collider.enabled, Is.False);
        yield return new WaitForSeconds(0.25f);
        Assert.That(animator.speed, Is.Zero);
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"), Is.True);
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(1f).Within(0.01f));
        Call(boss, "PrendreDegats", 6);
        Assert.That((int)boss.GetType().GetProperty("PvActuels").GetValue(boss), Is.Zero);
    }

    [UnityTest]
    public IEnumerator PlayerCrossesBossAndTakesContactDamageWhileBossStaysOnFloor()
    {
        GameObject floor = Create("Floor test");
        floor.transform.position = new Vector3(0f, -1.5f, 0f);
        floor.AddComponent<BoxCollider2D>().size = new Vector2(20f, 1f);
        floor.SetActive(true);
        GameObject player = Create("Player test");
        player.transform.position = new Vector3(-3f, 0f, 0f);
        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        player.AddComponent<CapsuleCollider2D>().size = new Vector2(0.5f, 0.8f);
        Component health = player.AddComponent(Script("Player"));
        player.SetActive(true);
        yield return null; // Start configures collision pairs after both actors exist.
        body.linearVelocity = Vector2.right * 4f;
        Physics2D.SyncTransforms();
        for (int i = 0; i < 100; i++) Physics2D.Simulate(0.02f);
        Assert.That(body.position.x, Is.GreaterThan(3f));
        Assert.That((int)health.GetType().GetProperty("PvActuels").GetValue(health), Is.EqualTo(2));
        Assert.That(bossObject.GetComponent<Rigidbody2D>().position.y, Is.GreaterThan(-0.1f));
    }
}
#endif
