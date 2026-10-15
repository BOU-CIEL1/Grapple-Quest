using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

// Reflection : les scripts du jeu restent dans Assembly-CSharp sans changer leurs references.
public class GrappleTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<GameObject> objets = new List<GameObject>();
    private Component grappin;
    private Component mouvements;
    private Rigidbody2D corps;
    private SimulationMode2D simulationInitiale;
    private float tempsInitial;
    private Mouse souris;
    private InputTestFixture entrees;

    private static Type Script(string nom) => Type.GetType(nom + ", Assembly-CSharp", true);
    private static object Lire(object cible, string nom) => cible.GetType().GetField(nom, Flags).GetValue(cible);
    private static void Ecrire(object cible, string nom, object valeur) => cible.GetType().GetField(nom, Flags).SetValue(cible, valeur);
    private static object Propriete(object cible, string nom) => cible.GetType().GetProperty(nom, Flags).GetValue(cible);
    private static void Appeler(object cible, string nom, params object[] arguments) => cible.GetType().GetMethod(nom, Flags).Invoke(cible, arguments);
    private string Etat => Propriete(grappin, "Etat").ToString();

    private GameObject Creer(string nom, Vector2 position)
    {
        GameObject objet = new GameObject(nom);
        objet.SetActive(false);
        objet.transform.position = position;
        objets.Add(objet);
        return objet;
    }

    [SetUp]
    public void Preparer()
    {
        entrees = new InputTestFixture();
        entrees.Setup();
        simulationInitiale = Physics2D.simulationMode;
        tempsInitial = Time.timeScale;
        Time.timeScale = 1f;
        Physics2D.simulationMode = SimulationMode2D.Script;
        GameObject joueur = Creer("Joueur test", Vector2.zero);
        joueur.AddComponent<SpriteRenderer>();
        corps = joueur.AddComponent<Rigidbody2D>();
        joueur.AddComponent<CapsuleCollider2D>().size = new Vector2(0.4f, 0.8f);
        mouvements = joueur.AddComponent(Script("Mouvements"));
        grappin = joueur.AddComponent(Script("Grapple"));
        joueur.AddComponent(Script("Player"));
        Ecrire(grappin, "positionMain", new Vector2(0.25f, 0f));
#if UNITY_EDITOR
        Ecrire(grappin, "animationGrappin", UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animation/Grapple.controller"));
#endif
        joueur.SetActive(true);
        Physics2D.SyncTransforms();
    }

    [TearDown]
    public void Nettoyer()
    {
        if (souris != null) InputSystem.RemoveDevice(souris);
        foreach (GameObject objet in objets)
            if (objet != null) Object.DestroyImmediate(objet);
        objets.Clear();
        entrees.TearDown();
        Physics2D.simulationMode = simulationInitiale;
        Time.timeScale = tempsInitial;
    }

    private GameObject Anneau(Vector2 position, bool mobile = false)
    {
        GameObject objet = Creer("Anneau test", position);
        objet.AddComponent<CircleCollider2D>().radius = 0.2f;
        objet.GetComponent<Collider2D>().isTrigger = true;
        if (mobile) objet.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        objet.AddComponent(Script("GrappleAnchor"));
        objet.SetActive(true);
        Physics2D.SyncTransforms();
        return objet;
    }

    private Component Ennemi(Vector2 position)
    {
        GameObject objet = Creer("Ennemi test", position);
        objet.AddComponent<Rigidbody2D>();
        objet.AddComponent<BoxCollider2D>().size = Vector2.one;
        Component ennemi = objet.AddComponent(Script("Ennemy"));
        objet.SetActive(true);
        Physics2D.SyncTransforms();
        return ennemi;
    }

    private void Lancer(Vector2 cible) => Appeler(grappin, "LancerVers", cible);
    private void Pas() => Appeler(grappin, "FixedUpdate");
    private void JusquAuContact()
    {
        for (int i = 0; i < 100 && Etat == "Lance"; i++) Pas();
    }

    [Test]
    public void LanceEnLigneDroitePuisRevientA12Unites()
    {
        Lancer(new Vector2(100f, 0f));
        Pas();
        Vector2 tete = (Vector2)Lire(grappin, "tete");
        Assert.That(tete.x, Is.GreaterThan(0.25f));
        Assert.That(tete.y, Is.EqualTo(0f).Within(0.001f));
        // Le curseur peut changer sans rediriger le projectile.
        Ecrire(grappin, "cibleClic", Vector2.up);
        JusquAuContact();
        Assert.That(Etat, Is.EqualTo("Retour"));
        Assert.That(Vector2.Distance((Vector2)Lire(grappin, "tete"), new Vector2(0.25f, 0f)),
            Is.EqualTo(12f).Within(0.002f));
        for (int i = 0; i < 100 && Etat != "EnMain"; i++) Pas();
        Assert.That(Etat, Is.EqualTo("EnMain"));
    }

    [Test]
    public void ClicGaucheLanceEtClicSuivantDecroche()
    {
        GameObject cameraObjet = Creer("Camera test", new Vector2(0f, 0f));
        cameraObjet.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObjet.AddComponent<Camera>();
        camera.orthographic = true;
        camera.pixelRect = new Rect(0f, 0f, 800f, 600f);
        cameraObjet.SetActive(true);
        Ecrire(grappin, "cameraVisee", camera);
        Anneau(new Vector2(4f, 0f));
        souris = InputSystem.AddDevice<Mouse>();
        souris.MakeCurrent();
        Vector2 ecran = camera.WorldToScreenPoint(new Vector3(4f, 0f, 0f));
        InputSystem.QueueStateEvent(souris, new MouseState { position = ecran }.WithButton(MouseButton.Left));
        InputSystem.Update();
        Assert.That(souris.leftButton.wasPressedThisFrame, Is.True);
        Assert.That(Mouse.current, Is.SameAs(souris));
        Assert.That((bool)grappin.GetType().GetMethod("PeutJouer", Flags).Invoke(grappin, null), Is.True);
        Appeler(grappin, "Update");
        Pas();
        JusquAuContact();
        Assert.That(Etat, Is.EqualTo("Accroche"));
        InputSystem.QueueStateEvent(souris, new MouseState { position = ecran });
        InputSystem.Update();
        InputSystem.QueueStateEvent(souris, new MouseState { position = ecran }.WithButton(MouseButton.Left));
        InputSystem.Update();
        Appeler(grappin, "Update");
        Pas();
        Assert.That(Etat, Is.EqualTo("Retour"));
        Assert.That(corps.GetComponent<DistanceJoint2D>().enabled, Is.False);
    }

    [Test]
    public void MurBloqueLAnneauDerriereLui()
    {
        GameObject mur = Creer("Mur test", new Vector2(2f, 0f));
        mur.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 4f);
        mur.SetActive(true);
        Anneau(new Vector2(4f, 0f));
        Lancer(new Vector2(4f, 0f));
        JusquAuContact();
        Assert.That(Etat, Is.EqualTo("Retour"));
        Assert.That(((Vector2)Lire(grappin, "tete")).x, Is.LessThan(2f));
    }

    [Test]
    public void PiecesEnTriggerNeBloquentPasLAnneau()
    {
        GameObject piece = Creer("Piece test", new Vector2(2f, 0f));
        piece.AddComponent<CircleCollider2D>().isTrigger = true;
        piece.SetActive(true);
        Anneau(new Vector2(4f, 0f));
        Lancer(new Vector2(4f, 0f));
        JusquAuContact();
        Assert.That(Etat, Is.EqualTo("Accroche"));
    }

    [Test]
    public void EnnemiRecoitUnSeulCoupDe3A6SansAttraction()
    {
        Component ennemi = Ennemi(new Vector2(4f, 0f));
        Vector2 vitesseEnnemi = new Vector2(2f, 0f);
        ennemi.GetComponent<Rigidbody2D>().linearVelocity = vitesseEnnemi;
        corps.linearVelocity = new Vector2(11f, 3f);
        Lancer(new Vector2(4f, 0f));
        JusquAuContact();
        int vie = (int)Propriete(ennemi, "PvActuels");
        Assert.That(12 - vie, Is.InRange(3, 6));
        Assert.That(Etat, Is.EqualTo("Retour"));
        Assert.That(corps.GetComponent<DistanceJoint2D>().enabled, Is.False);
        Assert.That(corps.linearVelocity, Is.EqualTo(new Vector2(11f, 3f)));
        Assert.That(ennemi.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(vitesseEnnemi));
        for (int i = 0; i < 20; i++) Pas();
        Assert.That(Propriete(ennemi, "PvActuels"), Is.EqualTo(vie));
    }

    [Test]
    public void AnneauEtDecrochageConserventLaVitesseDuJoueur()
    {
        Anneau(new Vector2(0f, 5f));
        Vector2 elan = new Vector2(13f, 4f);
        corps.linearVelocity = elan;
        Lancer(new Vector2(0f, 5f));
        JusquAuContact();
        Assert.That(Etat, Is.EqualTo("Accroche"));
        Appeler(mouvements, "FixedUpdate");
        Assert.That(corps.linearVelocity, Is.EqualTo(elan));
        Ecrire(grappin, "clicDemande", true);
        Pas();
        Appeler(mouvements, "FixedUpdate");
        Assert.That(corps.linearVelocity, Is.EqualTo(elan));
        corps.linearVelocity = new Vector2(13f, -4f);
        Appeler(mouvements, "FixedUpdate");
        Assert.That(corps.linearVelocity, Is.EqualTo(new Vector2(13f, -4f)));
    }

    [Test]
    public void BalancierPhysiqueLimiteLaDistanceSansBloquerLeMouvement()
    {
        Vector2 positionAnneau = new Vector2(0f, 5f);
        Anneau(positionAnneau);
        corps.linearVelocity = new Vector2(8f, 0f);
        Lancer(positionAnneau);
        JusquAuContact();
        DistanceJoint2D corde = corps.GetComponent<DistanceJoint2D>();
        float xMaximum = 0f;
        for (int i = 0; i < 150; i++)
        {
            Pas();
            Appeler(mouvements, "FixedUpdate");
            Physics2D.Simulate(Time.fixedDeltaTime);
            Assert.That(Vector2.Distance(corps.position, positionAnneau), Is.LessThan(corde.distance + 0.1f));
            xMaximum = Mathf.Max(xMaximum, Mathf.Abs(corps.position.x));
        }
        Assert.That(xMaximum, Is.GreaterThan(1f));
    }

    [Test]
    public void AncrageSuitUnAnneauMobile()
    {
        GameObject anneau = Anneau(new Vector2(4f, 0f), true);
        Lancer(new Vector2(4f, 0f));
        JusquAuContact();
        Assert.That(corps.GetComponent<DistanceJoint2D>().connectedBody,
            Is.EqualTo(anneau.GetComponent<Rigidbody2D>()));
        anneau.GetComponent<Rigidbody2D>().MovePosition(new Vector2(4f, 1f));
        Physics2D.Simulate(Time.fixedDeltaTime);
        Pas();
        Assert.That((Vector2)Lire(grappin, "tete"), Is.EqualTo(new Vector2(4f, 1f)));
    }

    [Test]
    public void AnneauDesactiveDeclencheLeRetour()
    {
        GameObject anneau = Anneau(new Vector2(4f, 0f));
        Lancer(new Vector2(4f, 0f));
        JusquAuContact();
        anneau.SetActive(false);
        Pas();
        Assert.That(Etat, Is.EqualTo("Retour"));
        Assert.That(corps.GetComponent<DistanceJoint2D>().enabled, Is.False);
    }

    [Test]
    public void PauseNeConsommePasDeClic()
    {
        Time.timeScale = 0f;
        Ecrire(grappin, "clicDemande", true);
        Appeler(grappin, "Update");
        Assert.That(Lire(grappin, "clicDemande"), Is.False);
        Pas();
        Assert.That(Etat, Is.EqualTo("EnMain"));
    }

    [Test]
    public void DesactivationLibereLaCorde()
    {
        Anneau(new Vector2(4f, 0f));
        Lancer(new Vector2(4f, 0f));
        JusquAuContact();
        ((Behaviour)grappin).enabled = false;
        Assert.That(Etat, Is.EqualTo("EnMain"));
        Assert.That(corps.GetComponent<DistanceJoint2D>().enabled, Is.False);
    }

    [Test]
    public void MortDuJoueurLibereLaCorde()
    {
        Anneau(new Vector2(4f, 0f));
        Lancer(new Vector2(4f, 0f));
        JusquAuContact();
        Appeler(corps.GetComponent(Script("Player")), "Tuer");
        Assert.That(((Behaviour)grappin).enabled, Is.False);
        Assert.That(corps.GetComponent<DistanceJoint2D>().enabled, Is.False);
        Assert.That(corps.simulated, Is.False);
    }

    [Test]
    public void EnnemiMortDesactiveSesCollisions()
    {
        Component ennemi = Ennemi(new Vector2(4f, 0f));
        Appeler(ennemi, "PrendreDegats", 12);
        Assert.That(Propriete(ennemi, "EstMort"), Is.True);
        Assert.That(Propriete(ennemi, "PvActuels"), Is.EqualTo(0));
        Assert.That(ennemi.gameObject.activeSelf, Is.False);
        Assert.That(ennemi.GetComponent<Rigidbody2D>().simulated, Is.False);
        Assert.That(ennemi.GetComponent<Collider2D>().enabled, Is.False);
    }

    [Test]
    public void DirectionAjouteDeLElanEtPermetDeFreinerApresDecrochage()
    {
        Anneau(new Vector2(0f, 5f));
        Lancer(new Vector2(0f, 5f));
        JusquAuContact();
        Ecrire(mouvements, "direction", 1f);
        Appeler(mouvements, "FixedUpdate");
        Assert.That(corps.linearVelocity.x, Is.GreaterThan(0f));
        corps.linearVelocity = new Vector2(13f, 0f);
        Ecrire(grappin, "clicDemande", true);
        Pas();
        Ecrire(mouvements, "direction", -1f);
        Appeler(mouvements, "FixedUpdate");
        Assert.That(corps.linearVelocity.x, Is.InRange(12f, 13f));
    }

    [Test]
    public void ClicDroitEnrouleEtRelachementArreteSansPerdreLElan()
    {
        Anneau(new Vector2(0f, 5f));
        Lancer(new Vector2(0f, 5f));
        JusquAuContact();
        DistanceJoint2D joint = corps.GetComponent<DistanceJoint2D>();
        float longueurInitiale = joint.distance;
        corps.linearVelocity = new Vector2(8f, 0f);
        souris = InputSystem.AddDevice<Mouse>();
        souris.MakeCurrent();
        InputSystem.QueueStateEvent(souris, new MouseState().WithButton(MouseButton.Right));
        InputSystem.Update();
        Appeler(grappin, "Update");
        Pas();
        Assert.That(Etat, Is.EqualTo("Accroche"));
        Assert.That(joint.distance, Is.LessThan(longueurInitiale));
        Assert.That(corps.linearVelocity.x, Is.EqualTo(8f).Within(0.001f));
        Assert.That(corps.linearVelocity.y, Is.GreaterThan(5f));
        float longueurEnroulee = joint.distance;
        InputSystem.QueueStateEvent(souris, new MouseState());
        InputSystem.Update();
        Appeler(grappin, "Update");
        Pas();
        Assert.That(joint.distance, Is.EqualTo(longueurEnroulee));
        Vector2 elan = corps.linearVelocity;
        Ecrire(grappin, "clicDemande", true);
        Pas();
        Appeler(mouvements, "FixedUpdate");
        Assert.That(Etat, Is.EqualTo("Retour"));
        Assert.That(corps.linearVelocity, Is.EqualTo(elan));
    }

    [Test]
    public void RetractionMontePhysiquementEtConserveLElanAuDecrochage()
    {
        Anneau(new Vector2(0f, 5f));
        Lancer(new Vector2(0f, 5f));
        JusquAuContact();
        Ecrire(grappin, "retractionDemandee", true);
        for (int i = 0; i < 25; i++)
        {
            Vector2 positionAvant = corps.position;
            Pas();
            Appeler(mouvements, "FixedUpdate");
            Physics2D.Simulate(Time.fixedDeltaTime);
            Assert.That(Vector2.Distance(corps.position, positionAvant), Is.LessThan(0.3f));
        }
        Assert.That(corps.position.y, Is.GreaterThan(2f));
        Assert.That(corps.linearVelocity.y, Is.GreaterThan(4f));
        Vector2 elan = corps.linearVelocity;
        Ecrire(grappin, "clicDemande", true);
        Pas();
        Appeler(mouvements, "FixedUpdate");
        Assert.That(corps.linearVelocity, Is.EqualTo(elan));
        float hauteurAvant = corps.position.y;
        Physics2D.Simulate(Time.fixedDeltaTime);
        Assert.That(corps.position.y, Is.GreaterThan(hauteurAvant));
    }

    [Test]
    public void RetractionRespecteLaLongueurMinimale()
    {
        Anneau(new Vector2(0f, 5f));
        Lancer(new Vector2(0f, 5f));
        JusquAuContact();
        Ecrire(grappin, "retractionDemandee", true);
        DistanceJoint2D joint = corps.GetComponent<DistanceJoint2D>();
        for (int i = 0; i < 100; i++)
        {
            Pas();
            Appeler(mouvements, "FixedUpdate");
            Physics2D.Simulate(Time.fixedDeltaTime);
            Assert.That(joint.distance, Is.GreaterThanOrEqualTo(1.2f));
        }
        Assert.That(joint.distance, Is.EqualTo(1.2f).Within(0.001f));
        Assert.That(Etat, Is.EqualTo("Accroche"));
    }

    [Test]
    public void RetractionNAllongePasUneAccrocheDejaTresCourte()
    {
        Anneau(new Vector2(0f, 0.5f));
        Lancer(new Vector2(0f, 0.5f));
        JusquAuContact();
        DistanceJoint2D joint = corps.GetComponent<DistanceJoint2D>();
        float longueur = joint.distance;
        Ecrire(grappin, "retractionDemandee", true);
        Pas();
        Assert.That(Etat, Is.EqualTo("Accroche"));
        Assert.That(joint.distance, Is.EqualTo(longueur));
    }

    [Test]
    public void PauseArreteLaRetractionEtEffaceLaDemande()
    {
        Anneau(new Vector2(0f, 5f));
        Lancer(new Vector2(0f, 5f));
        JusquAuContact();
        float longueur = corps.GetComponent<DistanceJoint2D>().distance;
        Ecrire(grappin, "retractionDemandee", true);
        Time.timeScale = 0f;
        Appeler(grappin, "Update");
        Pas();
        Assert.That(Lire(grappin, "retractionDemandee"), Is.False);
        Assert.That(corps.GetComponent<DistanceJoint2D>().distance, Is.EqualTo(longueur));
        Time.timeScale = 1f;
        Pas();
        Assert.That(corps.GetComponent<DistanceJoint2D>().distance, Is.EqualTo(longueur));
    }

    [Test]
    public void AucunCrochetNiCordeAuRepos()
    {
        Appeler(grappin, "LateUpdate");
        SpriteRenderer visuel = (SpriteRenderer)Lire(grappin, "crochet");
        Assert.That(visuel.enabled, Is.False);
        Assert.That(((LineRenderer)Lire(grappin, "corde")).enabled, Is.False);
        Assert.That(visuel.GetComponent<Rigidbody2D>(), Is.Null);
        Assert.That(visuel.GetComponent<Collider2D>(), Is.Null);
    }

#if UNITY_EDITOR
    [Test]
    public void LancerRateJoueLAnimationUneFoisSansCordeMemeApresLeRetour()
    {
        Lancer(new Vector2(100f, 0f));
        Appeler(grappin, "LateUpdate");
        SpriteRenderer visuel = (SpriteRenderer)Lire(grappin, "crochet");
        Animator animation = visuel.GetComponent<Animator>();
        Assert.That(visuel.enabled, Is.True);
        Assert.That(visuel.sprite, Is.Not.Null);
        Sprite premiereImage = visuel.sprite;
        animation.Update(0.2f);
        Assert.That(visuel.sprite, Is.Not.EqualTo(premiereImage));
        JusquAuContact();
        for (int i = 0; i < 100 && Etat != "EnMain"; i++) Pas();
        Appeler(grappin, "LateUpdate");
        Assert.That(Etat, Is.EqualTo("EnMain"));
        Assert.That(visuel.enabled, Is.True, "Le retour physique ne coupe pas l'animation.");
        Assert.That(((LineRenderer)Lire(grappin, "corde")).enabled, Is.False);
        animation.Update(1f);
        Appeler(grappin, "LateUpdate");
        Assert.That(visuel.enabled, Is.False);
        Assert.That(animation.enabled, Is.False);
        Lancer(new Vector2(100f, 0f));
        Appeler(grappin, "LateUpdate");
        Assert.That(visuel.enabled, Is.True);
        Assert.That(visuel.sprite, Is.EqualTo(premiereImage));
    }

    [Test]
    public void AccrocheRemplaceLAnimationParLaCordePuisDecrochageMasqueLesDeux()
    {
        Anneau(new Vector2(4f, 0f));
        Lancer(new Vector2(4f, 0f));
        JusquAuContact();
        Appeler(grappin, "LateUpdate");
        SpriteRenderer visuel = (SpriteRenderer)Lire(grappin, "crochet");
        LineRenderer corde = (LineRenderer)Lire(grappin, "corde");
        Assert.That(Etat, Is.EqualTo("Accroche"));
        Assert.That(visuel.enabled, Is.False);
        Assert.That(visuel.GetComponent<Animator>().enabled, Is.False);
        Assert.That(corde.enabled, Is.True);
        Assert.That(corps.GetComponent<DistanceJoint2D>().enabled, Is.True);
        Assert.That(Vector2.Distance(corde.GetPosition(1), new Vector2(4f, 0f)), Is.LessThan(0.001f));
        Appeler(grappin, "CommencerRetour");
        Appeler(grappin, "LateUpdate");
        Assert.That(visuel.enabled, Is.False);
        Assert.That(corde.enabled, Is.False);
        Assert.That(corps.GetComponent<DistanceJoint2D>().enabled, Is.False);
    }

    [Test]
    public void AnimationSuitLaMainEtGardeLaDirectionDuClic()
    {
        Lancer(new Vector2(100f, 0f));
        Appeler(grappin, "LateUpdate");
        SpriteRenderer visuel = (SpriteRenderer)Lire(grappin, "crochet");
        Vector2 pivot = (Vector2)Lire(grappin, "pivotAnimation");
        Assert.That(Vector2.Distance(visuel.transform.TransformPoint(pivot), new Vector2(0.25f, 0f)),
            Is.LessThan(0.001f));
        corps.GetComponent<SpriteRenderer>().flipX = true;
        Appeler(grappin, "LateUpdate");
        Assert.That(Vector2.Distance(visuel.transform.TransformPoint(pivot), new Vector2(-0.25f, 0f)),
            Is.LessThan(0.001f));
        Assert.That(Vector2.Dot(visuel.transform.right, Vector2.right), Is.GreaterThan(0.99f));
        Appeler(grappin, "Reinitialiser");
        Lancer(new Vector2(-100f, 0f));
        Appeler(grappin, "LateUpdate");
        Assert.That(Vector2.Dot(visuel.transform.right, Vector2.left), Is.GreaterThan(0.99f));
    }

    [Test]
    public void DesactiverLeGrappinMasqueAnimationEtCorde()
    {
        Lancer(new Vector2(100f, 0f));
        Appeler(grappin, "LateUpdate");
        ((Behaviour)grappin).enabled = false;
        Assert.That(((SpriteRenderer)Lire(grappin, "crochet")).enabled, Is.False);
        Assert.That(((LineRenderer)Lire(grappin, "corde")).enabled, Is.False);
        Assert.That(((SpriteRenderer)Lire(grappin, "crochet")).GetComponent<Animator>().enabled, Is.False);
    }
#endif
}
