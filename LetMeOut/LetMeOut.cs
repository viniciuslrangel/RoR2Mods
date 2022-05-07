using System;
using BepInEx;
using RoR2;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LetMeOut
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LetMeOutMod : BaseUnityPlugin
    {
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "viniciuslrangel";
        public const string PluginName = "LetMeOut";
        public const string PluginVersion = "1.1.2";

        private HoldoutZoneController currentTeleporter = null;
        private bool bIsCharging = false;
        private float chargeTime = 0f;
        private float chargeTimeDelay = 2f;

        private GameObject restrictionDome = null;

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            //Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            //But now we have defined an item, but it doesn't do anything yet. So we'll need to define that ourselves.
            // GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;

            SceneDirector.onPreGeneratePlayerSpawnPointsServer += (SceneDirector director, ref Action method) =>
            {
                bIsCharging = false;
                if (restrictionDome != null)
                {
                    Destroy(restrictionDome);
                }

                restrictionDome = null;
            };

            TeleporterInteraction.onTeleporterBeginChargingGlobal += interaction =>
            {
                if (bIsCharging)
                {
                    return;
                }

                bIsCharging = true;
                chargeTime = Time.time + chargeTimeDelay;
            };

            TeleporterInteraction.onTeleporterChargedGlobal += interaction =>
            {
                bIsCharging = false;
                if (restrictionDome != null)
                {
                    Destroy(restrictionDome);
                }
                restrictionDome = null;
            };

            // This line of log will appear in the bepinex console when the Awake method is done.
            Log.LogInfo(nameof(Awake) + " done.");
        }

        //The Update() method is run on every frame of the game.
        private void Update()
        {
            if (!bIsCharging || chargeTime > Time.time)
            {
                return;
            }

            var controller = TeleporterInteraction.instance?.holdoutZoneController;
            if (controller == null)
            {
                return;
            }

            if (controller != currentTeleporter)
            {
                if (currentTeleporter == null)
                {
                    currentTeleporter = controller;
                }
                else
                {
                    bIsCharging = false;
                    currentTeleporter = null;
                    return;
                }
            }

            if (controller.wasCharged)
            {
                bIsCharging = false;
                return;
            }

            var center = controller.transform.position;
            var radius = controller.currentRadius;

            if (restrictionDome != null)
            {
                if (!restrictionDome.scene.IsValid())
                {
                    Destroy(restrictionDome);
                    restrictionDome = null;
                }
            }

            if (restrictionDome == null)
            {
                createCollider();
                restrictionDome.transform.position = center;
            }

            var collisionRadius = radius * 1.01f;
            if (Math.Abs(restrictionDome.transform.localScale.x - collisionRadius) > 0.1f)
            {
                restrictionDome.transform.localScale = new Vector3(collisionRadius, collisionRadius, collisionRadius);
            }

            foreach (var player in PlayerCharacterMasterController.instances)
            {
                var body = player?.master?.GetBody();
                if (body == null)
                {
                    continue;
                }

                var characterMotor = body?.characterMotor;
                var motor = characterMotor?.Motor;
                if (characterMotor == null || motor == null)
                {
                    continue;
                }

                var playerPos = motor.Rigidbody.position;
                var delta = (playerPos - center);

                var length = delta.magnitude;

                if (length < radius * 0.98f)
                {
                    continue;
                }

                if (length > radius * 1.35f) // Teleport to center
                {
                    float rotateDelta = Random.Range(0.0f, 360.0f);
                    var translate = Quaternion.Euler(0, rotateDelta * 360.0f, 0) * Vector3.forward * 5f;
                    translate += Vector3.up * characterMotor.capsuleHeight * 2.0f;
                    motor.SetPosition(center + translate);
                }
                else
                {
                    Vector3 vel = characterMotor.velocity;
                    Vector3 newVel;

                    const float minVel = 10.0f;
                    if (vel.sqrMagnitude < minVel * minVel)
                    {
                        newVel = vel + delta * -0.1f;
                    }
                    else
                    {
                        newVel = Vector3.Project(vel, delta);
                        if (Vector3.Dot(newVel, delta) > 0)
                        {
                            newVel *= -1.0f;
                        }
                    }

                    characterMotor.velocity = newVel;
                }
            }
        }

        private void createCollider()
        {
            restrictionDome = new GameObject("LetMeOutCollider");
            restrictionDome.layer = 11;
            var meshFilterComp = restrictionDome.AddComponent<MeshFilter>();
            var mesh = meshFilterComp.mesh;

            IcoSphere.Create(restrictionDome);

            var meshColliderComp = restrictionDome.AddComponent<MeshCollider>();
            meshColliderComp.sharedMesh = mesh;

            var filterComp = restrictionDome.AddComponent<CollisionFilter>();
            filterComp.Collider = meshColliderComp;
        }
    }
}