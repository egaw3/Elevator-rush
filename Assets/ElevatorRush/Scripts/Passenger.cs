using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    public enum PassengerState
    {
        Waiting,
        Boarding,
        Riding,
        Exiting,
        Leaving
    }

    /// <summary>
    /// A passenger: a character whose body is tinted with its destination color, a billboarded
    /// bubble showing the destination icon, and a patience bar while waiting. Moves by lerping its
    /// local position (no physics). The pivot is at the feet.
    /// Uses the rigged "character" model with idle, walk and cheer clips when it is available;
    /// otherwise a capsule with a sphere head, animated procedurally (bob, hop, squash).
    /// </summary>
    public sealed class Passenger : MonoBehaviour, IPassengerInfo
    {
        const string ModelKeyA = "character/character_a";
        const string ModelKeyB = "character/character_b";
        const string BodyMeshName = "body-mesh";
        const string HeadMeshName = "head-mesh";
        const string IdleClip = "idle";
        const string WalkClip = "walk";
        const string CheerClip = "emote-yes";
        const string HeadBoneName = "head";
        const string TorsoBoneName = "torso";
        // Head sphere height relative to its size, for the capsule fallback.
        const float FallbackHeadCenter = 0.4f;
        // A walker counts as moving sideways when the horizontal step exceeds this.
        const float TurnThreshold = 1e-4f;

        GameConfig config;
        MaterialLibrary materials;
        SpriteFactory sprites;
        PassengerSpawner owner;
        CharacterSkinner skinner;
        SkinRig rig;
        readonly Dictionary<string, SkinParts> skins = new Dictionary<string, SkinParts>();
        SkinParts currentSkin;
        float crownBaseY;
        float bubbleHeight;

        float HatHeight => currentSkin != null ? currentSkin.hatHeight : 0f;

        Transform visual;
        Transform figure;
        Renderer bodyRenderer;
        Renderer headRenderer;
        Material headMaterial;
        Animation anim;
        string currentClip;
        float figureTop;
        float yaw;
        GameObject crown;
        Transform sparkles;
        Transform[] sparkleParts;
        Renderer iconRenderer;
        Transform bubble;
        Transform patienceBar;
        Transform patienceFill;
        Renderer patienceFillRenderer;
        int patienceColorStep = -1;

        Vector3 moveFrom;
        Vector3 moveTo;
        float moveT;
        float moveDuration;
        int hops;
        bool moving;
        float fadeT;
        bool fading;
        float cheerT;
        bool cheering;
        float squashT;
        float patience;
        float patienceMax;

        public int Origin { get; private set; }
        public int Destination { get; private set; }
        public PassengerKind Kind { get; private set; }
        public int Slots => Kind.Slots(config);
        public int CoinMultiplier => Kind.CoinMultiplier(config);
        public PassengerState State { get; private set; }
        /// <summary>The character skin being worn (home plan 7).</summary>
        public string Skin { get; private set; } = Catalog.DefaultSkin;
        public float PatienceFraction => patienceMax > 0f ? patience / patienceMax : 1f;
        bool Rigged => anim != null;

        public static Passenger Create(GameConfig config, MaterialLibrary materials, AssetLibrary assets,
            SpriteFactory sprites, CharacterSkinner skinner, PassengerSpawner owner, int variant)
        {
            var go = new GameObject("Passenger");
            var p = go.AddComponent<Passenger>();
            p.config = config;
            p.materials = materials;
            p.sprites = sprites;
            p.skinner = skinner;
            p.owner = owner;
            p.BuildVisuals(assets, variant);
            return p;
        }

        void BuildVisuals(AssetLibrary assets, int variant)
        {
            // Visuals sit under one child so shaking, bobbing and squashing never disturb the
            // walking position; the figure under it turns and widens.
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            figure = new GameObject("Figure").transform;
            figure.SetParent(visual, false);

            var prefab = assets.Model(variant % 2 == 0 ? ModelKeyA : ModelKeyB);
            Transform headBone = null;
            Transform torsoBone = null;
            if (prefab != null)
                BuildCharacter(assets, prefab, out headBone, out torsoBone);
            if (bodyRenderer == null || headRenderer == null)
            {
                BuildFallbackFigure(assets);
                headBone = null;
                torsoBone = null;
            }
            // Measured now, in the rest pose, so skin accessories follow the bones later.
            rig = SkinRig.Measure(figure, headRenderer, bodyRenderer, headBone, torsoBone);

            BuildCrown(assets);

            sparkles = new GameObject("Sparkles").transform;
            sparkles.SetParent(visual, false);
            sparkles.localPosition = new Vector3(0f, figureTop, 0f);
            sparkles.gameObject.AddComponent<Billboard>();
            var sparkleMaterial = materials.GetCutout(sprites.Star.texture, config.crownColor);
            sparkleParts = new Transform[config.sparkleCount];
            for (int i = 0; i < sparkleParts.Length; i++)
            {
                var part = assets.CreatePrimitive(PrimitiveType.Quad, "Sparkle" + i, sparkles, Vector3.zero,
                    Vector3.one * config.sparkleSize, sparkleMaterial);
                NoShadows(part.GetComponent<Renderer>());
                sparkleParts[i] = part.transform;
            }

            bubble = new GameObject("Bubble").transform;
            bubble.SetParent(visual, false);
            SetBubble(config.bubbleHeight, 1f);
            bubble.gameObject.AddComponent<Billboard>();

            var disc = assets.CreatePrimitive(PrimitiveType.Quad, "Disc", bubble, Vector3.zero,
                Vector3.one * config.bubbleSize, materials.GetCutout(sprites.Disc, config.bubbleColor));
            NoShadows(disc.GetComponent<Renderer>());
            iconRenderer = assets.CreatePrimitive(PrimitiveType.Quad, "Icon", bubble,
                new Vector3(0f, 0f, -config.bubbleIconOffset), Vector3.one * config.bubbleIconSize, null).GetComponent<Renderer>();
            NoShadows(iconRenderer);

            // Patience bar above the bubble: dark track with a fill anchored on the left.
            patienceBar = new GameObject("Patience").transform;
            patienceBar.SetParent(bubble, false);
            patienceBar.localPosition = new Vector3(0f, config.bubbleSize * 0.5f + config.patienceBarGap, 0f);
            var track = assets.CreatePrimitive(PrimitiveType.Quad, "Track", patienceBar, Vector3.zero,
                new Vector3(config.patienceBarWidth + config.patienceBarBorder * 2f, config.patienceBarHeight + config.patienceBarBorder * 2f, 1f),
                materials.GetUnlit(config.patienceTrackColor));
            NoShadows(track.GetComponent<Renderer>());
            patienceFill = assets.CreatePrimitive(PrimitiveType.Quad, "Fill", patienceBar,
                new Vector3(0f, 0f, -config.bubbleIconOffset), new Vector3(config.patienceBarWidth, config.patienceBarHeight, 1f), null).transform;
            patienceFillRenderer = patienceFill.GetComponent<Renderer>();
            NoShadows(patienceFillRenderer);
        }

        /// <summary>The rigged model: the body mesh takes the palette color, the head keeps its texture.</summary>
        void BuildCharacter(AssetLibrary assets, GameObject prefab, out Transform headBone, out Transform torsoBone)
        {
            var model = assets.SpawnModel(prefab, "Character", figure, config.characterHeight);
            headBone = FindChild(model.transform, HeadBoneName);
            torsoBone = FindChild(model.transform, TorsoBoneName);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == BodyMeshName)
                    bodyRenderer = r;
                else if (r.name == HeadMeshName)
                    headRenderer = r;
            }
            if (headRenderer != null)
                headMaterial = headRenderer.sharedMaterial;
            figureTop = config.characterHeight;

            anim = model.GetComponentInChildren<Animation>();
            if (anim == null || anim[IdleClip] == null || anim[WalkClip] == null)
            {
                anim = null;
                return;
            }
            anim[IdleClip].wrapMode = WrapMode.Loop;
            anim[WalkClip].wrapMode = WrapMode.Loop;
            anim[WalkClip].speed = config.walkAnimSpeed;
            if (anim[CheerClip] != null)
                anim[CheerClip].wrapMode = WrapMode.Loop;
            anim.cullingType = AnimationCullingType.BasedOnRenderers;
        }

        static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                    return t;
            return null;
        }

        /// <summary>
        /// Puts on a character skin; the body keeps its destination tint. Each skin is built the
        /// first time this passenger wears it and kept for later trips.
        /// </summary>
        public void SetSkin(string skinId)
        {
            if (skinId == null)
                skinId = Catalog.DefaultSkin;
            if (skinId == Skin && (currentSkin != null || skinId == Catalog.DefaultSkin))
                return;
            currentSkin?.SetActive(false);
            if (!skins.TryGetValue(skinId, out var parts))
            {
                parts = skinner.Build(skinId, rig);
                skins.Add(skinId, parts);
            }
            parts.SetActive(true);
            currentSkin = parts;
            Skin = skinId;
            // The VIP crown sits on top of any hat.
            var p = crown.transform.localPosition;
            crown.transform.localPosition = new Vector3(p.x, crownBaseY + parts.hatHeight, p.z);
            bubble.localPosition = new Vector3(0f, bubbleHeight + parts.hatHeight, 0f);
        }

        /// <summary>Capsule body and sphere head, used when the character model is missing.</summary>
        void BuildFallbackFigure(AssetLibrary assets)
        {
            // A model without the expected meshes is discarded.
            for (int i = figure.childCount - 1; i >= 0; i--)
                Destroy(figure.GetChild(i).gameObject);
            anim = null;
            float bodyH = config.passengerBodyHeight;
            float bodyW = config.passengerBodyWidth;
            float head = config.passengerHeadSize;
            // Unity's capsule is 2 units tall and 1 wide; scale it to the body size, feet at y = 0.
            bodyRenderer = assets.CreatePrimitive(PrimitiveType.Capsule, "Body", figure,
                new Vector3(0f, bodyH * 0.5f, 0f), new Vector3(bodyW, bodyH * 0.5f, bodyW), null).GetComponent<Renderer>();
            headRenderer = assets.CreatePrimitive(PrimitiveType.Sphere, "Head", figure,
                new Vector3(0f, bodyH + head * FallbackHeadCenter, 0f), Vector3.one * head, null).GetComponent<Renderer>();
            headMaterial = materials.Get(config.skinColor);
            figureTop = bodyH + head * (FallbackHeadCenter + 0.5f);
        }

        /// <summary>A procedural crown (band plus points), or the "crown" model when one is added.</summary>
        void BuildCrown(AssetLibrary assets)
        {
            var material = materials.Get(config.crownColor);
            var model = assets.Model("crown");
            float width = config.crownSize.x;
            float height = config.crownSize.y;
            if (model != null)
            {
                crown = assets.SpawnModel(model, "Crown", figure, height);
            }
            else
            {
                crown = new GameObject("Crown");
                crown.transform.SetParent(figure, false);
                float band = height * config.crownBandFraction;
                // Unity's cylinder is 2 units tall.
                assets.CreatePrimitive(PrimitiveType.Cylinder, "Band", crown.transform,
                    new Vector3(0f, band * 0.5f, 0f), new Vector3(width, band * 0.5f, width), material);
                var cone = assets.ConeMesh(config.crownSides);
                float pointSize = width * config.crownPointSize;
                for (int i = 0; i < config.crownPoints; i++)
                {
                    float a = i * Mathf.PI * 2f / config.crownPoints;
                    var pos = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (width * 0.5f - pointSize * 0.5f);
                    pos.y = band;
                    assets.CreateMesh(cone, "Point", crown.transform, pos, new Vector3(pointSize, height - band, pointSize), material);
                }
            }
            crownBaseY = figureTop - height * config.crownSink;
            crown.transform.localPosition = new Vector3(0f, crownBaseY, 0f);
        }

        static void NoShadows(Renderer r)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        /// <summary>Resets a pooled passenger for a new trip and places it at the spawn point.</summary>
        public void Spawn(int origin, int destination, PassengerKind kind, Vector3 position, float patienceSeconds)
        {
            Origin = origin;
            Destination = destination;
            Kind = kind;
            ApplyKindLook();
            State = PassengerState.Waiting;
            var color = FloorPalette.ColorOf(destination);
            bodyRenderer.sharedMaterial = materials.Get(color);
            headRenderer.sharedMaterial = headMaterial;
            iconRenderer.sharedMaterial = materials.GetCutout(sprites.Icon(FloorPalette.IconOf(destination)), color);
            transform.localPosition = position;
            transform.localScale = Vector3.one;
            visual.localPosition = Vector3.zero;
            visual.localScale = Vector3.one;
            yaw = config.faceCameraYaw;
            figure.localRotation = Quaternion.Euler(0f, yaw, 0f);
            SetBubble(config.bubbleHeight, 1f);
            patienceMax = patienceSeconds * kind.PatienceFactor(config);
            patience = patienceMax;
            patienceColorStep = -1;
            patienceBar.gameObject.SetActive(true);
            UpdatePatienceBar();
            moving = false;
            fading = false;
            cheering = false;
            squashT = 0f;
            currentClip = null;
            Play(IdleClip);
        }

        /// <summary>True while walking or stepping between spots.</summary>
        public bool IsMoving => moving;

        /// <summary>Shows or hides the destination bubble (hidden for passengers wandering on Home).</summary>
        public void SetBubbleVisible(bool visible)
        {
            bubble.gameObject.SetActive(visible);
        }

        /// <summary>Shows or hides the patience bar (hidden for the Characters preview).</summary>
        public void SetPatienceVisible(bool visible)
        {
            patienceBar.gameObject.SetActive(visible);
        }

        /// <summary>Walks to a position in the parent's space at walking speed.</summary>
        public void WalkTo(Vector3 target)
        {
            float distance = Vector3.Distance(transform.localPosition, target);
            StartMove(target, distance / config.walkSpeed, Mathf.Max(1, Mathf.RoundToInt(distance / config.hopLength)));
        }

        /// <summary>Steps into the cabin. Follow with <see cref="SetCabinSlot"/>.</summary>
        public void Board(Transform cabin)
        {
            State = PassengerState.Boarding;
            transform.SetParent(cabin, true);
            visual.localPosition = Vector3.zero;
            patienceBar.gameObject.SetActive(false);
        }

        /// <summary>Moves to a spot inside the cabin (local to it). Used when boarding and when riders shuffle.</summary>
        public void SetCabinSlot(Vector3 localSlot)
        {
            SetBubble(config.riderBubbleHeight, config.riderBubbleScale);
            StartMove(localSlot, config.boardTime, 1);
        }

        /// <summary>Leaves the cabin, walks to a point in the given parent's space, cheers, and fades out.</summary>
        public void Exit(Transform worldParent, Vector3 target)
        {
            State = PassengerState.Exiting;
            transform.SetParent(worldParent, true);
            SetBubble(config.bubbleHeight, 1f);
            WalkTo(target);
        }

        /// <summary>Patience ran out: the head turns red, then it stomps off to the right and fades out.</summary>
        void Leave()
        {
            State = PassengerState.Leaving;
            visual.localPosition = Vector3.zero;
            headRenderer.sharedMaterial = materials.Get(config.angryColor);
            patienceBar.gameObject.SetActive(false);
            owner.OnPatienceOut(this);
        }

        void ApplyKindLook()
        {
            bool big = Kind == PassengerKind.Big;
            float width = big ? config.bigWidthScale : 1f;
            figure.localScale = new Vector3(width, 1f, width);
            bool vip = Kind == PassengerKind.Vip;
            crown.SetActive(vip);
            sparkles.gameObject.SetActive(vip);
        }

        void Play(string clip)
        {
            if (!Rigged || clip == currentClip || anim[clip] == null)
                return;
            currentClip = clip;
            if (anim.isPlaying)
                anim.CrossFade(clip, config.animFadeTime);
            else
                anim.Play(clip);
        }

        void AnimateSparkles()
        {
            float t = Time.time * config.sparkleSpeed;
            for (int i = 0; i < sparkleParts.Length; i++)
            {
                float a = t + i * Mathf.PI * 2f / sparkleParts.Length;
                sparkleParts[i].localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.5f, 0f) * config.sparkleRadius;
                float twinkle = 0.5f + 0.5f * Mathf.Sin(Time.time * config.sparkleTwinkleSpeed + i * Mathf.PI);
                sparkleParts[i].localScale = Vector3.one * (config.sparkleSize * twinkle);
            }
        }

        /// <summary>Places the icon bubble, lifted by the worn hat's height so a hat never covers it.</summary>
        void SetBubble(float height, float scale)
        {
            bubbleHeight = height;
            bubble.localPosition = new Vector3(0f, height + HatHeight, 0f);
            bubble.localScale = Vector3.one * scale;
        }

        void StartMove(Vector3 target, float duration, int hopCount)
        {
            moveFrom = transform.localPosition;
            moveTo = target;
            moveT = 0f;
            moveDuration = Mathf.Max(duration, 1e-4f);
            hops = hopCount;
            moving = true;
            Play(WalkClip);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Kind == PassengerKind.Vip)
                AnimateSparkles();

            float shakeX = 0f;
            if (State == PassengerState.Waiting && owner.PatienceActive)
                shakeX = TickPatience(dt);

            float liftY = 0f;
            if (moving)
                liftY = StepMove(dt);
            else if (cheering)
                liftY = StepCheer(dt);
            else if (fading)
                StepFade(dt);
            else if (!Rigged && State == PassengerState.Waiting)
                liftY = Mathf.Abs(Mathf.Sin(Time.time * config.idleBobSpeed)) * config.idleBobHeight;

            visual.localPosition = new Vector3(shakeX, liftY, 0f);
            StepSquash(dt);
            StepTurn(dt);
        }

        /// <summary>Lerps along the current move; returns the hop height for the procedural figure.</summary>
        float StepMove(float dt)
        {
            moveT = Mathf.Min(1f, moveT + dt / moveDuration);
            transform.localPosition = Vector3.Lerp(moveFrom, moveTo, moveT);
            float hop = Rigged ? 0f : Mathf.Abs(Mathf.Sin(moveT * Mathf.PI * hops)) * config.hopHeight;
            if (moveT >= 1f)
            {
                moving = false;
                OnArrived();
            }
            return hop;
        }

        /// <summary>Plays the cheer (or little jumps for the procedural figure), then fades out.</summary>
        float StepCheer(float dt)
        {
            cheerT += dt;
            float lift = Rigged ? 0f : Mathf.Abs(Mathf.Sin(cheerT / config.cheerTime * Mathf.PI * 2f)) * config.hopHeight * 2f;
            if (cheerT >= config.cheerTime)
            {
                cheering = false;
                StartFade();
            }
            return lift;
        }

        void StepFade(float dt)
        {
            fadeT = Mathf.Min(1f, fadeT + dt / config.fadeTime);
            transform.localScale = Vector3.one * (1f - fadeT);
            if (fadeT >= 1f)
            {
                fading = false;
                owner.Despawn(this);
            }
        }

        /// <summary>A short squash after landing (procedural figure only).</summary>
        void StepSquash(float dt)
        {
            if (squashT <= 0f)
                return;
            squashT = Mathf.Max(0f, squashT - dt);
            float s = config.squashAmount * (squashT / config.squashTime);
            visual.localScale = new Vector3(1f + s, 1f - s, 1f + s);
        }

        /// <summary>Faces the camera when standing; turns three-quarters toward the walking direction.</summary>
        void StepTurn(float dt)
        {
            float target = config.faceCameraYaw;
            if (moving)
            {
                float dx = moveTo.x - moveFrom.x;
                if (dx > TurnThreshold)
                    target -= config.walkYawOffset;
                else if (dx < -TurnThreshold)
                    target += config.walkYawOffset;
            }
            yaw = Mathf.LerpAngle(yaw, target, 1f - Mathf.Exp(-config.characterTurnSpeed * dt));
            figure.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        float TickPatience(float dt)
        {
            patience = Mathf.Max(0f, patience - dt);
            UpdatePatienceBar();

            float shake = PatienceFraction < config.patienceShakeBelow
                ? Mathf.Sin(Time.time * config.shakeFrequency) * config.shakeAmplitude
                : 0f;

            if (patience <= 0f)
            {
                Leave();
                return 0f;
            }
            return shake;
        }

        void UpdatePatienceBar()
        {
            float f = PatienceFraction;
            float width = config.patienceBarWidth;
            patienceFill.localScale = new Vector3(width * f, config.patienceBarHeight, 1f);
            patienceFill.localPosition = new Vector3(-width * 0.5f * (1f - f), 0f, -config.bubbleIconOffset);

            // A handful of cached colors from red to green instead of a material per frame.
            int steps = Mathf.Max(1, config.patienceColorSteps);
            int step = Mathf.Clamp(Mathf.CeilToInt(f * steps), 0, steps);
            if (step == patienceColorStep)
                return;
            patienceColorStep = step;
            var color = Color.Lerp(config.patienceEmptyColor, config.patienceFullColor, (float)step / steps);
            patienceFillRenderer.sharedMaterial = materials.GetUnlit(color);
        }

        void OnArrived()
        {
            if (!Rigged)
                squashT = config.squashTime;

            switch (State)
            {
                case PassengerState.Boarding:
                    State = PassengerState.Riding;
                    Play(IdleClip);
                    break;
                case PassengerState.Exiting:
                    cheering = true;
                    cheerT = 0f;
                    Play(CheerClip);
                    break;
                case PassengerState.Leaving:
                    StartFade();
                    break;
                default:
                    Play(IdleClip);
                    break;
            }
        }

        void StartFade()
        {
            fading = true;
            fadeT = 0f;
        }
    }
}
