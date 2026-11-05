using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// Feedback effects (plan M8): floating text, coins flying to the coin counter, confetti on a
    /// win, scale punches, and haptic pulses. Everything is pooled up front; UI effects run on
    /// unscaled time so they keep going on end screens.
    /// </summary>
    public sealed class Fx : MonoBehaviour
    {
        struct Floater
        {
            public Text text;
            public Vector3 world;
            public Color color;
            public float t;
            public bool active;
        }

        struct CoinPop
        {
            public Image image;
            public Vector3 from;
            public float delay;
            public float t;
            public bool active;
        }

        struct Confetto
        {
            public Image image;
            public Vector2 position;
            public Vector2 velocity;
            public float rotation;
            public float spin;
            public float t;
            public bool active;
        }

        struct PunchSlot
        {
            public Transform target;
            public Vector3 baseScale;
            public float amount;
            public float t;
            public bool active;
        }

        GameConfig config;
        Canvas canvas;
        Camera cam;
        Hud hud;
        Sfx sfx;
        Floater[] floaters;
        CoinPop[] coins;
        Confetto[] confetti;
        PunchSlot[] punches;

        /// <summary>Where flying coins land: the HUD counter in levels, the Home counter on Home.</summary>
        public RectTransform CoinTarget { get; set; }

        /// <summary>Phone vibration on or off (the Vibration setting).</summary>
        public static bool HapticsEnabled = true;

        public void Init(GameConfig config, Font font, SpriteFactory sprites, Canvas canvas, Camera camera, Hud hud, Sfx sfx)
        {
            this.config = config;
            this.canvas = canvas;
            cam = camera;
            this.hud = hud;
            this.sfx = sfx;
            CoinTarget = hud.CoinIcon;

            var layer = UiBuilder.Stretch(UiBuilder.Rect("Fx", canvas.transform));
            layer.SetAsLastSibling();

            floaters = new Floater[config.fxFloaterCount];
            for (int i = 0; i < floaters.Length; i++)
            {
                var text = UiBuilder.Label(layer, "Floater" + i, font, "", config.floatTextFont, Color.white, TextAnchor.MiddleCenter, config.uiShadow);
                text.gameObject.SetActive(false);
                floaters[i].text = text;
            }

            coins = new CoinPop[config.maxCoinPops * config.fxCoinBursts];
            for (int i = 0; i < coins.Length; i++)
            {
                var image = UiBuilder.Image(layer, "Coin" + i, sprites.Coin, config.uiCoin, false);
                image.rectTransform.sizeDelta = Vector2.one * config.coinPopSize;
                image.gameObject.SetActive(false);
                coins[i].image = image;
            }

            confetti = new Confetto[config.confettiCount];
            for (int i = 0; i < confetti.Length; i++)
            {
                var image = UiBuilder.Image(layer, "Confetti" + i, sprites.Panel, FloorPalette.ColorOf(i), true);
                image.rectTransform.sizeDelta = config.confettiSize;
                image.gameObject.SetActive(false);
                confetti[i].image = image;
            }

            punches = new PunchSlot[config.fxPunchSlots];
        }

        // ---- Floating text ----

        /// <summary>Text that rises and fades above a world position. Texts that would overlap on screen stack upward.</summary>
        public void FloatText(Vector3 world, string message, Color color, int fontSize)
        {
            float stackPixels = config.floatTextStackPixels * canvas.scaleFactor;
            var screen = cam.WorldToScreenPoint(world);
            for (int k = 0; k < floaters.Length; k++)
            {
                if (!floaters[k].active)
                    continue;
                var other = cam.WorldToScreenPoint(floaters[k].world);
                if (Mathf.Abs(other.y - screen.y) < stackPixels)
                {
                    world.y += config.floatTextStack;
                    screen = cam.WorldToScreenPoint(world);
                }
            }

            int i = FreeSlot(floaters);
            ref var f = ref floaters[i];
            f.world = world;
            f.color = color;
            f.t = 0f;
            f.active = true;
            f.text.text = message;
            f.text.fontSize = fontSize;
            f.text.gameObject.SetActive(true);
            PlaceFloater(ref f);
        }

        static int FreeSlot(Floater[] slots)
        {
            int oldest = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].active)
                    return i;
                if (slots[i].t > slots[oldest].t)
                    oldest = i;
            }
            return oldest;
        }

        void PlaceFloater(ref Floater f)
        {
            float u = f.t / config.floatTextTime;
            var screen = cam.WorldToScreenPoint(f.world);
            screen.y += config.floatTextRise * canvas.scaleFactor * EaseOut(u);
            float scale = Mathf.Lerp(config.floatTextPopScale, 1f, Mathf.Clamp01(u * 5f));
            // Keep the whole (popped) text on screen, e.g. above the cabin at the far left.
            float margin = (f.text.preferredWidth * 0.5f * scale + config.floatTextEdgeMargin) * canvas.scaleFactor;
            screen.x = Mathf.Clamp(screen.x, margin, Screen.width - margin);
            f.text.rectTransform.position = new Vector3(screen.x, screen.y, 0f);
            var c = f.color;
            c.a = u < 0.6f ? 1f : 1f - (u - 0.6f) / 0.4f;
            f.text.color = c;
            f.text.rectTransform.localScale = Vector3.one * scale;
        }

        // ---- Coins flying to the counter ----

        /// <summary>A few coins burst from a world position and fly to the HUD coin counter.</summary>
        public void CoinBurst(Vector3 world, int amount)
        {
            CoinBurstFromScreen(cam.WorldToScreenPoint(world), Mathf.Clamp(amount, 1, config.maxCoinPops));
        }

        /// <summary>Coins fly from a screen position to <see cref="CoinTarget"/>; returns how many flew.</summary>
        public int CoinBurstFromScreen(Vector3 from, int count)
        {
            int launched = 0;
            for (int n = 0; n < count; n++)
            {
                int i = -1;
                for (int k = 0; k < coins.Length; k++)
                    if (!coins[k].active) { i = k; break; }
                if (i < 0)
                    break;
                launched++;
                ref var c = ref coins[i];
                c.from = new Vector3(from.x, from.y, 0f);
                c.delay = n * config.coinPopStagger;
                c.t = 0f;
                c.active = true;
                c.image.rectTransform.position = c.from;
                c.image.gameObject.SetActive(false);
            }
            return launched;
        }

        /// <summary>Seconds until the last of <paramref name="count"/> coins from one burst lands.</summary>
        public float CoinBurstDuration(int count) => Mathf.Max(0, count - 1) * config.coinPopStagger + config.coinPopTime;

        // ---- Confetti ----

        public void Confetti()
        {
            float scale = canvas.scaleFactor;
            float width = Screen.width;
            for (int i = 0; i < confetti.Length; i++)
            {
                ref var p = ref confetti[i];
                // Some start just inside the top edge so the burst shows at once.
                p.position = new Vector2(Random.Range(0f, width), Screen.height + Random.Range(-config.confettiStartInside, config.confettiSpread) * scale);
                p.velocity = new Vector2(Random.Range(-1f, 1f) * config.confettiDrift, -Random.Range(0.3f, 1f) * config.confettiSpeed) * scale;
                p.rotation = Random.Range(0f, 360f);
                p.spin = Random.Range(-1f, 1f) * config.confettiSpin;
                p.t = 0f;
                p.active = true;
                p.image.gameObject.SetActive(true);
            }
        }

        /// <summary>A small burst of confetti from one screen point, e.g. when decor is bought (home plan 5.3).</summary>
        public void ConfettiBurst(Vector2 screen, int count, float speed)
        {
            float scale = canvas.scaleFactor;
            int launched = 0;
            for (int i = 0; i < confetti.Length && launched < count; i++)
            {
                ref var p = ref confetti[i];
                if (p.active)
                    continue;
                float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                p.position = screen;
                p.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed * Random.Range(0.5f, 1f) * scale;
                p.rotation = Random.Range(0f, 360f);
                p.spin = Random.Range(-1f, 1f) * config.confettiSpin;
                p.t = config.confettiTime * 0.5f;
                p.active = true;
                p.image.gameObject.SetActive(true);
                launched++;
            }
        }

        // ---- Scale punch ----

        /// <summary>A quick scale bump that settles back to the target's original scale.</summary>
        public void Punch(Transform target, float amount)
        {
            int free = -1;
            for (int i = 0; i < punches.Length; i++)
            {
                if (punches[i].active && punches[i].target == target)
                {
                    punches[i].t = 0f;
                    punches[i].amount = amount;
                    return;
                }
                if (!punches[i].active && free < 0)
                    free = i;
            }
            if (free < 0)
                return;
            punches[free] = new PunchSlot { target = target, baseScale = target.localScale, amount = amount, t = 0f, active = true };
        }

        /// <summary>A short vibration on phones; nothing elsewhere.</summary>
        public static void Haptic()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (HapticsEnabled)
                Handheld.Vibrate();
#endif
        }

        /// <summary>Removes floating texts, e.g. when an end screen opens.</summary>
        public void ClearFloaters()
        {
            for (int i = 0; i < floaters.Length; i++)
            {
                floaters[i].active = false;
                floaters[i].text.gameObject.SetActive(false);
            }
        }

        /// <summary>Stops every effect, e.g. when a level is rebuilt.</summary>
        public void ClearAll()
        {
            ClearFloaters();
            for (int i = 0; i < coins.Length; i++)
            {
                coins[i].active = false;
                coins[i].image.gameObject.SetActive(false);
            }
            for (int i = 0; i < confetti.Length; i++)
            {
                confetti[i].active = false;
                confetti[i].image.gameObject.SetActive(false);
            }
            for (int i = 0; i < punches.Length; i++)
                EndPunch(ref punches[i]);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            UpdateFloaters(dt);
            UpdateCoins(dt);
            UpdateConfetti(dt);
            UpdatePunches(Time.deltaTime);
        }

        void UpdateFloaters(float dt)
        {
            for (int i = 0; i < floaters.Length; i++)
            {
                ref var f = ref floaters[i];
                if (!f.active)
                    continue;
                f.t += dt;
                if (f.t >= config.floatTextTime)
                {
                    f.active = false;
                    f.text.gameObject.SetActive(false);
                    continue;
                }
                PlaceFloater(ref f);
            }
        }

        void UpdateCoins(float dt)
        {
            var target = CoinTarget.position;
            float arc = config.coinPopArc * canvas.scaleFactor;
            for (int i = 0; i < coins.Length; i++)
            {
                ref var c = ref coins[i];
                if (!c.active)
                    continue;
                if (c.delay > 0f)
                {
                    c.delay -= dt;
                    if (c.delay > 0f)
                        continue;
                    c.image.gameObject.SetActive(true);
                }
                c.t += dt / config.coinPopTime;
                float u = Mathf.Clamp01(c.t);
                float e = u * u;
                var p = Vector3.Lerp(c.from, target, e);
                p.y += Mathf.Sin(u * Mathf.PI) * arc;
                c.image.rectTransform.position = p;
                c.image.rectTransform.localScale = Vector3.one * Mathf.Lerp(config.coinPopStartScale, 1f, u);
                if (u >= 1f)
                {
                    c.active = false;
                    c.image.gameObject.SetActive(false);
                    sfx.Play(SfxKey.Coin);
                    Punch(CoinTarget, config.uiPunch);
                }
            }
        }

        void UpdateConfetti(float dt)
        {
            float gravity = config.confettiGravity * canvas.scaleFactor;
            for (int i = 0; i < confetti.Length; i++)
            {
                ref var p = ref confetti[i];
                if (!p.active)
                    continue;
                p.t += dt;
                if (p.t >= config.confettiTime || p.position.y < -config.confettiSize.y * canvas.scaleFactor)
                {
                    p.active = false;
                    p.image.gameObject.SetActive(false);
                    continue;
                }
                p.velocity.y -= gravity * dt;
                p.position += p.velocity * dt;
                p.rotation += p.spin * dt;
                var rt = p.image.rectTransform;
                rt.position = new Vector3(p.position.x, p.position.y, 0f);
                // Flutter: squash the width as if each piece turns over.
                rt.localScale = new Vector3(Mathf.Cos(p.rotation * Mathf.Deg2Rad * 2f), 1f, 1f);
                rt.localRotation = Quaternion.Euler(0f, 0f, p.rotation);
            }
        }

        void UpdatePunches(float dt)
        {
            for (int i = 0; i < punches.Length; i++)
            {
                ref var p = ref punches[i];
                if (!p.active)
                    continue;
                if (p.target == null)
                {
                    p.active = false;
                    continue;
                }
                p.t += dt / config.punchTime;
                if (p.t >= 1f)
                {
                    EndPunch(ref p);
                    continue;
                }
                float bump = Mathf.Sin(p.t * Mathf.PI) * (1f - p.t * 0.5f) * p.amount;
                p.target.localScale = p.baseScale * (1f + bump);
            }
        }

        static void EndPunch(ref PunchSlot p)
        {
            if (p.active && p.target != null)
                p.target.localScale = p.baseScale;
            p.active = false;
        }

        static float EaseOut(float u)
        {
            u = Mathf.Clamp01(u);
            return 1f - (1f - u) * (1f - u);
        }
    }
}
