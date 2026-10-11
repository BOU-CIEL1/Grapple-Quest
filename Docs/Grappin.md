# Grappin

Le prefab `Player` porte deja `Grapple`, avec le sprite importe. Tous les anneaux
du prefab `GrappleClipChained` portent `GrappleAnchor` sur l'enfant `GrappleClip`.
Les instances existantes de ces prefabs heritent de ces modifications.

- Clic gauche : lancer en ligne droite vers la position du curseur au clic.
- Retour automatique apres 12 unites de trajet, si la corde du lancer depasse
  12 unites, ou au contact d'un obstacle non accrochable.
- Anneau : corde physique, Q/D ou les fleches pour prendre de l'elan.
- Maintenir le clic droit pendant l'accroche : enrouler la corde pour grimper.
  Relacher le clic droit arrete l'enroulement ; l'accroche reste en place.
- Les chaines et les anneaux sont traversables par le joueur. Les anneaux
  restent detectes par le grappin, et les chaines ne bloquent plus les tirs.
- Nouveau clic : decrochage immediat, puis retour visuel a la main.
- Ennemi ou boss : un seul coup de 3 a 6 degats inclus, puis retour.
  Aucun des deux personnages ne subit d'attraction.
- BigRock : un coup par lancer, petit tremblement du sprite, destruction au
  troisieme coup. Le rocher reste fixe et solide tant qu'il n'est pas detruit.
- Pendant le retour, attendre que le crochet revienne avant de relancer.

La corde utilise un `DistanceJoint2D` avec une distance maximale : elle peut se
detendre, et ne pousse pas le joueur. La vitesse n'est pas remise a zero quand
on s'accroche ou se decroche. Apres le decrochage, l'elan reste jusqu'au retour
au sol. Q/D permettent encore de freiner ou de corriger la vitesse en l'air.
L'accroche physique part du centre du joueur pour garder un balancier stable ;
la corde visible part de sa main. La corde ne s'enroule pas autour du decor.

## Reglages dans Unity

Selectionner le prefab `Player`, composant `Grapple` :

- `Position Main` : placement local sur la main droite, miroir a gauche.
  Valeur initiale `(0.085, -0.005)` adaptee a l'echelle actuelle de 7.1.
  `Main` permet de remplacer ce placement par un Transform enfant.
- `Taille Visuel`, `Angle Sprite` : taille en unites monde et orientation de
  l'asset. Le sprite actuel pointe vers le haut a gauche, d'ou `-135` degres.
- `Vitesse Lancer`, `Vitesse Retour`, `Portee Max`, `Rayon Impact` : sensations
  du lancer. Un rayon un peu plus grand facilite la visee des petits anneaux.
- `Epaisseur Corde`, `Couleur Corde` : aspect de la corde.
- `Vitesse Retraction` : vitesse d'enroulement (6 unites/seconde par defaut).
  `Longueur Min` : longueur minimale de corde (1.2 unite par defaut).
  L'enroulement conserve l'elan du balancier et donne une vitesse vers l'anneau
  quand la corde est tendue. Se decrocher permet de conserver cet elan de remontee.
- `Surfaces` doit inclure le decor, les anneaux et les ennemis pour bloquer
  correctement les tirs derriere les murs. Les triggers sans cible sont ignores.

Dans `Mouvements`, `Acceleration Balancier` regle l'aide Q/D quand on est
accroche et `Acceleration Air` regle le controle apres le decrochage.
Les ennemis ont 12 PV par defaut, modifiables avec `Pv Max` dans `Ennemy`.
Ils disparaissent a 0 PV ; aucune animation de mort n'est imposee.

Pour un nouvel anneau : ajouter `GrappleAnchor` sur l'objet portant son
`Collider2D`, avec un `Point Accroche` optionnel. Ne pas le mettre sur la chaine
ou sur toute la plateforme. Cocher `Is Trigger` sur les colliders de l'anneau
et de la chaine pour les rendre traversables. Les anneaux mobiles
conservent leur Rigidbody2D : l'accroche suit son mouvement.

## Essai dans Level1

1. Attendre l'import et verifier que la Console n'a aucune erreur.
2. Lancer Play et viser un anneau au clic gauche. Verifier le placement en main
   a droite et a gauche ; ajuster `Position Main` si necessaire sur les animations.
3. Sauter, s'accrocher, prendre de l'elan avec Q/D, puis cliquer pour se lacher.
   Tester aussi un anneau au-dessus : maintenir clic droit pour monter, relacher
   pour garder la longueur obtenue, puis clic gauche pour se lacher avec de l'elan.
4. Tirer dans le vide et dans une plateforme : le crochet doit revenir.
5. Tirer sur un ennemi : il doit disparaitre apres 2 a 4 coups pour ses 12 PV
   par defaut. Verifier aussi pause et mort pendant une accroche.
6. Tirer trois fois sur BigRock : il tremble sur les deux premiers coups puis
   disparait au troisieme. Dans son composant `BigRock`, `Amplitude` et
   `Duree Tremblement` permettent d'ajuster l'effet visuel.

Les tests automatiques sont dans `Assets/Tests/PlayMode`. Les lancer depuis
`Window > General > Test Runner > PlayMode`.
Les tests couvrent les comportements physiques et les entrees. Le placement
sur les animations et les sensations de Level1 restent a verifier visuellement
dans la scene.
