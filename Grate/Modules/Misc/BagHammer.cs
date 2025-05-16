using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Modules.Movement;
using GorillaLocomotion;
using Grate.Networking;
using Grate.Tools;
using System;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using NetworkPlayer = NetPlayer;
using Grate.Modules.Multiplayer;

namespace Grate.Modules.Misc
{
    public class BagHammer : GrateModule
    {
        public static readonly string DisplayName = "Bag Hammer";
        public static GameObject Sword;

        protected override void Start()
        {
            base.Start();
            if (Sword == null)
            {
                Sword = Instantiate(Plugin.assetBundle.LoadAsset<GameObject>("bagHammer"));
                Sword.transform.SetParent(GestureTracker.Instance.rightHand.transform, true);
                Sword.transform.localPosition = new Vector3(-0.5f, 0.1f, 0.4f);
                Sword.transform.localRotation = Quaternion.Euler(90, 90, 0);
                Sword.transform.localScale = new Vector3(200, 200, 200);
                Sword.SetActive(false);
            }
            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
            Patches.VRRigCachePatches.OnRigCached += OnRigCached;
        }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            try
            {            
                GestureTracker.Instance.rightGrip.OnPressed += ToggleBagHammerOn;
                GestureTracker.Instance.rightGrip.OnReleased += ToggleBagHammerOff;
            }
            catch (Exception e) { Logging.Exception(e); }
        }
        void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool enabled)
        {
            if (mod == DisplayName && player != NetworkSystem.Instance.LocalPlayer && player.UserId == "9ABD0C174289F58E")
            {
                if (enabled)
                {
                    player.Rig().gameObject.GetOrAddComponent<NetSword>();
                }
                else
                {
                    Destroy(player.Rig().gameObject.GetComponent<NetSword>());
                }
            }
        }


        void ToggleBagHammerOn(InputTracker tracker)
        {
            Sword?.SetActive(true);
        }

        void ToggleBagHammerOff(InputTracker tracker)
        {
            Sword?.SetActive(false);
        }

        protected override void Cleanup()
        {
            Sword?.Obliterate();
            if (GestureTracker.Instance != null)
            {
                GestureTracker.Instance.rightGrip.OnPressed -= ToggleBagHammerOn;
                GestureTracker.Instance.rightGrip.OnReleased -= ToggleBagHammerOff;
            }
        }

        private void OnRigCached(NetPlayer player, VRRig rig)
        {
            rig?.gameObject?.GetComponent<NetSword>()?.Obliterate();
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "i will destroy you and your puny rat sword";
        }

        class NetSword : MonoBehaviour
        {
            NetworkedPlayer networkedPlayer;
            GameObject hammer;

            void OnEnable()
            {
                networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
                var rightHand = networkedPlayer.rig.rightHandTransform;

                hammer = Instantiate(Sword);

                hammer.transform.SetParent(rightHand);
                hammer.transform.localPosition = new Vector3(0.1845f, -0.1f, -0.3f);
                hammer.transform.localRotation = Quaternion.Euler(25.83f, 208.26f, 121.76f);
                hammer.transform.localScale = new Vector3(16, 16, 16);

                networkedPlayer.OnGripPressed += OnGripPressed;
                networkedPlayer.OnGripReleased += OnGripReleased;
            }



            void OnGripPressed(NetworkedPlayer player, bool isLeft)
            {
                if (!isLeft)
                {
                    hammer.SetActive(true);
                }
            }
            void OnGripReleased(NetworkedPlayer player, bool isLeft)
            {
                if (!isLeft)
                {
                    hammer.SetActive(false);
                }
            }

            void OnDisable()
            {
                networkedPlayer.OnGripPressed -= OnGripPressed;
                networkedPlayer.OnGripReleased -= OnGripReleased;
            }

            void OnDestroy()
            {
                networkedPlayer.OnGripPressed -= OnGripPressed;
                networkedPlayer.OnGripReleased -= OnGripReleased;
            }
        }
    }

    class Hit
    {
        public static BagHammer sword;
        private float lastPunch;
        GameObject collider = sword.transform.Find("collision").gameObject;
        GorillaVelocityEstimator Velocity;
        public void Init()
        {
            if (sword != null)
            {
                collider = sword.transform.Find("collision").gameObject;
                Velocity = collider.AddComponent<GorillaVelocityEstimator>();
            }
            else
            {
                Debug.LogWarning("sword is null");
            }
        }

        void OnTriggerEnter(Collider other)
        {
            DoPunch(other.GetComponent<GorillaTagger>());
        }
        private void DoPunch(GorillaTagger glove)
            {
                if (Time.time - lastPunch < 1) return;
                Vector3 force = glove.bodyCollider.attachedRigidbody.velocity;
                force.Normalize();
                force *= 10;
                GorillaTagger.Instance.bodyCollider.attachedRigidbody.velocity += force;
                lastPunch = Time.time;
                GestureTracker.Instance.HapticPulse(false);
                GestureTracker.Instance.HapticPulse(true);

            }
    }
}
