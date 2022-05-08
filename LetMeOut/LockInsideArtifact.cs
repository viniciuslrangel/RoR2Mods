using System;
using System.IO;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Path = System.IO.Path;
using Random = UnityEngine.Random;

namespace LetMeOut;

public class LockInsideArtifact
{
    private string ArtifactName => "Artifact of Jail";
    private string ArtifactLangTokenName => "LETMEOUT_JAIL";
    private string ArtifactDescription => "When enabled, no player can leave the teleporter zone.";
    private Sprite ArtifactEnabledIcon => LoadTexture("artifactEnabled.png");
    private Sprite ArtifactDisabledIcon => LoadTexture("artifactDisabled.png");

    public ArtifactDef ArtifactDef;


    private bool bIsCharging = false;
    private float chargeTime = 0f;
    private float chargeTimeDelay = 2f;

    private GameObject restrictionDome = null;


    public bool ArtifactEnabled => RunArtifactManager.instance.IsArtifactEnabled(ArtifactDef);
    
    private bool bIsEnabledCached = false;

    public static LockInsideArtifact Instance { get; private set; }

    public LockInsideArtifact()
    {
        if (Instance != null)
            throw new InvalidOperationException("Singleton class \"LockInsideArtifact\" was instantiated twice");
        Instance = this;
    }

    public void Init()
    {
        CreateLang();
        CreateArtifact();
        Hook();
    }

    private void CreateLang()
    {
        LanguageAPI.Add("ARTIFACT_" + ArtifactLangTokenName + "_NAME", ArtifactName);
        LanguageAPI.Add("ARTIFACT_" + ArtifactLangTokenName + "_DESCRIPTION", ArtifactDescription);
    }

    private void CreateArtifact()
    {
        ArtifactDef = ScriptableObject.CreateInstance<ArtifactDef>();
        ArtifactDef.cachedName = "ARTIFACT_" + ArtifactLangTokenName;
        ArtifactDef.nameToken = "ARTIFACT_" + ArtifactLangTokenName + "_NAME";
        ArtifactDef.descriptionToken = "ARTIFACT_" + ArtifactLangTokenName + "_DESCRIPTION";
        ArtifactDef.smallIconSelectedSprite = ArtifactEnabledIcon;
        ArtifactDef.smallIconDeselectedSprite = ArtifactDisabledIcon;
        ContentAddition.AddArtifactDef(ArtifactDef);
    }

    private void Hook()
    {
        LetMeOutMod.OnUpdate += Update;
        SceneManager.sceneLoaded += (_, _) =>
        {
            bIsEnabledCached = ArtifactEnabled;
            TurnOff();
        };
        TeleporterInteraction.onTeleporterChargedGlobal += _ => TurnOff();
        TeleporterInteraction.onTeleporterBeginChargingGlobal += _ => TurnOn();
    }

    private void Update()
    {
        if (!bIsEnabledCached)
        {
            return;
        }
        if (!bIsCharging || chargeTime > Time.time)
        {
            return;
        }

        var controller = TeleporterInteraction.instance?.holdoutZoneController;
        if (controller == null)
        {
            return;
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
                Object.Destroy(restrictionDome);
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

    private void TurnOn()
    {
        if (!bIsEnabledCached)
        {
            return;
        }
        if (bIsCharging)
        {
            return;
        }

        bIsCharging = true;
        chargeTime = Time.time + chargeTimeDelay;
    }

    private void TurnOff()
    {
        bIsCharging = false;
        if (restrictionDome != null)
        {
            Object.Destroy(restrictionDome);
        }

        restrictionDome = null;
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

    private Sprite LoadTexture(string name)
    {
        var path = Path.Combine(Path.GetDirectoryName(LetMeOutMod.PInfo.Location) ?? string.Empty, name);
        var data = File.ReadAllBytes(path);
        var tex = new Texture2D(64, 64);
        if(!tex.LoadImage(data))
        {
            return null;
        }
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
}