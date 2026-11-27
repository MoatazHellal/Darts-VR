# Darts VR --- Dictionnaire des données CSV

**Version :** 1.0 --- documentation du fonctionnement actuel en simulation\
**Projet :** Comparaison de l'effet du guidage et du feedback terminal sur l'amélioration de la précision au lancer de fléchettes

## 1. Objectif

Le fichier CSV permet de comparer les performances des participants dans les deux conditions expérimentales : `Guidance` (guidage) et `Biofeedback` (feedback terminal). La mesure principale prévue est la distance entre l'impact et le centre de la cible. Le score, les échecs et les paramètres du lancer constituent des mesures complémentaires.

## 2. Organisation des fichiers

Un fichier est créé à l'initialisation de chaque session, même si aucun essai n'est effectué. Chaque tentative finalisée et exportée avec succès produit une ligne, y compris les tentatives invalides. La première ligne contient les 36 noms de colonnes.

Convention : `<participant_id>_<session_id>.csv`.

Exemples : `TEST-P004_86cf747256734bd584d90f3a212c5278.csv` (illustratif), `P001_<session_id>.csv` (session réelle).

-   Séparateur : virgule.
-   Séparateur décimal : point ; notation scientifique possible.
-   Booléens : `true`, `false`.
-   Dates : ISO 8601 en UTC, suffixe `Z`.
-   Champ absent : cellule vide, distincte de zéro.
-   Encodage et règles d'échappement des chaînes : à confirmer dans `CSVExporter.cs`.

### Emplacement

Racine : `Application.persistentDataPath/ExperimentData/`.

Sous Windows, le chemin observé est `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Darts/ExperimentData/Test/` pour les tests. Le sous-dossier exact des sessions réelles doit être confirmé dans l'exporteur.

Sur Meta Quest 3 (Android), la racine Unity est généralement `/storage/emulated/0/Android/data/<application_id>/files/`. L'identifiant de l'application et le chemin effectif doivent être vérifiés sur le casque. Le journal `CSV file:` indique le chemin de sortie à la fin de l'expérience.

## 3. Dictionnaire des 36 colonnes

Les exemples sont illustratifs et reprennent les formats observés dans les simulations. Les vecteurs sont décomposés en trois composantes.

  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
              Nº Colonne                      Type         Unité        Description                                                                                   Exemple
  -------------- ---------------------------- ------------ ------------ --------------------------------------------------------------------------------------------- ----------------------------------
               1 `participant_id`             Texte        ---          Identifiant du participant, préfixé par TEST- en mode test.                                   TEST-P004

               2 `session_id`                 Texte        ---          GUID unique de session, 32 caractères hexadécimaux sans tirets.                               86cf747256734bd584d90f3a212c5278

               3 `is_test_mode`               Booléen      ---          Indique si la session est un test.                                                            true

               4 `dominant_hand`              Texte        ---          Main dominante sélectionnée : Right ou Left.                                                  Right

               5 `language`                   Texte        ---          Langue sélectionnée : FR ou EN.                                                               FR

               6 `condition_group`            Texte        ---          Groupe de contrebalancement A ou B.                                                           B

               7 `condition_order`            Texte        ---          Ordre des conditions G-B ou B-G.                                                              B-G

               8 `condition_index`            Entier       ---          Rang de la condition dans la session : 1 ou 2.                                                1

               9 `condition`                  Texte        ---          Méthode active : Guidance ou Biofeedback.                                                     Biofeedback

              10 `attempt_number`             Entier       ---          Numéro de tentative dans toute la session, invalides incluses.                                3

              11 `trial_number`               Entier       ---          Rang prévu de l'essai dans la condition ; peut être répété après une tentative non comptée.   2

              12 `global_trial_number`        Entier       ---          Rang prévu de l'essai dans la session ; peut être répété après une tentative non comptée.     2

              13 `is_counted`                 Booléen      ---          Indique si la tentative contribue au quota expérimental.                                      true

              14 `elapsed_time_s`             Décimal      s            Temps entre la saisie enregistrée et le relâchement.                                          1.03

              15 `release_position_x_m`       Décimal      m            Position X au relâchement, repère cible.                                                      0.1

              16 `release_position_y_m`       Décimal      m            Position Y au relâchement, repère cible.                                                      0

              17 `release_position_z_m`       Décimal      m            Position Z au relâchement, repère cible.                                                      2.37

              18 `release_velocity_x_ms`      Décimal      m/s          Composante X de la vitesse au relâchement, repère cible.                                      -0.253

              19 `release_velocity_y_ms`      Décimal      m/s          Composante Y de la vitesse au relâchement, repère cible.                                      0

              20 `release_velocity_z_ms`      Décimal      m/s          Composante Z de la vitesse au relâchement, repère cible.                                      -5.995

              21 `release_speed_ms`           Décimal      m/s          Norme du vecteur vitesse au relâchement.                                                      6

              22 `release_direction_x`        Décimal      Sans unité   Composante X de la direction normalisée de la fléchette.                                      -0.0422

              23 `release_direction_y`        Décimal      Sans unité   Composante Y de cette direction.                                                              0

              24 `release_direction_z`        Décimal      Sans unité   Composante Z de cette direction.                                                              -0.9991

              25 `vertical_tilt_deg`          Décimal      °            Inclinaison verticale signée par rapport à l'horizontale globale Unity.                       0

              26 `horizontal_tilt_deg`        Décimal      °            Angle horizontal signé par rapport à la référence transmise à RecordRelease.                  0

              27 `impact_position_x_m`        Décimal      m            Position X de l'impact valide, repère cible.                                                  0.05

              28 `impact_position_y_m`        Décimal      m            Position Y de l'impact valide, repère cible.                                                  0

              29 `impact_position_z_m`        Décimal      m            Position Z de l'impact valide, repère cible.                                                  0

              30 `impact_distance_cm`         Décimal      cm           Distance au centre fournie par le mécanisme d'impact, convertie de mètres en centimètres.     5

              31 `score`                      Entier       Points       Score fourni par l'impact ; zéro pour Miss et Deflected ; vide si non mesuré.                 20

              32 `zone`                       Texte        ---          Nom de la zone transmis par le mécanisme d'impact.                                            SimulatedSingle20

              33 `trial_status`               Texte        ---          Statut final de la tentative (voir section 5).                                                Hit

              34 `failure_reason`             Texte        ---          Motif technique ou événement associé, si renseigné.                                           NoImpactDetected

              35 `release_timestamp_utc`      Date/heure   UTC          Horodatage du relâchement enregistré.                                                         2026-11-27T01:17:06.1283013Z

              36 `completion_timestamp_utc`   Date/heure   UTC          Horodatage de finalisation de la tentative, avant export.                                     2026-11-27T01:17:06.4392281Z
  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

## 4. Repère spatial et mesures temporelles

`TrialRecorder` utilise le Transform `boardCenter` comme origine et orientation. Pour une position mondiale p, la position exportée est `Quaternion.Inverse(boardCenter.rotation) * (p - boardCenter.position)`. Pour un vecteur vitesse ou direction, seule la rotation inverse est appliquée. Aucune division par l'échelle du Transform n'est effectuée. Les unités métriques supposent une scène calibrée à une unité Unity par mètre.

Les axes X, Y, Z suivent respectivement les axes locaux right, up et forward du centre de la cible. Leur orientation physique doit être confirmée dans la scène réelle. Dans les simulations, la position de départ Z est positive et la vitesse Z est négative.

La direction de la fléchette est `-transform.forward` au relâchement ; elle n'est pas nécessairement identique à la direction de la vitesse. La vitesse scalaire est la norme du vecteur vitesse. L'angle vertical est calculé à partir du produit scalaire de la direction normalisée avec `Vector3.up` (positif vers le haut). L'angle horizontal est calculé avec `Atan2` relativement à la projection horizontale de `forwardReference` ; le signe positif correspond au côté droit de cette référence. La référence réellement passée par `DartHandGrabber` reste à confirmer. Le simulateur passe la direction du lancer elle-même, ce qui explique les angles horizontaux proches de zéro.

Le temps écoulé utilise `Time.realtimeSinceStartupAsDouble` : instant de relâchement moins instant de saisie. Il ne mesure ni le temps de vol ni nécessairement tout le temps de positionnement prévu au protocole. Le temps entre les deux horodatages UTC représente le délai de finalisation après relâchement, pas une mesure garantie du temps de vol physique.

## 5. Statuts et règles de comptabilisation

  ----------------------------------------------------------------------------------------------------------------------------------------------------------------
  Statut         Définition actuelle                                           Compté                      Distance                    Score
  -------------- ------------------------------------------------------------- --------------------------- --------------------------- ---------------------------
  Hit            Impact accepté, score strictement positif.                    Oui                         Mesurée                     Positif

  Outside        Impact accepté, score nul (cas observé).                      Oui                         Mesurée                     0

  Miss           Échec signalé par OnDartMiss.                                 Oui                         Vide                        0

  Deflected      Impact rejeté, motif RejectedBoardImpact.                     Oui                         Vide                        0

  Timeout        Aucun résultat détecté dans le délai impactTimeout.           Selon countTimeoutAsTrial   Vide                        Vide dans les tests

  Invalid        Tentative annulée ou fléchette détruite avant finalisation.   Non                         Selon données disponibles   Selon données disponibles
  ----------------------------------------------------------------------------------------------------------------------------------------------------------------

`Pending` est un état initial interne, non attendu dans les lignes finalisées. La configuration actuelle du recorder est un délai de 5 secondes et `countTimeoutAsTrial = true`. Une tentative invalide avant relâchement ne contient pas de mesures de relâchement ; une invalidation après relâchement peut en conserver.

Les motifs observés incluent `SimulatedTrackingLoss`, `SimulatedWallImpact`, `RejectedBoardImpact`, `NoImpactDetected`. Le code prévoit également `DartDestroyed`. Les motifs commençant par Simulated sont propres aux tests. La liste réelle exhaustive dépend des scripts d'interaction et de collision.

### Exemple de numérotation (extrait de colonnes)

``` csv
condition,attempt_number,trial_number,global_trial_number,is_counted,trial_status
Guidance,1,1,1,true,Hit
Guidance,2,2,2,false,Invalid
Guidance,3,2,2,true,Miss
Guidance,4,3,3,true,Outside
Biofeedback,5,1,4,true,Hit
Biofeedback,6,2,5,true,Deflected
Biofeedback,7,3,6,true,Timeout
```

Sept tentatives sont exportées, six sont comptabilisées. Le numéro d'essai local repart à 1 lors du changement de condition. Le numéro de tentative ne repart pas à 1. Pour identifier une ligne de manière unique, utiliser le couple (`session_id`, `attempt_number`).

## 6. Contrebalancement et phases

  Groupe   Numéro participant   Ordre   Condition 1   Condition 2
  -------- -------------------- ------- ------------- -------------
  A        Impair               G-B     Guidance      Biofeedback
  B        Pair                 B-G     Biofeedback   Guidance

Les compteurs test et réel sont indépendants (`LastTestParticipantNumber`, `LastParticipantNumber`), persistés dans PlayerPrefs. Ils sont incrémentés à l'initialisation, même si la session est abandonnée. Ils sont locaux à l'appareil ; une attribution centralisée est nécessaire si plusieurs casques collectent des données. L'alternance des ordres vise à limiter les effets d'apprentissage et de fatigue, sans les supprimer.

Déroulement automatisé testé : accueil → attente du suivi de la main → préparation → première condition → pause → deuxième condition → fin. Le quota est de N essais comptabilisés par condition. Valeurs par défaut du code : N=20, préparation=10 s, pause=120 s ; tests rapides : N=3, préparation=5 s, pause=10 s. Les valeurs de l'Inspector prévalent. Ces paramètres ne figurent pas dans les 36 colonnes : conserver la configuration utilisée séparément pour chaque campagne. La familiarisation prévue au protocole doit être exclue des données expérimentales ; son intégration complète reste à vérifier.

## 7. Exploitation et contrôle qualité

-   Exclure les sessions de test (`is_test_mode=true`) de l'analyse réelle.
-   Conserver les fichiers bruts ; travailler sur des copies.
-   Filtrer `is_counted=true` pour les essais du quota, mais conserver les invalides pour le contrôle qualité.
-   Ne jamais remplacer une distance manquante par zéro : zéro signifie un impact au centre.
-   Distinguer Outside (distance mesurée, score nul) de Miss, Deflected et Timeout (distance absente).
-   Calculer les distances moyennes sur les distances disponibles uniquement en précisant le dénominateur et le taux d'échec ; cette analyse conditionnelle peut introduire un biais si les méthodes ont des taux d'échec différents. Définir avec l'équipe une stratégie principale pour les échecs avant la collecte réelle.
-   Vérifier 36 champs par ligne, unicité des couples session/tentative, cohérence des groupes et des conditions, progression des compteurs et N essais comptés par condition pour une session terminée.
-   Une session incomplète peut contenir uniquement l'en-tête ou moins de 2N essais comptés. Ne pas l'assimiler à une session complète.
-   Importer le CSV avec séparateur virgule et décimales point, notamment dans Excel en français.

Les questionnaires SUS et NASA-TLX prévus au protocole ne figurent pas dans ce CSV. Leur collecte et leur liaison aux identifiants de participant/session doivent être documentées séparément.

## 8. Validation et limites

Les tests Unity Editor ont confirmé l'export des six statuts, la numérotation, l'exclusion des invalides du quota, la fin à 6/6 pour le scénario G-B et l'alternance des groupes A/B/A. Le démarrage du scénario B-G a également été confirmé ; son export complet reste à vérifier sur les éléments disponibles.

Le simulateur génère des données artificielles et appelle directement le recorder : il ne valide ni la physique réelle, ni le suivi des mains, ni le calcul réel des scores et distances. Le module Guidance n'était pas encore assigné lors des tests communiqués. Avant la collecte réelle, vérifier son intégration, les interactions et collisions sur Quest 3, la récupération des CSV, la séparation des dossiers test/réel, la calibration spatiale, les paramètres définitifs et les règles méthodologiques (notamment Timeout et échecs). Désactiver le mode test pour les sessions réelles.

## 9. Références internes et maintenance

Scripts concernés : `SessionSettings.cs`, `ExperimentManager.cs`, `TrialRecorder.cs`, `TrialData.cs`, `CSVExporter.cs`, `ThrowData.cs`, `ExperimentTestSimulator.cs`. Les scripts de saisie et de collision fournissent les événements réels.

Cette version repose sur les scripts communiqués et les CSV de simulation observés. Mettre à jour le dictionnaire à chaque changement de schéma, de statut, d'unité ou de règle de comptabilisation. Vérifier les détails non confirmés dans le code final avant publication de la version expérimentale.
